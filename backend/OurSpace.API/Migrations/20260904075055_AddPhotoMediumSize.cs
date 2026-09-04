using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OurSpace.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoMediumSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MediumPath",
                table: "Photos",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediumPath",
                table: "Photos");
        }
    }
}
