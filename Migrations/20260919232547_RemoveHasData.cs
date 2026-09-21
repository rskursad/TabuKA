using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TabuKA.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHasData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "GameSettings",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "GameSettings",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "GameSettings",
                keyColumn: "Id",
                keyValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AppSettings",
                columns: new[] { "Id", "CreatedAt", "Description", "Key", "Type", "UpdatedAt", "Value" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 19, 23, 21, 27, 680, DateTimeKind.Utc).AddTicks(2672), "Uygulama teması (Light/Dark/System)", "Theme", 0, null, "System" },
                    { 2, new DateTime(2026, 9, 19, 23, 21, 27, 680, DateTimeKind.Utc).AddTicks(4400), "Uygulama dili", "Language", 0, null, "tr-TR" },
                    { 3, new DateTime(2026, 9, 19, 23, 21, 27, 680, DateTimeKind.Utc).AddTicks(4402), "Genel ses seviyesi (0-100)", "MasterVolume", 1, null, "80" },
                    { 4, new DateTime(2026, 9, 19, 23, 21, 27, 680, DateTimeKind.Utc).AddTicks(4404), "Otomatik kaydetme aktif", "AutoSave", 2, null, "true" },
                    { 5, new DateTime(2026, 9, 19, 23, 21, 27, 680, DateTimeKind.Utc).AddTicks(4405), "İlk açılışta öğretici göster", "ShowTutorial", 2, null, "true" },
                    { 6, new DateTime(2026, 9, 19, 23, 21, 27, 680, DateTimeKind.Utc).AddTicks(4408), "Veritabanı şema sürümü", "DatabaseVersion", 1, null, "1" }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 19, 23, 21, 27, 678, DateTimeKind.Utc).AddTicks(1496), "Genel kelimeler", true, "Genel", 1 },
                    { 2, new DateTime(2026, 9, 19, 23, 21, 27, 678, DateTimeKind.Utc).AddTicks(3506), "Hayvanlarla ilgili kelimeler", true, "Hayvanlar", 2 },
                    { 3, new DateTime(2026, 9, 19, 23, 21, 27, 678, DateTimeKind.Utc).AddTicks(3510), "Yiyecek ve içeceklerle ilgili kelimeler", true, "Yiyecekler", 3 },
                    { 4, new DateTime(2026, 9, 19, 23, 21, 27, 678, DateTimeKind.Utc).AddTicks(3512), "Günlük eşyalar", true, "Eşyalar", 4 },
                    { 5, new DateTime(2026, 9, 19, 23, 21, 27, 678, DateTimeKind.Utc).AddTicks(3514), "Meslek ve işlerle ilgili kelimeler", true, "Meslekler", 5 },
                    { 6, new DateTime(2026, 9, 19, 23, 21, 27, 678, DateTimeKind.Utc).AddTicks(3523), "Yer ve mekanlarla ilgili kelimeler", true, "Yerler", 6 },
                    { 7, new DateTime(2026, 9, 19, 23, 21, 27, 678, DateTimeKind.Utc).AddTicks(3524), "Fiiller ve eylemler", true, "Eylemler", 7 },
                    { 8, new DateTime(2026, 9, 19, 23, 21, 27, 678, DateTimeKind.Utc).AddTicks(3526), "Doğa olayları ve öğeleri", true, "Doğa", 8 },
                    { 9, new DateTime(2026, 9, 19, 23, 21, 27, 678, DateTimeKind.Utc).AddTicks(3527), "Teknoloji ve bilgisayar terimleri", true, "Teknoloji", 9 },
                    { 10, new DateTime(2026, 9, 19, 23, 21, 27, 678, DateTimeKind.Utc).AddTicks(3529), "Spor dalları ve terimleri", true, "Spor", 10 }
                });

            migrationBuilder.InsertData(
                table: "GameSettings",
                columns: new[] { "Id", "CreatedAt", "EnableAutoTabooCheck", "EnableSound", "IsDefault", "Name", "PassLimit", "RoundTimeSeconds", "ScoreToWin", "SelectedCategoryIdsJson", "TeamCount", "UpdatedAt", "UseCustomCategories", "Volume" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 19, 23, 21, 27, 679, DateTimeKind.Utc).AddTicks(2712), false, true, true, "Varsayılan", 3, 60, 0, "[]", 2, null, false, 80 },
                    { 2, new DateTime(2026, 9, 19, 23, 21, 27, 679, DateTimeKind.Utc).AddTicks(6295), false, true, false, "Hızlı Oyun", 1, 30, 10, "[]", 2, null, false, 80 },
                    { 3, new DateTime(2026, 9, 19, 23, 21, 27, 679, DateTimeKind.Utc).AddTicks(6299), false, true, false, "Uzun Oyun", 5, 90, 25, "[]", 2, null, false, 80 }
                });
        }
    }
}
