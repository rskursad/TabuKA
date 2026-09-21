using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TabuKA.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RoundTimeSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    PassLimit = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ScoreToWin = table.Column<int>(type: "INTEGER", nullable: false),
                    UseCustomCategories = table.Column<bool>(type: "INTEGER", nullable: false),
                    SelectedCategoryIdsJson = table.Column<string>(type: "TEXT", nullable: false),
                    EnableSound = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableAutoTabooCheck = table.Column<bool>(type: "INTEGER", nullable: false),
                    Volume = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentRound = table.Column<int>(type: "INTEGER", nullable: false),
                    PassUsed = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Words",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MainWord = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ForbiddenWordsJson = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    Difficulty = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Words", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Words_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameSettingsId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CurrentTeamIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentRound = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalRounds = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    WinnerTeamId = table.Column<string>(type: "TEXT", nullable: true),
                    GameDataJson = table.Column<string>(type: "TEXT", nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameSessions_GameSettings_GameSettingsId",
                        column: x => x.GameSettingsId,
                        principalTable: "GameSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameSessions_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rounds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    WordId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoundNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Result = table.Column<int>(type: "INTEGER", nullable: false),
                    ScoreGained = table.Column<int>(type: "INTEGER", nullable: false),
                    PassUsed = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeUsedSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rounds_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Rounds_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Rounds_Words_WordId",
                        column: x => x.WordId,
                        principalTable: "Words",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_AppSettings_Key",
                table: "AppSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_GameSettingsId",
                table: "GameSessions",
                column: "GameSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_TeamId",
                table: "GameSessions",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamId",
                table: "Players",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_GameSessionId",
                table: "Rounds",
                column: "GameSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_TeamId",
                table: "Rounds",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_WordId",
                table: "Rounds",
                column: "WordId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_Name",
                table: "Teams",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Words_CategoryId",
                table: "Words",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Words_MainWord_CategoryId",
                table: "Words",
                columns: new[] { "MainWord", "CategoryId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Rounds");

            migrationBuilder.DropTable(
                name: "GameSessions");

            migrationBuilder.DropTable(
                name: "Words");

            migrationBuilder.DropTable(
                name: "GameSettings");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
