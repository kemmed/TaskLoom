using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace diplom.Migrations
{
    /// <inheritdoc />
    public partial class RenameProjectStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProjectStatus",
                table: "Project",
                newName: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Project",
                newName: "ProjectStatus");
        }
    }
}
