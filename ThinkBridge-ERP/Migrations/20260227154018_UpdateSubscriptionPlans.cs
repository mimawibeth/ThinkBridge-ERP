using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThinkBridge_ERP.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSubscriptionPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SubscriptionPlan",
                keyColumn: "PlanID",
                keyValue: 1,
                column: "IsActive",
                value: false);

            migrationBuilder.UpdateData(
                table: "SubscriptionPlan",
                keyColumn: "PlanID",
                keyValue: 2,
                columns: new[] { "MaxProjects", "MaxUsers" },
                values: new object[] { 100, 35 });

            migrationBuilder.UpdateData(
                table: "SubscriptionPlan",
                keyColumn: "PlanID",
                keyValue: 3,
                columns: new[] { "MaxProjects", "MaxUsers" },
                values: new object[] { 500, 75 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SubscriptionPlan",
                keyColumn: "PlanID",
                keyValue: 1,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "SubscriptionPlan",
                keyColumn: "PlanID",
                keyValue: 2,
                columns: new[] { "MaxProjects", "MaxUsers" },
                values: new object[] { 10, 10 });

            migrationBuilder.UpdateData(
                table: "SubscriptionPlan",
                keyColumn: "PlanID",
                keyValue: 3,
                columns: new[] { "MaxProjects", "MaxUsers" },
                values: new object[] { 50, 50 });
        }
    }
}
