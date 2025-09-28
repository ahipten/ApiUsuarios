using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiUsuarios.Migrations
{
    /// <inheritdoc />
    public partial class AddCamposLectura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "Lng",
                table: "Lecturas",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "Lat",
                table: "Lecturas",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AddColumn<double>(
                name: "IndiceSequia",
                table: "Lecturas",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MateriaOrganica",
                table: "Lecturas",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetodoRiego",
                table: "Lecturas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "pH_Suelo",
                table: "Lecturas",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IndiceSequia",
                table: "Lecturas");

            migrationBuilder.DropColumn(
                name: "MateriaOrganica",
                table: "Lecturas");

            migrationBuilder.DropColumn(
                name: "MetodoRiego",
                table: "Lecturas");

            migrationBuilder.DropColumn(
                name: "pH_Suelo",
                table: "Lecturas");

            migrationBuilder.AlterColumn<double>(
                name: "Lng",
                table: "Lecturas",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Lat",
                table: "Lecturas",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);
        }
    }
}
