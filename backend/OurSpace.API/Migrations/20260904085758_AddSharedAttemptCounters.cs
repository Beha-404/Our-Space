using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OurSpace.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedAttemptCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttemptCounters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Bucket = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WindowStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttemptCounters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttemptCounters_Bucket_WindowStart",
                table: "AttemptCounters",
                columns: new[] { "Bucket", "WindowStart" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttemptCounters");
        }
    }
}
