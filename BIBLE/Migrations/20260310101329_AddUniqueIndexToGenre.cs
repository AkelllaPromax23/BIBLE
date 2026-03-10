using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BIBLE.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToGenre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Genre_Name_Unique",
                table: "Genres",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Genre_Name_Unique",
                table: "Genres");
        }
    }
}
