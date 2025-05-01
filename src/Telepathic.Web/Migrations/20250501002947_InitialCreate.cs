using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Telepathic.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    TeamMemberID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Projects_Categories_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "Categories",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Projects_TeamMembers_TeamMemberID",
                        column: x => x.TeamMemberID,
                        principalTable: "TeamMembers",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "ProjectTasks",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ProjectID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTasks", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ProjectTasks_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CategoryID",
                table: "Projects",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_TeamMemberID",
                table: "Projects",
                column: "TeamMemberID");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_ProjectID",
                table: "ProjectTasks",
                column: "ProjectID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectTasks");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "TeamMembers");
        }
    }
}
