using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefLan.DB.Migrations.Courses
{
    /// <inheritdoc />
    public partial class AddLessonIncludedInCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "included_in_course",
                schema: "courses",
                table: "lessons",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "true = part of the course price; false = add-on, bought separately (requires price_vnd)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_lessons_addon_has_price",
                schema: "courses",
                table: "lessons",
                sql: "included_in_course OR price_vnd IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_lessons_addon_has_price",
                schema: "courses",
                table: "lessons");

            migrationBuilder.DropColumn(
                name: "included_in_course",
                schema: "courses",
                table: "lessons");
        }
    }
}
