using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyRide.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialOrchestratorDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "orchestrator");

            migrationBuilder.CreateTable(
                name: "CompleteRideSagas",
                schema: "orchestrator",
                columns: table => new
                {
                    SagaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FareAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FareCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompleteRideSagas", x => x.SagaId);
                });

            migrationBuilder.CreateTable(
                name: "StartRideSagas",
                schema: "orchestrator",
                columns: table => new
                {
                    SagaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriverName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FareAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FareCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PickupLat = table.Column<double>(type: "float", nullable: false),
                    PickupLng = table.Column<double>(type: "float", nullable: false),
                    DropoffLat = table.Column<double>(type: "float", nullable: false),
                    DropoffLng = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StartRideSagas", x => x.SagaId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompleteRideSagas",
                schema: "orchestrator");

            migrationBuilder.DropTable(
                name: "StartRideSagas",
                schema: "orchestrator");
        }
    }
}
