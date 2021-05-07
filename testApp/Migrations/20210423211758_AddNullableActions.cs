using Microsoft.EntityFrameworkCore.Migrations;

namespace ARPG.Migrations
{
    public partial class AddNullableActions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Action_Book_BookId",
                table: "Action");

            migrationBuilder.AlterColumn<int>(
                name: "SuccessorCode2",
                table: "Action",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "SuccessorCode1",
                table: "Action",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

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

            migrationBuilder.AlterColumn<int>(
                name: "SuccessorCode2",
                table: "Action",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SuccessorCode1",
                table: "Action",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Action_Book_BookId",
                table: "Action",
                column: "BookId",
                principalTable: "Book",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
