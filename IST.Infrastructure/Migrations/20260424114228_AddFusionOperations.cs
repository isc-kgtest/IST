using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IST.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFusionOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "_Events",
                columns: table => new
                {
                    Uuid = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    LoggedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DelayUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValueJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Events", x => x.Uuid);
                });

            migrationBuilder.CreateTable(
                name: "_Operations",
                columns: table => new
                {
                    Index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<string>(type: "text", nullable: false),
                    HostId = table.Column<string>(type: "text", nullable: false),
                    LoggedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CommandJson = table.Column<string>(type: "text", nullable: false),
                    ItemsJson = table.Column<string>(type: "text", nullable: true),
                    NestedOperations = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Operations", x => x.Index);
                });

            migrationBuilder.CreateIndex(
                name: "IX__Events_DelayUntil_State",
                table: "_Events",
                columns: new[] { "DelayUntil", "State" });

            migrationBuilder.CreateIndex(
                name: "IX__Events_State_DelayUntil",
                table: "_Events",
                columns: new[] { "State", "DelayUntil" });

            migrationBuilder.CreateIndex(
                name: "IX__Operations_LoggedAt",
                table: "_Operations",
                column: "LoggedAt");

            migrationBuilder.CreateIndex(
                name: "IX__Operations_Uuid",
                table: "_Operations",
                column: "Uuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "_Events");

            migrationBuilder.DropTable(
                name: "_Operations");
        }
    }
}
