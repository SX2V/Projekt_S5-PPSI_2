using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportConnect.API.Migrations
{
    public partial class ForceFixTrainingDateTime2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Usuń default, jeśli istnieje
            migrationBuilder.Sql(
                "ALTER TABLE \"TrainingRequests\" ALTER COLUMN \"TrainingDateTime\" DROP DEFAULT;"
            );

            // Usuń NOT NULL, jeśli istnieje
            migrationBuilder.Sql(
                "ALTER TABLE \"TrainingRequests\" ALTER COLUMN \"TrainingDateTime\" DROP NOT NULL;"
            );

            // Wymuś zmianę typu NA SIŁĘ
            migrationBuilder.Sql(
                "ALTER TABLE \"TrainingRequests\" ALTER COLUMN \"TrainingDateTime\" TYPE timestamp without time zone USING \"TrainingDateTime\"::timestamp;"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"TrainingRequests\" ALTER COLUMN \"TrainingDateTime\" TYPE timestamp with time zone USING \"TrainingDateTime\"::timestamptz;"
            );
        }
    }
}
