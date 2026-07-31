using Backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260731120000_AddAnswerOptionDisplayOrder")]
public partial class AddAnswerOptionDisplayOrder : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "DisplayOrder",
            table: "AnswerOptions",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql("""
            WITH ordered_options AS (
                SELECT "Id", ROW_NUMBER() OVER (
                    PARTITION BY "QuestionId"
                    ORDER BY "Id") - 1 AS "DisplayOrder"
                FROM "AnswerOptions"
            )
            UPDATE "AnswerOptions" AS option
            SET "DisplayOrder" = ordered_options."DisplayOrder"
            FROM ordered_options
            WHERE option."Id" = ordered_options."Id";
            """);

        migrationBuilder.CreateIndex(
            name: "IX_AnswerOptions_QuestionId_DisplayOrder",
            table: "AnswerOptions",
            columns: new[] { "QuestionId", "DisplayOrder" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AnswerOptions_QuestionId_DisplayOrder",
            table: "AnswerOptions");

        migrationBuilder.DropColumn(
            name: "DisplayOrder",
            table: "AnswerOptions");
    }
}
