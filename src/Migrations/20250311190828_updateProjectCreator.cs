using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace diplom.Migrations
{
    /// <inheritdoc />
    public partial class updateProjectCreator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatorID",
                table: "Project",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CreatorUserID",
                table: "Project",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Project_CreatorUserID",
                table: "Project",
                column: "CreatorUserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Project_User_CreatorUserID",
                table: "Project",
                column: "CreatorUserID",
                principalTable: "User",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Project_User_CreatorUserID",
                table: "Project");

            migrationBuilder.DropIndex(
                name: "IX_Project_CreatorUserID",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "CreatorID",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "CreatorUserID",
                table: "Project");
        }
    }
}
