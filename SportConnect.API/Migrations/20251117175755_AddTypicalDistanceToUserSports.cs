using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTypicalDistanceToUserSports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TypicalDistanceKm",
                table: "UserSports",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TypicalDistanceKm",
                table: "UserSports");
        }
    }
}
