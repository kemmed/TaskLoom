using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace diplom.Migrations
{
    /// <inheritdoc />
    public partial class addPerformer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Issue_User_CreatorID",
                table: "Issue");

            migrationBuilder.CreateIndex(
                name: "IX_Issue_PerformerID",
                table: "Issue",
                column: "PerformerID");

            migrationBuilder.AddForeignKey(
                name: "FK_Issue_User_CreatorID",
                table: "Issue",
                column: "CreatorID",
                principalTable: "User",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Issue_User_PerformerID",
                table: "Issue",
                column: "PerformerID",
                principalTable: "User",
                principalColumn: "ID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Issue_User_CreatorID",
                table: "Issue");

            migrationBuilder.DropForeignKey(
                name: "FK_Issue_User_PerformerID",
                table: "Issue");

            migrationBuilder.DropIndex(
                name: "IX_Issue_PerformerID",
                table: "Issue");

            migrationBuilder.AddForeignKey(
                name: "FK_Issue_User_CreatorID",
                table: "Issue",
                column: "CreatorID",
                principalTable: "User",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
