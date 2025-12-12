using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymProject1.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceSalonAndAppointmentDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "Trainers",
                newName: "Name");

            // Önce SalonId kolonunu nullable olarak ekle
            migrationBuilder.AddColumn<int>(
                name: "SalonId",
                table: "Services",
                type: "int",
                nullable: true);

            // Mevcut Services kayıtlarını ilk salon ID'sine ata (eğer salon varsa)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT TOP 1 SalonId FROM Salons)
                BEGIN
                    UPDATE Services SET SalonId = (SELECT TOP 1 SalonId FROM Salons ORDER BY SalonId) WHERE SalonId IS NULL;
                END
            ");

            // Şimdi kolonu NOT NULL yap (önce index ve foreign key olmadan)
            migrationBuilder.Sql(@"
                ALTER TABLE Services ALTER COLUMN SalonId int NOT NULL;
            ");

            // Şimdi index'i oluştur
            migrationBuilder.CreateIndex(
                name: "IX_Services_SalonId",
                table: "Services",
                column: "SalonId");

            // Son olarak foreign key'i ekle
            migrationBuilder.AddForeignKey(
                name: "FK_Services_Salons_SalonId",
                table: "Services",
                column: "SalonId",
                principalTable: "Salons",
                principalColumn: "SalonId",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Services_Salons_SalonId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_SalonId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "SalonId",
                table: "Services");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Trainers",
                newName: "FullName");
        }
    }
}
