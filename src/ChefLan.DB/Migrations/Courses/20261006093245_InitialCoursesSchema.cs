using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ChefLan.DB.Migrations.Courses
{
    /// <inheritdoc />
    public partial class InitialCoursesSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "courses");

            migrationBuilder.EnsureSchema(
                name: "customers");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "categories",
                schema: "courses",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    slug = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    thumbnail_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
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
                    table.PrimaryKey("pk_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "courses",
                schema: "courses",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subtitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    what_you_will_learn = table.Column<string>(type: "text", nullable: true),
                    meta_description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    thumbnail_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    promo_video_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "0=Unspecified, 1=Beginner, 2=Intermediate, 3=Advanced"),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "1=Draft, 2=Published, 3=Archived"),
                    published_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    price_vnd = table.Column<decimal>(type: "numeric(12,0)", precision: 12, scale: 0, nullable: false, defaultValue: 0m),
                    compare_at_price_vnd = table.Column<decimal>(type: "numeric(12,0)", precision: 12, scale: 0, nullable: true),
                    currency = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "VND"),
                    access_duration_days = table.Column<int>(type: "integer", nullable: true, comment: "NULL means lifetime access"),
                    lesson_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_duration_seconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    enrollment_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    legacy_facebook_group_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, comment: "Bridge to the Facebook group this course used to be sold through"),
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
                    table.PrimaryKey("pk_courses", x => x.id);
                    table.CheckConstraint("ck_courses_access_duration_positive", "access_duration_days IS NULL OR access_duration_days > 0");
                    table.CheckConstraint("ck_courses_compare_at_price", "compare_at_price_vnd IS NULL OR compare_at_price_vnd >= price_vnd");
                    table.CheckConstraint("ck_courses_level", "level IN (0, 1, 2, 3)");
                    table.CheckConstraint("ck_courses_price_non_negative", "price_vnd >= 0");
                    table.CheckConstraint("ck_courses_published_has_date", "status <> 2 OR published_at IS NOT NULL");
                    table.CheckConstraint("ck_courses_status", "status IN (1, 2, 3)");
                });

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "customers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "citext", nullable: true),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "E.164, e.g. +8490..."),
                    facebook_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    facebook_profile_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    email_confirmed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    activated_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    invited_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    failed_login_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    locked_until = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_by_staff_id = table.Column<long>(type: "bigint", nullable: true, comment: "identity.users.id — by id only, no FK: it crosses a schema boundary"),
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
                    table.PrimaryKey("pk_customers", x => x.id);
                    table.CheckConstraint("ck_customers_activated_requires_password", "activated_at IS NULL OR password_hash IS NOT NULL");
                    table.CheckConstraint("ck_customers_password_requires_email", "password_hash IS NULL OR email IS NOT NULL");
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "citext", nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false, defaultValue: 2, comment: "1=Admin, 2=Staff"),
                    job_title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    must_change_password = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_login_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    failed_login_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    locked_until = table.Column<DateTime>(type: "timestamptz", nullable: true),
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
                    table.PrimaryKey("pk_users", x => x.id);
                    table.CheckConstraint("ck_users_role", "role IN (1, 2)");
                });

            migrationBuilder.CreateTable(
                name: "course_categories",
                schema: "courses",
                columns: table => new
                {
                    course_id = table.Column<long>(type: "bigint", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_course_categories", x => new { x.course_id, x.category_id });
                    table.ForeignKey(
                        name: "fk_course_categories_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "courses",
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_course_categories_courses_course_id",
                        column: x => x.course_id,
                        principalSchema: "courses",
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lessons",
                schema: "courses",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    course_id = table.Column<long>(type: "bigint", nullable: false),
                    slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    video_provider = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "0=None, 1=YouTube, 2=Vimeo, 3=Cdn, 4=Facebook"),
                    video_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    video_external_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Provider id, so the frontend can build an embed without parsing URLs"),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_free_preview = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    thumbnail_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    resources_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_lessons", x => x.id);
                    table.CheckConstraint("ck_lessons_duration_non_negative", "duration_seconds >= 0");
                    table.CheckConstraint("ck_lessons_published_has_video", "is_published = false OR video_provider = 0 OR video_url IS NOT NULL");
                    table.CheckConstraint("ck_lessons_sort_order_non_negative", "sort_order >= 0");
                    table.CheckConstraint("ck_lessons_video_provider", "video_provider IN (0, 1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "fk_lessons_courses_course_id",
                        column: x => x.course_id,
                        principalSchema: "courses",
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_refresh_tokens",
                schema: "customers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<long>(type: "bigint", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(88)", maxLength: 88, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    created_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    replaced_by_token_hash = table.Column<string>(type: "character varying(88)", maxLength: 88, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_refresh_tokens_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "customers",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "enrollments",
                schema: "courses",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<long>(type: "bigint", nullable: false),
                    course_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "1=Pending, 2=Active, 3=Expired, 4=Revoked"),
                    source = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "1=AdminGrant, 2=FacebookMigration, 3=OnlineCheckout"),
                    access_granted_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamptz", nullable: true, comment: "NULL means lifetime. Snapshotted from the course at grant time."),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    paid_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    amount_paid_vnd = table.Column<decimal>(type: "numeric(12,0)", precision: 12, scale: 0, nullable: true, comment: "What was actually received — a snapshot, not the current course price"),
                    currency = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "VND"),
                    payment_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    payment_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Bank transaction reference — the join key to a future payments table"),
                    granted_by_staff_id = table.Column<long>(type: "bigint", nullable: true, comment: "identity.users.id — by id only, no FK, so courses does not depend on identity"),
                    last_accessed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_enrollments", x => x.id);
                    table.CheckConstraint("ck_enrollments_active_has_grant_date", "status <> 2 OR access_granted_at IS NOT NULL");
                    table.CheckConstraint("ck_enrollments_amount_non_negative", "amount_paid_vnd IS NULL OR amount_paid_vnd >= 0");
                    table.CheckConstraint("ck_enrollments_paid_has_date", "is_paid = false OR paid_at IS NOT NULL");
                    table.CheckConstraint("ck_enrollments_source", "source IN (1, 2, 3)");
                    table.CheckConstraint("ck_enrollments_status", "status IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "fk_enrollments_courses_course_id",
                        column: x => x.course_id,
                        principalSchema: "courses",
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_enrollments_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "customers",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_refresh_tokens",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(88)", maxLength: 88, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    created_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    replaced_by_token_hash = table.Column<string>(type: "character varying(88)", maxLength: 88, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "uq_categories_slug",
                schema: "courses",
                table: "categories",
                column: "slug",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_course_categories_category_id_course_id",
                schema: "courses",
                table: "course_categories",
                columns: new[] { "category_id", "course_id" });

            migrationBuilder.CreateIndex(
                name: "ix_courses_status_published_at",
                schema: "courses",
                table: "courses",
                columns: new[] { "status", "published_at" },
                descending: new[] { false, true },
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "uq_courses_slug",
                schema: "courses",
                table: "courses",
                column: "slug",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_customer_refresh_tokens_customer_id_expires_at",
                schema: "customers",
                table: "customer_refresh_tokens",
                columns: new[] { "customer_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "uq_customer_refresh_tokens_token_hash",
                schema: "customers",
                table: "customer_refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customers_full_name",
                schema: "customers",
                table: "customers",
                column: "full_name",
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_customers_phone",
                schema: "customers",
                table: "customers",
                column: "phone",
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "uq_customers_email",
                schema: "customers",
                table: "customers",
                column: "email",
                unique: true,
                filter: "email IS NOT NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_enrollments_course_id_status",
                schema: "courses",
                table: "enrollments",
                columns: new[] { "course_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_enrollments_customer_id_status",
                schema: "courses",
                table: "enrollments",
                columns: new[] { "customer_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_enrollments_expires_at",
                schema: "courses",
                table: "enrollments",
                column: "expires_at",
                filter: "status = 2");

            migrationBuilder.CreateIndex(
                name: "uq_enrollments_customer_id_course_id",
                schema: "courses",
                table: "enrollments",
                columns: new[] { "customer_id", "course_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lessons_course_id_sort_order",
                schema: "courses",
                table: "lessons",
                columns: new[] { "course_id", "sort_order" },
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "uq_lessons_course_slug",
                schema: "courses",
                table: "lessons",
                columns: new[] { "course_id", "slug" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_user_refresh_tokens_user_id_expires_at",
                schema: "identity",
                table: "user_refresh_tokens",
                columns: new[] { "user_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "uq_user_refresh_tokens_token_hash",
                schema: "identity",
                table: "user_refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_role",
                schema: "identity",
                table: "users",
                column: "role",
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "uq_users_email",
                schema: "identity",
                table: "users",
                column: "email",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "course_categories",
                schema: "courses");

            migrationBuilder.DropTable(
                name: "customer_refresh_tokens",
                schema: "customers");

            migrationBuilder.DropTable(
                name: "enrollments",
                schema: "courses");

            migrationBuilder.DropTable(
                name: "lessons",
                schema: "courses");

            migrationBuilder.DropTable(
                name: "user_refresh_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "courses");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "customers");

            migrationBuilder.DropTable(
                name: "courses",
                schema: "courses");

            migrationBuilder.DropTable(
                name: "users",
                schema: "identity");
        }
    }
}
