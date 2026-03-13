using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThinkBridge_ERP.Migrations
{
    /// <inheritdoc />
    public partial class BackfillPublishedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill PublishedAt for existing approved articles using their CreatedAt date
            migrationBuilder.Sql(
                "UPDATE Document SET PublishedAt = CreatedAt WHERE ApprovalStatus = 'Approved' AND PublishedAt IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
