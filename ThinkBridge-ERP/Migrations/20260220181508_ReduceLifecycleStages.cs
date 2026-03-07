using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ThinkBridge_ERP.Migrations
{
    /// <inheritdoc />
    public partial class ReduceLifecycleStages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Move any products on removed stages to Production (stage 5)
            migrationBuilder.Sql(
                "UPDATE ProductHistory SET StageID = 5 WHERE StageID IN (6, 7)");
            migrationBuilder.Sql(
                "UPDATE Product SET Status = 'Production' WHERE Status IN ('Maintenance', 'End of Life')");

            migrationBuilder.DeleteData(
                table: "LifecycleStage",
                keyColumn: "StageID",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "LifecycleStage",
                keyColumn: "StageID",
                keyValue: 7);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "LifecycleStage",
                columns: new[] { "StageID", "StageName", "StageOrder" },
                values: new object[,]
                {
                    { 6, "Maintenance", 6 },
                    { 7, "End of Life", 7 }
                });
        }
    }
}
