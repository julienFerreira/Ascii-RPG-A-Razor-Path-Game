using Microsoft.EntityFrameworkCore.Migrations;

namespace ARPG.Migrations
{
    public partial class AddisWonInActionModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Action_Book_bookId",
                table: "Action");

            migrationBuilder.RenameColumn(
                name: "bookId",
                table: "Action",
                newName: "BookId");

            migrationBuilder.RenameIndex(
                name: "IX_Action_bookId",
                table: "Action",
                newName: "IX_Action_BookId");

            migrationBuilder.AlterColumn<int>(
                name: "BookId",
                table: "Action",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isWon",
                table: "Action",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Action_Book_BookId",
                table: "Action",
                column: "BookId",
                principalTable: "Book",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Action_Book_BookId",
                table: "Action");

            migrationBuilder.DropColumn(
                name: "isWon",
                table: "Action");

            migrationBuilder.RenameColumn(
                name: "BookId",
                table: "Action",
                newName: "bookId");

            migrationBuilder.RenameIndex(
                name: "IX_Action_BookId",
                table: "Action",
                newName: "IX_Action_bookId");

            migrationBuilder.AlterColumn<int>(
                name: "bookId",
                table: "Action",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddForeignKey(
                name: "FK_Action_Book_bookId",
                table: "Action",
                column: "bookId",
                principalTable: "Book",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
