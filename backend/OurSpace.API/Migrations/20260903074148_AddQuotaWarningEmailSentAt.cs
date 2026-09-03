using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OurSpace.API.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotaWarningEmailSentAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "QuotaWarningEmailSentAt",
                table: "Couples",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuotaWarningEmailSentAt",
                table: "Couples");
        }
    }
}
