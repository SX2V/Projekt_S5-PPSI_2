using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchRequestNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MatchRequests_SportId",
                table: "MatchRequests",
                column: "SportId");

            migrationBuilder.AddForeignKey(
                name: "FK_MatchRequests_Sports_SportId",
                table: "MatchRequests",
                column: "SportId",
                principalTable: "Sports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchRequests_Sports_SportId",
                table: "MatchRequests");

            migrationBuilder.DropIndex(
                name: "IX_MatchRequests_SportId",
                table: "MatchRequests");
        }
    }
}
