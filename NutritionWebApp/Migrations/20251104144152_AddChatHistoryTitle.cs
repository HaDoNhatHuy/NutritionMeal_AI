using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutritionWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddChatHistoryTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "ChatHistory",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "ChatHistory");
        }
    }
}
