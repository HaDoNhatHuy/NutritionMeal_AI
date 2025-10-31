using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutritionWebApp.Migrations
{
    /// <inheritdoc />
    public partial class fixExerciseVideo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "ExerciseVideo");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "ExerciseVideo");

            migrationBuilder.RenameColumn(
                name: "VideoUrl",
                table: "ExerciseVideo",
                newName: "YoutubeVideoUrl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "YoutubeVideoUrl",
                table: "ExerciseVideo",
                newName: "VideoUrl");

            migrationBuilder.AddColumn<string>(
                name: "Duration",
                table: "ExerciseVideo",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "ExerciseVideo",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
