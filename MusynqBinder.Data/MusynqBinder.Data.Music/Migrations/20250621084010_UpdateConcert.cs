using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConcertTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateConcert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArtistId",
                table: "Concerts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArtistId",
                table: "Concerts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
