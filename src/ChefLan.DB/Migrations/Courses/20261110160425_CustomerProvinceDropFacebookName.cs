using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefLan.DB.Migrations.Courses
{
    /// <inheritdoc />
    public partial class CustomerProvinceDropFacebookName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "facebook_name",
                schema: "customers",
                table: "customers");

            migrationBuilder.AddColumn<string>(
                name: "province_code",
                schema: "customers",
                table: "customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                comment: "Province slug, e.g. ha-noi. The list of 34 is fixed in code, so there is no table or FK.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "province_code",
                schema: "customers",
                table: "customers");

            migrationBuilder.AddColumn<string>(
                name: "facebook_name",
                schema: "customers",
                table: "customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
