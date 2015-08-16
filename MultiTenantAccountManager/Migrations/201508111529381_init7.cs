namespace MultiTenantAccountManager.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class init7 : DbMigration
    {
        public override void Up()
        {
            //DropIndex("dbo.Tenants", new[] { "Id" });
            DropIndex("dbo.Tenants", new[] { "Name" });
            CreateIndex("dbo.Tenants", "Name", unique: true);
        }
        
        public override void Down()
        {
            DropIndex("dbo.Tenants", new[] { "Name" });
            CreateIndex("dbo.Tenants", "Name", unique: true);
            //CreateIndex("dbo.Tenants", "Id");
        }
    }
}
