using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefLan.DB.Migrations.Courses
{
    /// <inheritdoc />
    public partial class AddStaffOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_payment_method",
                schema: "courses",
                table: "orders");

            migrationBuilder.AlterColumn<int>(
                name: "payment_method",
                schema: "courses",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                comment: "1=BankTransfer, 2=ContactChef, 3=StaffCreated",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1,
                oldComment: "1=BankTransfer, 2=ContactChef");

            migrationBuilder.AddColumn<string>(
                name: "discount_reason",
                schema: "courses",
                table: "orders",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_vnd",
                schema: "courses",
                table: "orders",
                type: "numeric(12,0)",
                precision: 12,
                scale: 0,
                nullable: true,
                comment: "Staff's discount on the whole order (already split over the items); null when there is none");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_payment_method",
                schema: "courses",
                table: "orders",
                sql: "payment_method IN (1, 2, 3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_payment_method",
                schema: "courses",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "discount_reason",
                schema: "courses",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "discount_vnd",
                schema: "courses",
                table: "orders");

            migrationBuilder.AlterColumn<int>(
                name: "payment_method",
                schema: "courses",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                comment: "1=BankTransfer, 2=ContactChef",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1,
                oldComment: "1=BankTransfer, 2=ContactChef, 3=StaffCreated");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_payment_method",
                schema: "courses",
                table: "orders",
                sql: "payment_method IN (1, 2)");
        }
    }
}
