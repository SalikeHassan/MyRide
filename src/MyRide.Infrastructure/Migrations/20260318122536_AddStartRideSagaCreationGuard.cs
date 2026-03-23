using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyRide.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStartRideSagaCreationGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_ActiveStartRideSagaPerRider",
                schema: "orchestrator",
                table: "StartRideSagas",
                columns: new[] { "RiderId", "TenantId" },
                unique: true,
                filter: "[Status] IN ('Pending', 'DriverAssigned')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ActiveStartRideSagaPerRider",
                schema: "orchestrator",
                table: "StartRideSagas");
        }
    }
}
