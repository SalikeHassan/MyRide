using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Drivers.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDriversSchemaAndCheckpoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "drivers");

            migrationBuilder.RenameTable(
                name: "Drivers",
                newName: "Drivers",
                newSchema: "drivers");

            migrationBuilder.CreateTable(
                name: "ProjectionCheckpoints",
                schema: "drivers",
                columns: table => new
                {
                    ProjectionName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CommitPosition = table.Column<long>(type: "bigint", nullable: false),
                    PreparePosition = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectionCheckpoints", x => x.ProjectionName);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectionCheckpoints",
                schema: "drivers");

            migrationBuilder.RenameTable(
                name: "Drivers",
                schema: "drivers",
                newName: "Drivers");
        }
    }
}
