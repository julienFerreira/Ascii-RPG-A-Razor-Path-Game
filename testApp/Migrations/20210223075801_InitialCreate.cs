using Microsoft.EntityFrameworkCore.Migrations;

namespace testApp.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Action",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActionNumber = table.Column<int>(nullable: false),
                    ActionMessage = table.Column<string>(nullable: true),
                    SuccessorMessage1 = table.Column<string>(nullable: true),
                    SuccessorCode1 = table.Column<int>(nullable: false),
                    SuccessorMessage2 = table.Column<string>(nullable: true),
                    SuccessorCode2 = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Action", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Action");
        }
    }
}
