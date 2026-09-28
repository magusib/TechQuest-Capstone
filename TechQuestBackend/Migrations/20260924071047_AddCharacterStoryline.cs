using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechQuestBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterStoryline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Storyline",
                table: "Shop_items",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Storyline",
                table: "Shop_items");
        }
    }
}
