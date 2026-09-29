using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartnerCommission.Wallets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "wallet_entries",
                columns: table => new
                {
                    CommissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserExternalId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EventExternalId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AccruedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_entries", x => x.CommissionId);
                });

            migrationBuilder.CreateTable(
                name: "wallets",
                columns: table => new
                {
                    UserExternalId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallets", x => x.UserExternalId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_wallet_entries_Status",
                table: "wallet_entries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_entries_UserExternalId_PaidAtUtc",
                table: "wallet_entries",
                columns: new[] { "UserExternalId", "PaidAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_wallet_entries_UserExternalId_Status",
                table: "wallet_entries",
                columns: new[] { "UserExternalId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wallet_entries");

            migrationBuilder.DropTable(
                name: "wallets");
        }
    }
}
