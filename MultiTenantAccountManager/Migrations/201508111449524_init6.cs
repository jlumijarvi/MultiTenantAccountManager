namespace MultiTenantAccountManager.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class init6 : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.Tenants", new[] { "Name" });
            AlterColumn("dbo.Tenants", "Name", c => c.String(maxLength: 128));
            CreateIndex("dbo.Tenants", "Name", unique: true);
        }
        
        public override void Down()
        {
            DropIndex("dbo.Tenants", new[] { "Name" });
            AlterColumn("dbo.Tenants", "Name", c => c.String());
            CreateIndex("dbo.Tenants", "Name", unique: true);
        }
    }
}
