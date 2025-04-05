using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace diplom.Migrations
{
    /// <inheritdoc />
    public partial class addCategoryType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StatusType_Project_ProjectID1",
                table: "StatusType");

            migrationBuilder.DropIndex(
                name: "IX_StatusType_ProjectID1",
                table: "StatusType");

            migrationBuilder.DropColumn(
                name: "ProjectID1",
                table: "StatusType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectID1",
                table: "StatusType",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatusType_ProjectID1",
                table: "StatusType",
                column: "ProjectID1");

            migrationBuilder.AddForeignKey(
                name: "FK_StatusType_Project_ProjectID1",
                table: "StatusType",
                column: "ProjectID1",
                principalTable: "Project",
                principalColumn: "ID");
        }
    }
}
