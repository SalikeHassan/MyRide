using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rides.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialRidesReadDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RideReadModels')
                BEGIN
                    CREATE TABLE [RideReadModels] (
                        [RideId] uniqueidentifier NOT NULL,
                        [TenantId] nvarchar(100) NOT NULL,
                        [RiderId] uniqueidentifier NOT NULL,
                        [DriverId] uniqueidentifier NOT NULL,
                        [DriverName] nvarchar(200) NOT NULL,
                        [Status] nvarchar(450) NOT NULL,
                        [FareAmount] decimal(18,2) NOT NULL,
                        [FareCurrency] nvarchar(10) NOT NULL,
                        [LastUpdatedOn] datetime2 NOT NULL,
                        CONSTRAINT [PK_RideReadModels] PRIMARY KEY ([RideId])
                    );

                    CREATE INDEX [IX_RideReadModels_TenantId_Status]
                        ON [RideReadModels] ([TenantId], [Status]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RideReadModels");
        }
    }
}
