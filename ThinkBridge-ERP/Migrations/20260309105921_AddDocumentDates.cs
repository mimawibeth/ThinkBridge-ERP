using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThinkBridge_ERP.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Document",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Document",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Document");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Document");
        }
    }
}
