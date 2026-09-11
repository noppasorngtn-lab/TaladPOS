using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaladPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StaffAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    PerformedByStaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffAuditLogs_Staff_PerformedByStaffId",
                        column: x => x.PerformedByStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffAuditLogs_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaffAuditLogs_PerformedByStaffId",
                table: "StaffAuditLogs",
                column: "PerformedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAuditLogs_StaffId",
                table: "StaffAuditLogs",
                column: "StaffId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaffAuditLogs");
        }
    }
}
