namespace MultiTenantAccountManager.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class init9 : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.Users", new[] { "TenantId" });
            DropIndex("dbo.Users", "UserNameIndex");
            CreateIndex("dbo.Users", new[] { "UserName", "TenantId" }, unique: true, name: "UserNameIndex");
        }
        
        public override void Down()
        {
            DropIndex("dbo.Users", "UserNameIndex");
            CreateIndex("dbo.Users", "UserName", name: "UserNameIndex");
            CreateIndex("dbo.Users", "TenantId");
        }
    }
}
