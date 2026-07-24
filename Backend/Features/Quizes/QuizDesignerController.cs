using Microsoft.AspNetCore.Authorization;
using Backend.Features.Quizes.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Backend.Features.Quizes;

[ApiController]
[Route("api/v1/admin/quizes")]
[Authorize(Policy = "Authoring")]
public class QuizDesignerController(QuizDesigner quizDesigner) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<QuizListItemDto>>> GetQuizes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await quizDesigner.GetQuizesAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{quizId:guid}")]
    public async Task<ActionResult<AdminQuizDto>> GetQuiz(
        Guid quizId,
        CancellationToken cancellationToken)
    {
        return Ok(await quizDesigner.GetQuizAsync(quizId, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<AdminQuizDto>> CreateQuiz(
        [FromBody] CreateQuizRequest request,
        CancellationToken cancellationToken)
    {
        var quiz = await quizDesigner.CreateQuizAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetQuiz), new { quizId = quiz.Id }, quiz);
    }

    [HttpPut("{quizId:guid}")]
    public async Task<ActionResult<AdminQuizDto>> UpdateQuiz(
        Guid quizId,
        [FromBody] UpdateQuizRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await quizDesigner.UpdateQuizAsync(quizId, request, cancellationToken));
    }

    [HttpPost("{quizId:guid}/publish")]
    public async Task<ActionResult<AdminQuizDto>> PublishQuiz(
        Guid quizId,
        CancellationToken cancellationToken)
    {
        return Ok(await quizDesigner.PublishQuizAsync(quizId, cancellationToken));
    }

    [HttpPost("{quizId:guid}/archive")]
    public async Task<ActionResult<AdminQuizDto>> ArchiveQuiz(
        Guid quizId,
        CancellationToken cancellationToken)
    {
        return Ok(await quizDesigner.ArchiveQuizAsync(quizId, cancellationToken));
    }

    [HttpPost("{quizId:guid}/draft")]
    public async Task<ActionResult<AdminQuizDto>> MoveQuizToDraft(
        Guid quizId,
        CancellationToken cancellationToken)
    {
        return Ok(await quizDesigner.MoveQuizToDraftAsync(quizId, cancellationToken));
    }

    [HttpDelete("{quizId:guid}")]
    public async Task<IActionResult> DeleteQuiz(Guid quizId, CancellationToken cancellationToken)
    {
        await quizDesigner.DeleteQuizAsync(quizId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{quizId:guid}/export")]
    public async Task<IActionResult> ExportQuiz(Guid quizId, CancellationToken cancellationToken)
    {
        var document = await quizDesigner.ExportQuizAsync(quizId, cancellationToken);
        var json = JsonSerializer.Serialize(document, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
        });

        return File(
            System.Text.Encoding.UTF8.GetBytes(json),
            "application/json",
            $"quiz-{quizId}.json");
    }

    [HttpPost("import/validate")]
    public ActionResult<QuizImportValidationDto> ValidateImport([FromBody] QuizImportDto document)
    {
        return Ok(quizDesigner.ValidateImport(document));
    }

    [HttpPost("import")]
    public async Task<ActionResult<AdminQuizDto>> ImportQuiz(
        [FromBody] QuizImportDto document,
        CancellationToken cancellationToken)
    {
        var quiz = await quizDesigner.ImportQuizAsync(document, cancellationToken);
        return CreatedAtAction(nameof(GetQuiz), new { quizId = quiz.Id }, quiz);
    }
}
