using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefLan.DB.Migrations.Courses
{
    /// <inheritdoc />
    public partial class AllowFreeOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_amount_positive",
                schema: "courses",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_paid_via",
                schema: "courses",
                table: "orders");

            migrationBuilder.AlterColumn<int>(
                name: "paid_via",
                schema: "courses",
                table: "orders",
                type: "integer",
                nullable: true,
                comment: "1=Webhook, 2=Manual, 3=Free",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "1=Webhook, 2=Manual");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_amount_non_negative",
                schema: "courses",
                table: "orders",
                sql: "amount_vnd >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_paid_via",
                schema: "courses",
                table: "orders",
                sql: "paid_via IS NULL OR paid_via IN (1, 2, 3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_amount_non_negative",
                schema: "courses",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_paid_via",
                schema: "courses",
                table: "orders");

            migrationBuilder.AlterColumn<int>(
                name: "paid_via",
                schema: "courses",
                table: "orders",
                type: "integer",
                nullable: true,
                comment: "1=Webhook, 2=Manual",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "1=Webhook, 2=Manual, 3=Free");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_amount_positive",
                schema: "courses",
                table: "orders",
                sql: "amount_vnd > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_paid_via",
                schema: "courses",
                table: "orders",
                sql: "paid_via IS NULL OR paid_via IN (1, 2)");
        }
    }
}
