using Backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260717150000_NormalizeQuestionDifficultyScale")]
public partial class NormalizeQuestionDifficultyScale : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "Questions"
            SET "Difficulty" = CASE "Difficulty"
                WHEN 0 THEN 100
                WHEN 1 THEN 200
                WHEN 2 THEN 300
                WHEN 3 THEN 400
                ELSE "Difficulty"
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "Questions"
            SET "Difficulty" = CASE "Difficulty"
                WHEN 100 THEN 0
                WHEN 200 THEN 1
                WHEN 300 THEN 2
                WHEN 400 THEN 3
                ELSE "Difficulty"
            END;
            """);
    }
}
