namespace MultiTenantAccountManager.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class init2 : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.Users", "UserNameIndex");
            CreateIndex("dbo.Users", "UserName", unique: true, name: "UserNameIndex");
        }
        
        public override void Down()
        {
            DropIndex("dbo.Users", "UserNameIndex");
            CreateIndex("dbo.Users", "UserName", name: "UserNameIndex");
        }
    }
}
