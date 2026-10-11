using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ChefLan.DB.Migrations.Courses
{
    /// <inheritdoc />
    public partial class AddLessonPriceAndLessonEnrollments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "price_vnd",
                schema: "courses",
                table: "lessons",
                type: "numeric(12,0)",
                precision: 12,
                scale: 0,
                nullable: true,
                comment: "Price to buy this lesson alone. NULL = not sold separately, only through the course");

            migrationBuilder.CreateTable(
                name: "lesson_enrollments",
                schema: "courses",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<long>(type: "bigint", nullable: false),
                    lesson_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "1=Pending, 2=Active, 3=Expired, 4=Revoked"),
                    source = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "1=AdminGrant, 2=FacebookMigration, 3=OnlineCheckout"),
                    access_granted_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamptz", nullable: true, comment: "NULL means lifetime. Snapshotted from the lesson's course at grant time."),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    paid_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    amount_paid_vnd = table.Column<decimal>(type: "numeric(12,0)", precision: 12, scale: 0, nullable: true, comment: "What was actually received — a snapshot, not the current lesson price"),
                    currency = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "VND"),
                    payment_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    payment_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    granted_by_staff_id = table.Column<long>(type: "bigint", nullable: true, comment: "identity.users.id — by id only, no FK, so courses does not depend on identity"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lesson_enrollments", x => x.id);
                    table.CheckConstraint("ck_lesson_enrollments_active_has_grant_date", "status <> 2 OR access_granted_at IS NOT NULL");
                    table.CheckConstraint("ck_lesson_enrollments_amount_non_negative", "amount_paid_vnd IS NULL OR amount_paid_vnd >= 0");
                    table.CheckConstraint("ck_lesson_enrollments_paid_has_date", "is_paid = false OR paid_at IS NOT NULL");
                    table.CheckConstraint("ck_lesson_enrollments_source", "source IN (1, 2, 3)");
                    table.CheckConstraint("ck_lesson_enrollments_status", "status IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "fk_lesson_enrollments_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "customers",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lesson_enrollments_lessons_lesson_id",
                        column: x => x.lesson_id,
                        principalSchema: "courses",
                        principalTable: "lessons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_lessons_price_non_negative",
                schema: "courses",
                table: "lessons",
                sql: "price_vnd IS NULL OR price_vnd >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_lesson_enrollments_customer_id_status",
                schema: "courses",
                table: "lesson_enrollments",
                columns: new[] { "customer_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_lesson_enrollments_lesson_id_status",
                schema: "courses",
                table: "lesson_enrollments",
                columns: new[] { "lesson_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uq_lesson_enrollments_customer_id_lesson_id",
                schema: "courses",
                table: "lesson_enrollments",
                columns: new[] { "customer_id", "lesson_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lesson_enrollments",
                schema: "courses");

            migrationBuilder.DropCheckConstraint(
                name: "ck_lessons_price_non_negative",
                schema: "courses",
                table: "lessons");

            migrationBuilder.DropColumn(
                name: "price_vnd",
                schema: "courses",
                table: "lessons");
        }
    }
}
