using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biblio.BackOffice.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAuditTrailAndImportIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminAuditEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Loans_DueAt",
                table: "Loans",
                column: "DueAt");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_UserEmail_ReturnedAt_DueAt",
                table: "Loans",
                columns: new[] { "UserEmail", "ReturnedAt", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Books_Title_Author",
                table: "Books",
                columns: new[] { "Title", "Author" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditEvents_AdminEmail",
                table: "AdminAuditEvents",
                column: "AdminEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditEvents_CreatedAt",
                table: "AdminAuditEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditEvents_EntityType_EntityId_CreatedAt",
                table: "AdminAuditEvents",
                columns: new[] { "EntityType", "EntityId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAuditEvents");

            migrationBuilder.DropIndex(
                name: "IX_Loans_DueAt",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Loans_UserEmail_ReturnedAt_DueAt",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Books_Title_Author",
                table: "Books");
        }
    }
}
