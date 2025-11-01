using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutritionWebApp.Migrations
{
    /// <inheritdoc />
    public partial class addToExercise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Duration",
                table: "ExerciseVideo",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "ExerciseVideo",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "ExerciseVideo");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "ExerciseVideo");
        }
    }
}
