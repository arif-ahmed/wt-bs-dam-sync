using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandShareDAMSync.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeletionPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DirectoryDeletionPolicy",
                table: "Jobs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FileDeletionPolicy",
                table: "Jobs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DirectoryDeletionPolicy",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "FileDeletionPolicy",
                table: "Jobs");
        }
    }
}
