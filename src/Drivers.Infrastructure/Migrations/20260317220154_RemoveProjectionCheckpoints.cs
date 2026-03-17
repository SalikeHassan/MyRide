using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Drivers.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProjectionCheckpoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectionCheckpoints",
                schema: "drivers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
