using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefLan.DB.Migrations.Courses
{
    /// <inheritdoc />
    public partial class AddCourseIsSharedModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_shared_module",
                schema: "courses",
                table: "courses",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "A building block that other courses include; hidden from the public course list");

            migrationBuilder.Sql(
                """
                UPDATE courses.courses
                SET is_shared_module = true
                WHERE id IN (SELECT included_course_id FROM courses.course_includes);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_shared_module",
                schema: "courses",
                table: "courses");
        }
    }
}
