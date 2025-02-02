using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inventory_TrainerId",
                table: "Inventory");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_TrainerId",
                table: "Inventory",
                column: "TrainerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inventory_TrainerId",
                table: "Inventory");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_TrainerId",
                table: "Inventory",
                column: "TrainerId");
        }
    }
}
