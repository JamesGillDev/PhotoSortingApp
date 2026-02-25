using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoSortingApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScanRoots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RootPath = table.Column<string>(type: "TEXT", nullable: false),
                    LastScanUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalFilesLastScan = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    EnableDuplicateDetection = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanRoots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhotoAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ScanRootId = table.Column<int>(type: "INTEGER", nullable: false),
                    FullPath = table.Column<string>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    Extension = table.Column<string>(type: "TEXT", nullable: false),
                    FolderPath = table.Column<string>(type: "TEXT", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    DateTaken = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateTakenSource = table.Column<int>(type: "INTEGER", nullable: false),
                    CameraMake = table.Column<string>(type: "TEXT", nullable: true),
                    CameraModel = table.Column<string>(type: "TEXT", nullable: true),
                    Width = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<int>(type: "INTEGER", nullable: true),
                    Sha256 = table.Column<string>(type: "TEXT", nullable: true),
                    FileCreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FileLastWriteUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IndexedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhotoAssets_ScanRoots_ScanRootId",
                        column: x => x.ScanRootId,
                        principalTable: "ScanRoots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhotoAssets_DateTaken",
                table: "PhotoAssets",
                column: "DateTaken");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoAssets_FullPath",
                table: "PhotoAssets",
                column: "FullPath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhotoAssets_ScanRootId",
                table: "PhotoAssets",
                column: "ScanRootId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoAssets_Sha256",
                table: "PhotoAssets",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_ScanRoots_RootPath",
                table: "ScanRoots",
                column: "RootPath",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhotoAssets");

            migrationBuilder.DropTable(
                name: "ScanRoots");
        }
    }
}
