using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefLan.DB.Migrations.Courses
{
    /// <inheritdoc />
    public partial class AddOrderPaymentMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "payment_method",
                schema: "courses",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                comment: "1=BankTransfer, 2=ContactChef");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_payment_method",
                schema: "courses",
                table: "orders",
                sql: "payment_method IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_payment_method",
                schema: "courses",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "payment_method",
                schema: "courses",
                table: "orders");
        }
    }
}
