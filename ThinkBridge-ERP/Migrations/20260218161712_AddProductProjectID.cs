using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThinkBridge_ERP.Migrations
{
    /// <inheritdoc />
    public partial class AddProductProjectID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectID",
                table: "Product",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "UserID",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$HAUg98czHouH.kt4CQcZbOE2GFiwGVnZF0gMv2GVY5lokSKReiDUy");

            migrationBuilder.CreateIndex(
                name: "IX_Product_ProjectID",
                table: "Product",
                column: "ProjectID");

            migrationBuilder.AddForeignKey(
                name: "FK_Product_Project_ProjectID",
                table: "Product",
                column: "ProjectID",
                principalTable: "Project",
                principalColumn: "ProjectID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Product_Project_ProjectID",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Product_ProjectID",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "ProjectID",
                table: "Product");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "UserID",
                keyValue: 1,
                column: "Password",
                value: "Admin@123");
        }
    }
}
