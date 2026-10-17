using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefLan.DB.Migrations.Courses
{
    /// <inheritdoc />
    public partial class AddCourseIncludes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "discount_reason",
                schema: "courses",
                table: "enrollments",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_vnd",
                schema: "courses",
                table: "enrollments",
                type: "numeric(12,0)",
                precision: 12,
                scale: 0,
                nullable: true,
                comment: "Included-course discount applied at grant time");

            migrationBuilder.AddColumn<decimal>(
                name: "list_price_vnd",
                schema: "courses",
                table: "enrollments",
                type: "numeric(12,0)",
                precision: 12,
                scale: 0,
                nullable: true,
                comment: "The course price at grant time, before any included-course discount");

            migrationBuilder.CreateTable(
                name: "course_includes",
                schema: "courses",
                columns: table => new
                {
                    course_id = table.Column<long>(type: "bigint", nullable: false),
                    included_course_id = table.Column<long>(type: "bigint", nullable: false),
                    discount_vnd = table.Column<decimal>(type: "numeric(12,0)", precision: 12, scale: 0, nullable: false, defaultValue: 0m, comment: "Taken off the parent's price when the customer already has the included course"),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_course_includes", x => new { x.course_id, x.included_course_id });
                    table.CheckConstraint("ck_course_includes_discount_non_negative", "discount_vnd >= 0");
                    table.CheckConstraint("ck_course_includes_not_self", "course_id <> included_course_id");
                    table.ForeignKey(
                        name: "fk_course_includes_courses_course_id",
                        column: x => x.course_id,
                        principalSchema: "courses",
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_course_includes_courses_included_course_id",
                        column: x => x.included_course_id,
                        principalSchema: "courses",
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_course_includes_included_course_id",
                schema: "courses",
                table: "course_includes",
                column: "included_course_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "course_includes",
                schema: "courses");

            migrationBuilder.DropColumn(
                name: "discount_reason",
                schema: "courses",
                table: "enrollments");

            migrationBuilder.DropColumn(
                name: "discount_vnd",
                schema: "courses",
                table: "enrollments");

            migrationBuilder.DropColumn(
                name: "list_price_vnd",
                schema: "courses",
                table: "enrollments");
        }
    }
}
