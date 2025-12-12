using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymProject1.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailabilityHoursToTrainer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvailabilityHours",
                table: "Trainers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailabilityHours",
                table: "Trainers");
        }
    }
}

