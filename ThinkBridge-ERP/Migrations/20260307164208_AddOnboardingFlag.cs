using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThinkBridge_ERP.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasCompletedOnboarding",
                table: "User",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "UserID",
                keyValue: 1,
                column: "HasCompletedOnboarding",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasCompletedOnboarding",
                table: "User");
        }
    }
}
