using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiUsuarios.Migrations
{
    /// <inheritdoc />
    public partial class AddLatLngToLectura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
                migrationBuilder.AddColumn<double>(
                name: "Lat",
                table: "Lecturas",
                type: "float",
                nullable: true);

                migrationBuilder.AddColumn<double>(
                name: "Lng",
                table: "Lecturas",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
            name: "Lat",
            table: "Lecturas");

            migrationBuilder.DropColumn(
            name: "Lng",
            table: "Lecturas");
        }
    }
}
