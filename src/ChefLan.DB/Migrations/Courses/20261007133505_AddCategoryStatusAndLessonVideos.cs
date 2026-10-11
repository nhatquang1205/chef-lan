using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ChefLan.DB.Migrations.Courses
{
    /// <inheritdoc />
    public partial class AddCategoryStatusAndLessonVideos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_lessons_published_has_video",
                schema: "courses",
                table: "lessons");

            migrationBuilder.DropCheckConstraint(
                name: "ck_lessons_video_provider",
                schema: "courses",
                table: "lessons");

            migrationBuilder.DropColumn(
                name: "is_free_preview",
                schema: "courses",
                table: "lessons");

            migrationBuilder.DropColumn(
                name: "thumbnail_url",
                schema: "courses",
                table: "lessons");

            migrationBuilder.DropColumn(
                name: "video_external_id",
                schema: "courses",
                table: "lessons");

            migrationBuilder.DropColumn(
                name: "video_provider",
                schema: "courses",
                table: "lessons");

            migrationBuilder.DropColumn(
                name: "video_url",
                schema: "courses",
                table: "lessons");

            migrationBuilder.AlterColumn<int>(
                name: "duration_seconds",
                schema: "courses",
                table: "lessons",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Denormalized sum of published, non-deleted lesson_videos durations",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "recipes",
                schema: "courses",
                table: "lessons",
                type: "text",
                nullable: true,
                comment: "Markdown");

            migrationBuilder.AddColumn<int>(
                name: "video_count",
                schema: "courses",
                table: "lessons",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Denormalized count of published, non-deleted lesson_videos");

            migrationBuilder.AddColumn<int>(
                name: "status",
                schema: "courses",
                table: "categories",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                comment: "1=Active, 2=InActive");

            migrationBuilder.CreateTable(
                name: "lesson_videos",
                schema: "courses",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lesson_id = table.Column<long>(type: "bigint", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    provider = table.Column<int>(type: "integer", nullable: false, defaultValue: 5, comment: "0=None, 1=YouTube, 2=Vimeo, 3=Cdn, 4=Facebook, 5=Bunny"),
                    external_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Provider id (Bunny video GUID). Never handed out unsigned."),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    thumbnail_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_free = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    free_till_seconds = table.Column<int>(type: "integer", nullable: true, comment: "NULL = the whole video is free. Enforced by the frontend, not the API."),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    deleted_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lesson_videos", x => x.id);
                    table.CheckConstraint("ck_lesson_videos_duration_non_negative", "duration_seconds >= 0");
                    table.CheckConstraint("ck_lesson_videos_free_till_positive", "free_till_seconds IS NULL OR free_till_seconds > 0");
                    table.CheckConstraint("ck_lesson_videos_free_till_requires_free", "free_till_seconds IS NULL OR is_free = true");
                    table.CheckConstraint("ck_lesson_videos_provider", "provider IN (0, 1, 2, 3, 4, 5)");
                    table.CheckConstraint("ck_lesson_videos_published_has_source", "is_published = false OR provider = 0 OR external_id IS NOT NULL OR url IS NOT NULL");
                    table.CheckConstraint("ck_lesson_videos_sort_order_non_negative", "sort_order >= 0");
                    table.ForeignKey(
                        name: "fk_lesson_videos_lessons_lesson_id",
                        column: x => x.lesson_id,
                        principalSchema: "courses",
                        principalTable: "lessons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_lessons_video_count_non_negative",
                schema: "courses",
                table: "lessons",
                sql: "video_count >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_categories_status",
                schema: "courses",
                table: "categories",
                column: "status",
                filter: "is_deleted = false");

            migrationBuilder.AddCheckConstraint(
                name: "ck_categories_status",
                schema: "courses",
                table: "categories",
                sql: "status IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "ix_lesson_videos_lesson_id_sort_order",
                schema: "courses",
                table: "lesson_videos",
                columns: new[] { "lesson_id", "sort_order" },
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lesson_videos",
                schema: "courses");

            migrationBuilder.DropCheckConstraint(
                name: "ck_lessons_video_count_non_negative",
                schema: "courses",
                table: "lessons");

            migrationBuilder.DropIndex(
                name: "ix_categories_status",
                schema: "courses",
                table: "categories");

            migrationBuilder.DropCheckConstraint(
                name: "ck_categories_status",
                schema: "courses",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "recipes",
                schema: "courses",
                table: "lessons");

            migrationBuilder.DropColumn(
                name: "video_count",
                schema: "courses",
                table: "lessons");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "courses",
                table: "categories");

            migrationBuilder.AlterColumn<int>(
                name: "duration_seconds",
                schema: "courses",
                table: "lessons",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0,
                oldComment: "Denormalized sum of published, non-deleted lesson_videos durations");

            migrationBuilder.AddColumn<bool>(
                name: "is_free_preview",
                schema: "courses",
                table: "lessons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "thumbnail_url",
                schema: "courses",
                table: "lessons",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "video_external_id",
                schema: "courses",
                table: "lessons",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "Provider id, so the frontend can build an embed without parsing URLs");

            migrationBuilder.AddColumn<int>(
                name: "video_provider",
                schema: "courses",
                table: "lessons",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                comment: "0=None, 1=YouTube, 2=Vimeo, 3=Cdn, 4=Facebook, 5=Bunny");

            migrationBuilder.AddColumn<string>(
                name: "video_url",
                schema: "courses",
                table: "lessons",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_lessons_published_has_video",
                schema: "courses",
                table: "lessons",
                sql: "is_published = false OR video_provider = 0 OR video_url IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_lessons_video_provider",
                schema: "courses",
                table: "lessons",
                sql: "video_provider IN (0, 1, 2, 3, 4, 5)");
        }
    }
}
