using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace taskloom.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FName = table.Column<string>(type: "TEXT", nullable: true),
                    LName = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    HashPass = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    RegToken = table.Column<string>(type: "TEXT", nullable: true),
                    RegTokenDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PassRecoveryToken = table.Column<string>(type: "TEXT", nullable: true),
                    PassRecoveryTokenDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Project",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeadlineDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDelete = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Project_User_UserID",
                        column: x => x.UserID,
                        principalTable: "User",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "CategoryType",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryType", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CategoryType_Project_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Project",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Log",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Event = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Log", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Log_Project_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Project",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriorityType",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriorityType", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PriorityType_Project_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Project",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StatusType",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusType", x => x.ID);
                    table.ForeignKey(
                        name: "FK_StatusType_Project_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Project",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProject",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserID = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectID = table.Column<int>(type: "INTEGER", nullable: false),
                    UserRole = table.Column<int>(type: "INTEGER", nullable: false),
                    InviteToken = table.Column<string>(type: "TEXT", nullable: true),
                    InviteTokenDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsCreator = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProject", x => x.ID);
                    table.ForeignKey(
                        name: "FK_UserProject_Project_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Project",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProject_User_UserID",
                        column: x => x.UserID,
                        principalTable: "User",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Issue",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeadlineDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatorID = table.Column<int>(type: "INTEGER", nullable: false),
                    PerformerID = table.Column<int>(type: "INTEGER", nullable: true),
                    ProjectID = table.Column<int>(type: "INTEGER", nullable: false),
                    PriorityTypeID = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDelete = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CategoryTypeID = table.Column<int>(type: "INTEGER", nullable: true),
                    StatusTypeID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Issue", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Issue_CategoryType_CategoryTypeID",
                        column: x => x.CategoryTypeID,
                        principalTable: "CategoryType",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Issue_PriorityType_PriorityTypeID",
                        column: x => x.PriorityTypeID,
                        principalTable: "PriorityType",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Issue_Project_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Project",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Issue_StatusType_StatusTypeID",
                        column: x => x.StatusTypeID,
                        principalTable: "StatusType",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Issue_User_CreatorID",
                        column: x => x.CreatorID,
                        principalTable: "User",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Issue_User_PerformerID",
                        column: x => x.PerformerID,
                        principalTable: "User",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CategoryTypeUserProject",
                columns: table => new
                {
                    CategoryTypesID = table.Column<int>(type: "INTEGER", nullable: false),
                    UserProjectsID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryTypeUserProject", x => new { x.CategoryTypesID, x.UserProjectsID });
                    table.ForeignKey(
                        name: "FK_CategoryTypeUserProject_CategoryType_CategoryTypesID",
                        column: x => x.CategoryTypesID,
                        principalTable: "CategoryType",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryTypeUserProject_UserProject_UserProjectsID",
                        column: x => x.UserProjectsID,
                        principalTable: "UserProject",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryType_ProjectID",
                table: "CategoryType",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryTypeUserProject_UserProjectsID",
                table: "CategoryTypeUserProject",
                column: "UserProjectsID");

            migrationBuilder.CreateIndex(
                name: "IX_Issue_CategoryTypeID",
                table: "Issue",
                column: "CategoryTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Issue_CreatorID",
                table: "Issue",
                column: "CreatorID");

            migrationBuilder.CreateIndex(
                name: "IX_Issue_PerformerID",
                table: "Issue",
                column: "PerformerID");

            migrationBuilder.CreateIndex(
                name: "IX_Issue_PriorityTypeID",
                table: "Issue",
                column: "PriorityTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Issue_ProjectID",
                table: "Issue",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_Issue_StatusTypeID",
                table: "Issue",
                column: "StatusTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Log_ProjectID",
                table: "Log",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_PriorityType_ProjectID",
                table: "PriorityType",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_Project_UserID",
                table: "Project",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_StatusType_ProjectID",
                table: "StatusType",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_UserProject_ProjectID",
                table: "UserProject",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_UserProject_UserID",
                table: "UserProject",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryTypeUserProject");

            migrationBuilder.DropTable(
                name: "Issue");

            migrationBuilder.DropTable(
                name: "Log");

            migrationBuilder.DropTable(
                name: "UserProject");

            migrationBuilder.DropTable(
                name: "CategoryType");

            migrationBuilder.DropTable(
                name: "PriorityType");

            migrationBuilder.DropTable(
                name: "StatusType");

            migrationBuilder.DropTable(
                name: "Project");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
