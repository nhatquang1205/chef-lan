using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefLan.DB.Migrations.Courses
{
    /// <inheritdoc />
    public partial class AddCustomerInvitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "invite_expires_at",
                schema: "customers",
                table: "customers",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "invite_token_hash",
                schema: "customers",
                table: "customers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "uq_customers_invite_token_hash",
                schema: "customers",
                table: "customers",
                column: "invite_token_hash",
                unique: true,
                filter: "invite_token_hash IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_customers_invite_token_hash",
                schema: "customers",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "invite_expires_at",
                schema: "customers",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "invite_token_hash",
                schema: "customers",
                table: "customers");
        }
    }
}
