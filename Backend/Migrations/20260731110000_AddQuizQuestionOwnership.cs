using Backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260731110000_AddQuizQuestionOwnership")]
public partial class AddQuizQuestionOwnership : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "QuizQuestions",
            columns: table => new
            {
                QuizId = table.Column<Guid>(type: "uuid", nullable: false),
                QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_QuizQuestions", x => new { x.QuizId, x.QuestionId });
                table.ForeignKey(
                    name: "FK_QuizQuestions_Quizes_QuizId",
                    column: x => x.QuizId,
                    principalTable: "Quizes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_QuizQuestions_Questions_QuestionId",
                    column: x => x.QuestionId,
                    principalTable: "Questions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_QuizQuestions_QuestionId",
            table: "QuizQuestions",
            column: "QuestionId");

        migrationBuilder.Sql("""
            INSERT INTO "QuizQuestions" ("QuizId", "QuestionId")
            SELECT q."Id", question."Id"
            FROM "Quizes" AS q
            INNER JOIN "Questions" AS question ON question."ThemeId" = q."ThemeId"
            ON CONFLICT ("QuizId", "QuestionId") DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "QuizQuestions");
    }
}
