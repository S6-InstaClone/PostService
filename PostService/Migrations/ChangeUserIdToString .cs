using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostService.Migrations
{
    /// <inheritdoc />
    public partial class ChangeUserIdToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Change UserId column from int to string (varchar 36 for UUID)
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Posts",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: This will fail if there are existing string UUIDs
            // You may need to handle data conversion manually
            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Posts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(36)",
                oldMaxLength: 36);
        }
    }
}