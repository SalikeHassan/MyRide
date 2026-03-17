using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rides.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRidesSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "rides");

            migrationBuilder.RenameTable(
                name: "RideReadModels",
                newName: "RideReadModels",
                newSchema: "rides");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "RideReadModels",
                schema: "rides",
                newName: "RideReadModels");
        }
    }
}
