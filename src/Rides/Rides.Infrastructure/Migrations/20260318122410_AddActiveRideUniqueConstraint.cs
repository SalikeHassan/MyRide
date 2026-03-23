using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rides.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveRideUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_ActiveRidePerRider",
                schema: "rides",
                table: "RideReadModels",
                columns: new[] { "RiderId", "TenantId" },
                unique: true,
                filter: "[Status] IN ('Requested', 'InProgress')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ActiveRidePerRider",
                schema: "rides",
                table: "RideReadModels");
        }
    }
}
