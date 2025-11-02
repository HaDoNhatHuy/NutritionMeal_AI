using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutritionWebApp.Migrations
{
    /// <inheritdoc />
    public partial class addpathologyToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Pathology",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pathology",
                table: "Users");
        }
    }
}
