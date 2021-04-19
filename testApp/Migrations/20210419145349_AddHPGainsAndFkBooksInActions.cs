using Microsoft.EntityFrameworkCore.Migrations;

namespace ARPG.Migrations
{
    public partial class AddHPGainsAndFkBooksInActions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HPGains",
                table: "Action",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "bookId",
                table: "Action",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Action_bookId",
                table: "Action",
                column: "bookId");

            migrationBuilder.AddForeignKey(
                name: "FK_Action_Book_bookId",
                table: "Action",
                column: "bookId",
                principalTable: "Book",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Action_Book_bookId",
                table: "Action");

            migrationBuilder.DropIndex(
                name: "IX_Action_bookId",
                table: "Action");

            migrationBuilder.DropColumn(
                name: "HPGains",
                table: "Action");

            migrationBuilder.DropColumn(
                name: "bookId",
                table: "Action");
        }
    }
}
