using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SportId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchRequests", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchRequests");
        }
    }
}
