using Backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261009121500_AddGameSessionQuestionStartedAt")]
public partial class AddGameSessionQuestionStartedAt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "StartedAt",
            table: "GameSessionQuestions",
            type: "timestamp with time zone",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "StartedAt",
            table: "GameSessionQuestions");
    }
}
