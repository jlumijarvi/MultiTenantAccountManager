namespace MultiTenantAccountManager.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class init4 : DbMigration
    {
        public override void Up()
        {
            //CreateIndex("dbo.Tenants", "Name", unique: true);
        }
        
        public override void Down()
        {
            //DropIndex("dbo.Tenants", new[] { "Name" });
        }
    }
}
