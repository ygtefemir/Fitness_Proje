using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web_Proje.Migrations
{
    /// <inheritdoc />
    public partial class dbb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
        name: "ClosingTime",
        table: "Gyms");


            migrationBuilder.AddColumn<TimeSpan>(
            name: "ClosingTime",
            table: "Gyms",
            type: "time",
            nullable: false,
            defaultValue: new TimeSpan(0, 0, 0));


        }

        /// <inheritdoc />
    }
}
