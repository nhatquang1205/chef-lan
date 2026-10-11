using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefLan.DB.Migrations.Courses
{
    /// <inheritdoc />
    public partial class AddLessonVideoPublicId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "public_id",
                schema: "courses",
                table: "lesson_videos",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValueSql: "left(replace(gen_random_uuid()::text, '-', ''), 16)",
                comment: "Opaque id for public URLs and the public API; never the integer id or the Bunny GUID");

            migrationBuilder.CreateIndex(
                name: "uq_lesson_videos_public_id",
                schema: "courses",
                table: "lesson_videos",
                column: "public_id",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_lesson_videos_public_id",
                schema: "courses",
                table: "lesson_videos");

            migrationBuilder.DropColumn(
                name: "public_id",
                schema: "courses",
                table: "lesson_videos");
        }
    }
}
