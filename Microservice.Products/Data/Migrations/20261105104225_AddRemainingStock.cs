using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Microservice.Products.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRemainingStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RemainingStock",
                table: "ReservationLines",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemainingStock",
                table: "ReservationLines");
        }
    }
}
