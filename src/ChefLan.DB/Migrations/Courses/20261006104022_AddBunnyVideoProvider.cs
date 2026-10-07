using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefLan.DB.Migrations.Courses
{
    /// <inheritdoc />
    public partial class AddBunnyVideoProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_lessons_video_provider",
                schema: "courses",
                table: "lessons");

            migrationBuilder.AlterColumn<int>(
                name: "video_provider",
                schema: "courses",
                table: "lessons",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                comment: "0=None, 1=YouTube, 2=Vimeo, 3=Cdn, 4=Facebook, 5=Bunny",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1,
                oldComment: "0=None, 1=YouTube, 2=Vimeo, 3=Cdn, 4=Facebook");

            migrationBuilder.AddCheckConstraint(
                name: "ck_lessons_video_provider",
                schema: "courses",
                table: "lessons",
                sql: "video_provider IN (0, 1, 2, 3, 4, 5)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_lessons_video_provider",
                schema: "courses",
                table: "lessons");

            migrationBuilder.AlterColumn<int>(
                name: "video_provider",
                schema: "courses",
                table: "lessons",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                comment: "0=None, 1=YouTube, 2=Vimeo, 3=Cdn, 4=Facebook",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1,
                oldComment: "0=None, 1=YouTube, 2=Vimeo, 3=Cdn, 4=Facebook, 5=Bunny");

            migrationBuilder.AddCheckConstraint(
                name: "ck_lessons_video_provider",
                schema: "courses",
                table: "lessons",
                sql: "video_provider IN (0, 1, 2, 3, 4)");
        }
    }
}
