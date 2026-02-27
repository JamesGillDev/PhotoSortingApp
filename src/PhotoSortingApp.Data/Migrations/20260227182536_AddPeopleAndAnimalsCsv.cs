using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoSortingApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPeopleAndAnimalsCsv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnimalsCsv",
                table: "PhotoAssets",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PeopleCsv",
                table: "PhotoAssets",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnimalsCsv",
                table: "PhotoAssets");

            migrationBuilder.DropColumn(
                name: "PeopleCsv",
                table: "PhotoAssets");
        }
    }
}
