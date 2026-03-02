using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoSortingApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationIdsCsv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocationsCsv",
                table: "PhotoAssets",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocationsCsv",
                table: "PhotoAssets");
        }
    }
}
