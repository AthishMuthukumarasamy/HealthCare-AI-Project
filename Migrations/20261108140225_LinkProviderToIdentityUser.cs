using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Healthcare.API.Migrations
{
    /// <inheritdoc />
    public partial class LinkProviderToIdentityUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Providers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Providers_ApplicationUserId",
                table: "Providers",
                column: "ApplicationUserId",
                unique: true,
                filter: "[ApplicationUserId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Providers_AspNetUsers_ApplicationUserId",
                table: "Providers",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Providers_AspNetUsers_ApplicationUserId",
                table: "Providers");

            migrationBuilder.DropIndex(
                name: "IX_Providers_ApplicationUserId",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Providers");
        }
    }
}
