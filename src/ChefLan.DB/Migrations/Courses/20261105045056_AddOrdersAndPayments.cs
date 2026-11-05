using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ChefLan.DB.Migrations.Courses
{
    /// <inheritdoc />
    public partial class AddOrdersAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "orders",
                schema: "courses",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "Goes in the bank transfer note; how a transaction finds its order"),
                    customer_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "1=Pending, 2=Paid, 3=Expired, 4=Cancelled. Expiry is lazy: Pending past expires_at reads as Expired"),
                    amount_vnd = table.Column<decimal>(type: "numeric(12,0)", precision: 12, scale: 0, nullable: false, comment: "From the server-side quote when the order was created"),
                    expires_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    paid_via = table.Column<int>(type: "integer", nullable: true, comment: "1=Webhook, 2=Manual"),
                    paid_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    paid_by_staff_id = table.Column<long>(type: "bigint", nullable: true, comment: "identity.users.id — by id only, no FK, so courses does not depend on identity"),
                    payment_transaction_id = table.Column<long>(type: "bigint", nullable: true, comment: "payment_transactions.id of the bank transaction that paid it. No FK: the two tables point at each other"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                    table.CheckConstraint("ck_orders_amount_positive", "amount_vnd > 0");
                    table.CheckConstraint("ck_orders_manual_has_reason", "paid_via IS DISTINCT FROM 2 OR paid_reason IS NOT NULL");
                    table.CheckConstraint("ck_orders_paid_has_date", "status <> 2 OR paid_at IS NOT NULL");
                    table.CheckConstraint("ck_orders_paid_via", "paid_via IS NULL OR paid_via IN (1, 2)");
                    table.CheckConstraint("ck_orders_status", "status IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "fk_orders_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "customers",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                schema: "courses",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false, comment: "1=Course, 2=Lesson"),
                    course_id = table.Column<long>(type: "bigint", nullable: true),
                    lesson_id = table.Column<long>(type: "bigint", nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    course_slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    list_price_vnd = table.Column<decimal>(type: "numeric(12,0)", precision: 12, scale: 0, nullable: false),
                    discount_vnd = table.Column<decimal>(type: "numeric(12,0)", precision: 12, scale: 0, nullable: false, defaultValue: 0m),
                    discount_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    unit_price_vnd = table.Column<decimal>(type: "numeric(12,0)", precision: 12, scale: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_items", x => x.id);
                    table.CheckConstraint("ck_order_items_kind", "kind IN (1, 2)");
                    table.CheckConstraint("ck_order_items_prices", "list_price_vnd >= 0 AND discount_vnd >= 0 AND unit_price_vnd = list_price_vnd - discount_vnd");
                    table.CheckConstraint("ck_order_items_target", "(kind = 1 AND course_id IS NOT NULL AND lesson_id IS NULL) OR (kind = 2 AND lesson_id IS NOT NULL AND course_id IS NULL)");
                    table.ForeignKey(
                        name: "fk_order_items_courses_course_id",
                        column: x => x.course_id,
                        principalSchema: "courses",
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_items_lessons_lesson_id",
                        column: x => x.lesson_id,
                        principalSchema: "courses",
                        principalTable: "lessons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_items_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "courses",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_transactions",
                schema: "courses",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    provider_transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    amount_vnd = table.Column<decimal>(type: "numeric(12,0)", precision: 12, scale: 0, nullable: false),
                    content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    transaction_date = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    raw_payload = table.Column<string>(type: "text", nullable: true, comment: "The notification body exactly as received. Not jsonb: it may not be valid JSON"),
                    order_id = table.Column<long>(type: "bigint", nullable: true),
                    match_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "1=Unmatched, 2=Matched, 3=Overpaid, 4=Underpaid, 5=LateAfterExpiry, 6=Duplicate, 7=Ignored"),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_transactions", x => x.id);
                    table.CheckConstraint("ck_payment_transactions_amount_non_negative", "amount_vnd >= 0");
                    table.CheckConstraint("ck_payment_transactions_match_status", "match_status IN (1, 2, 3, 4, 5, 6, 7)");
                    table.ForeignKey(
                        name: "fk_payment_transactions_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "courses",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_items_course_id",
                schema: "courses",
                table: "order_items",
                column: "course_id",
                filter: "course_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_order_items_lesson_id",
                schema: "courses",
                table: "order_items",
                column: "lesson_id",
                filter: "lesson_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_order_items_order_id",
                schema: "courses",
                table: "order_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_customer_id_status",
                schema: "courses",
                table: "orders",
                columns: new[] { "customer_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_orders_status_created_at",
                schema: "courses",
                table: "orders",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "uq_orders_code",
                schema: "courses",
                table: "orders",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_match_status_created_at",
                schema: "courses",
                table: "payment_transactions",
                columns: new[] { "match_status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_order_id",
                schema: "courses",
                table: "payment_transactions",
                column: "order_id",
                filter: "order_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_payment_transactions_provider_provider_transaction_id",
                schema: "courses",
                table: "payment_transactions",
                columns: new[] { "provider", "provider_transaction_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_items",
                schema: "courses");

            migrationBuilder.DropTable(
                name: "payment_transactions",
                schema: "courses");

            migrationBuilder.DropTable(
                name: "orders",
                schema: "courses");
        }
    }
}
