using Microsoft.EntityFrameworkCore.Migrations;

namespace ARPG.Migrations
{
    public partial class AddIsValidAttribut : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsValid",
                table: "Book",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsValid",
                table: "Book");
        }
    }
}
