using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using MultiTenantAccountManager.Models;
using System.Net.Mail;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using System.Data.Entity;

namespace MultiTenantAccountManager
{
    // Configure the application user manager used in this application. UserManager is defined in ASP.NET Identity and is used by the application.

    public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(IUserStore<ApplicationUser> store)
            : base(store)
        {
        }

        public override Task<ApplicationUser> FindByNameAsync(string userName)
        {
            throw new NotImplementedException();
        }

        public async Task<ApplicationUser> FindByNameAsync(string userName, string tenantIdOrName)
        {
            var ret = await Users.FirstOrDefaultAsync(it => (it.TenantId == tenantIdOrName || it.Tenant.Name == tenantIdOrName) && it.UserName == userName);
            return ret;
        }

        public ApplicationUser FindByName(string userName, string tenantIdOrName)
        {
            var ret =  Users.FirstOrDefault(it => (it.TenantId == tenantIdOrName || it.Tenant.Name == tenantIdOrName) && it.UserName == userName);
            return ret;
        }

        public async Task<ApplicationUser> FindAsync(string userName, string tenantIdOrName, string password)
        {
            var user = Users.FirstOrDefault(it => (it.TenantId == tenantIdOrName || it.Tenant.Name == tenantIdOrName) && it.UserName == userName);
            if (user == null || !await CheckPasswordAsync(user, password))
                return null;
            return user;
        }

        public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext context)
        {
            var manager = new ApplicationUserManager(new UserStore<ApplicationUser>(context.Get<ApplicationDbContext>()));
            // Configure validation logic for usernames
            manager.UserValidator = new ApplicationUserValidator(manager);
            // Configure validation logic for passwords
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6,
                RequireNonLetterOrDigit = true,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
            };
            var dataProtectionProvider = options.DataProtectionProvider;
            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider = new DataProtectorTokenProvider<ApplicationUser>(dataProtectionProvider.Create("ASP.NET Identity"));
            }
            return manager;
        }
    }

    public class ApplicationTenantManager : IDisposable
    {
        public ApplicationDbContext Context { get; private set; }

        public ApplicationTenantManager(ApplicationDbContext context)
        {
            Context = context;
        }

        public static ApplicationTenantManager Create(IdentityFactoryOptions<ApplicationTenantManager> options, IOwinContext context)
        {
            var manager = new ApplicationTenantManager(context.Get<ApplicationDbContext>());
            return manager;
        }

        public async Task<ApplicationTenant> FindByIdAsync(string id)
        {
            return await Context.Tenants.FirstOrDefaultAsync(it => it.Id == id);
        }

        public async Task<ApplicationTenant> FindByNameAsync(string name)
        {
            return await Context.Tenants.FirstOrDefaultAsync(it => it.Name == name);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Context.Dispose();
                }

                disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion

    }

    public class ApplicationUserValidator : IIdentityValidator<ApplicationUser>
    {
        public ApplicationUserManager Manager { get; private set; }

        public ApplicationUserValidator(ApplicationUserManager manager)
        {
            Manager = manager;
        }

        public async Task<IdentityResult> ValidateAsync(ApplicationUser item)
        {
            var errors = new List<string>();

            try
            {
                var mail = new MailAddress(item.Email);
            }
            catch
            {
                errors.Add(string.Format("Email {0} is invalid.", item.Email));
            }

            if (!Regex.IsMatch(item.UserName, @"^[\w\.]+$"))
            {
                // If any characters are not letters or digits, its an illegal user name
                errors.Add(string.Format("User name {0} is invalid, can only contain letters or digits.", item.UserName));
            }
            else if (await Manager.Users.AnyAsync(it => it.TenantId == item.Tenant.Id && it.UserName == item.UserName))
            {
                errors.Add( string.Format("User name {0} is already taken.", item.UserName));
            }

            if (errors.Count > 0)
                return IdentityResult.Failed(errors.ToArray());

            return IdentityResult.Success;
        }
    }
}
