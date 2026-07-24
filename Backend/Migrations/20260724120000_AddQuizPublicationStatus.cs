using Backend.Data;
using Backend.Features.Quizes;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260724120000_AddQuizPublicationStatus")]
public partial class AddQuizPublicationStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Status",
            table: "Quizes",
            type: "integer",
            nullable: false,
            defaultValue: (int)QuizStatus.Published);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Status",
            table: "Quizes");
    }
}
