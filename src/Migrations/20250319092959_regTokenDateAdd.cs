using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace diplom.Migrations
{
    /// <inheritdoc />
    public partial class regTokenDateAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RegTokenDate",
                table: "User",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegTokenDate",
                table: "User");
        }
    }
}
