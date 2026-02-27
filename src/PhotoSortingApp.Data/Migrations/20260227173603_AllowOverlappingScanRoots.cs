using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoSortingApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowOverlappingScanRoots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PhotoAssets_FullPath",
                table: "PhotoAssets");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoAssets_FullPath",
                table: "PhotoAssets",
                column: "FullPath");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoAssets_ScanRootId_FullPath",
                table: "PhotoAssets",
                columns: new[] { "ScanRootId", "FullPath" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PhotoAssets_FullPath",
                table: "PhotoAssets");

            migrationBuilder.DropIndex(
                name: "IX_PhotoAssets_ScanRootId_FullPath",
                table: "PhotoAssets");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoAssets_FullPath",
                table: "PhotoAssets",
                column: "FullPath",
                unique: true);
        }
    }
}
