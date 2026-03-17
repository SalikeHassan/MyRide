using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Drivers.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialDriversReadDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Drivers')
                BEGIN
                    CREATE TABLE [Drivers] (
                        [Id] uniqueidentifier NOT NULL,
                        [TenantId] nvarchar(100) NOT NULL,
                        [Name] nvarchar(200) NOT NULL,
                        [Phone] nvarchar(20) NOT NULL,
                        [Status] nvarchar(450) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_Drivers] PRIMARY KEY ([Id])
                    );

                    CREATE INDEX [IX_Drivers_TenantId_Status]
                        ON [Drivers] ([TenantId], [Status]);

                    INSERT INTO [Drivers] ([Id], [CreatedAt], [Name], [Phone], [Status], [TenantId]) VALUES
                        ('aaaaaaaa-0000-0000-0000-000000000001', '2026-01-01', 'James Carter',  '+44 7700 900001', 'Available', 'tenant1'),
                        ('aaaaaaaa-0000-0000-0000-000000000002', '2026-01-01', 'Priya Sharma',  '+44 7700 900002', 'Available', 'tenant1'),
                        ('aaaaaaaa-0000-0000-0000-000000000003', '2026-01-01', 'Mohammed Ali',  '+44 7700 900003', 'Available', 'tenant1'),
                        ('aaaaaaaa-0000-0000-0000-000000000004', '2026-01-01', 'Sofia Reyes',   '+44 7700 900004', 'Available', 'tenant1'),
                        ('aaaaaaaa-0000-0000-0000-000000000005', '2026-01-01', 'Liam O''Brien', '+44 7700 900005', 'Available', 'tenant1');
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Drivers");
        }
    }
}
