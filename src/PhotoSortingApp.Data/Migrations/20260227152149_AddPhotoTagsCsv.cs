using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoSortingApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoTagsCsv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TagsCsv",
                table: "PhotoAssets",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TagsCsv",
                table: "PhotoAssets");
        }
    }
}
