using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System.Data.Entity.Infrastructure.Annotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System;

namespace MultiTenantAccountManager.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string TenantId { get; set; }
        public virtual ApplicationTenant Tenant { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager, string authenticationType)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, authenticationType);
            // Add custom user claims here
            return userIdentity;
        }
    }

    [Table("Tenants")]
    public class ApplicationTenant
    {
        public string Id { get; set; }
        [Index(IsUnique = true)]
        [MaxLength(128)]
        public string Name { get; set; }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        static ApplicationDbContext()
        {
            Database.SetInitializer(new ApplicationDbInitializer());
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // remove AspNet prefix
            modelBuilder.Entity<ApplicationUser>().ToTable("Users");
            modelBuilder.Entity<IdentityRole>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole>().ToTable("UserRoles");
            modelBuilder.Entity<IdentityUserClaim>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin>().ToTable("UserLogins");

            // set username uniqueness requirement inside tenant
            modelBuilder
                .Entity<ApplicationUser>()
                .Property(it => it.UserName)
                .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute("UserNameIndex") { IsUnique = true, Order = 1 }));
            modelBuilder
                .Entity<ApplicationUser>()
                .Property(it => it.TenantId)
                .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute("UserNameIndex") { IsUnique = true, Order = 2 }));
        }

        protected override DbEntityValidationResult ValidateEntity(DbEntityEntry entityEntry, IDictionary<object, object> items)
        {
            var res = base.ValidateEntity(entityEntry, items);

            if (entityEntry.Entity is ApplicationUser)
            {
                // remove username errors
                var usernameErrors = res.ValidationErrors.Where(it => it.PropertyName == "User").ToList();
                usernameErrors.ForEach(it => res.ValidationErrors.Remove(it));

                // ensure unique username inside tenant
                var user = entityEntry.Entity as ApplicationUser;

                if (Users.Any(it => it.Id != user.Id && it.TenantId == user.TenantId && it.UserName == user.UserName))
                {
                    res.ValidationErrors.Add(new DbValidationError("User", string.Format("User name {0} is already taken.", user.UserName)));
                }
            }

            return res;
        }

        public DbSet<ApplicationTenant> Tenants { get; set; }
    }

    public class ApplicationDbInitializer : IDatabaseInitializer<ApplicationDbContext>
    {
        public void InitializeDatabase(ApplicationDbContext context)
        {
            // if database did not exist before - create it
            context.Database.CreateIfNotExists();

            // create roles
            if (!(context.Roles.Any(it => it.Name == "SuperAdmin")))
                context.Roles.Add(new IdentityRole() { Id = "SuperAdmin", Name = "SuperAdmin" });
            if (!(context.Roles.Any(it => it.Name == "Admin")))
                context.Roles.Add(new IdentityRole() { Id = "Admin", Name = "Admin" });
            if (!(context.Roles.Any(it => it.Name == "User")))
                context.Roles.Add(new IdentityRole() { Id = "User", Name = "User" });

            // create a tenant for super admin user
            var tenant = context.Tenants.FirstOrDefault(it => it.Name == "admin");
            string tenantId = tenant == null ? Guid.NewGuid().ToString() : tenant.Id;
            if (tenant == null)
                context.Tenants.Add(new ApplicationTenant() { Id = tenantId, Name = "admin" });

            context.SaveChanges();

            // create super admin user
            using (var manager = new ApplicationUserManager(new UserStore<ApplicationUser>(context)))
            {
                manager.UserValidator = new ApplicationUserValidator(manager);
                var adminUser = manager.FindByNameAsync("admin", tenantId).Result;
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser() { UserName = "admin", Email = "admin@admin.com", TenantId = tenant?.Id, Tenant = tenant };
                    var res = manager.Create(adminUser, "adm1n_");
                    adminUser = manager.FindByNameAsync("admin", tenantId).Result;
                }

                manager.AddToRole(adminUser.Id, "SuperAdmin");
            }
        }
    }
}
