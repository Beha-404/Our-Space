using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OurSpace.API.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WishlistItems_CoupleId",
                table: "WishlistItems");

            migrationBuilder.DropIndex(
                name: "IX_WishlistItems_CreatedAt",
                table: "WishlistItems");

            migrationBuilder.DropIndex(
                name: "IX_Photos_CoupleId",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_TakenAt",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Events_CoupleId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_EventDate",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_AudioMessages_CoupleId",
                table: "AudioMessages");

            migrationBuilder.DropIndex(
                name: "IX_AudioMessages_RecordedAt",
                table: "AudioMessages");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_CoupleId_IsFulfilled_CreatedAt",
                table: "WishlistItems",
                columns: new[] { "CoupleId", "IsFulfilled", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_CoupleId_TakenAt",
                table: "Photos",
                columns: new[] { "CoupleId", "TakenAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_CoupleId_EventDate",
                table: "Events",
                columns: new[] { "CoupleId", "EventDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AudioMessages_CoupleId_RecordedAt",
                table: "AudioMessages",
                columns: new[] { "CoupleId", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WishlistItems_CoupleId_IsFulfilled_CreatedAt",
                table: "WishlistItems");

            migrationBuilder.DropIndex(
                name: "IX_Photos_CoupleId_TakenAt",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Events_CoupleId_EventDate",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_AudioMessages_CoupleId_RecordedAt",
                table: "AudioMessages");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_CoupleId",
                table: "WishlistItems",
                column: "CoupleId");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_CreatedAt",
                table: "WishlistItems",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_CoupleId",
                table: "Photos",
                column: "CoupleId");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_TakenAt",
                table: "Photos",
                column: "TakenAt");

            migrationBuilder.CreateIndex(
                name: "IX_Events_CoupleId",
                table: "Events",
                column: "CoupleId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventDate",
                table: "Events",
                column: "EventDate");

            migrationBuilder.CreateIndex(
                name: "IX_AudioMessages_CoupleId",
                table: "AudioMessages",
                column: "CoupleId");

            migrationBuilder.CreateIndex(
                name: "IX_AudioMessages_RecordedAt",
                table: "AudioMessages",
                column: "RecordedAt");
        }
    }
}
