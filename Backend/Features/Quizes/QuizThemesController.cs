using Microsoft.AspNetCore.Mvc;
using Backend.Features.Quizes.Dtos;

namespace Backend.Features.Quizes;

[ApiController]
[Route("api/v1/quiz-themes")]
public class QuizThemesController : ControllerBase
{
    private readonly QuizCatalog _quizCatalog;

    public QuizThemesController(QuizCatalog quizCatalog)
    {
        _quizCatalog = quizCatalog;
    }

    [HttpGet]
    public async Task<IActionResult> GetThemes(CancellationToken cancellationToken)
    {
        return Ok(await _quizCatalog.GetThemesAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateTheme(
        [FromBody] CreateQuizThemeRequest request,
        CancellationToken cancellationToken)
    {
        var theme = await _quizCatalog.CreateThemeAsync(request.Name, cancellationToken);
        return Created($"/api/v1/quiz-themes/{theme.Id}", theme);
    }

    [HttpGet("{themeId:guid}/questions")]
    public async Task<IActionResult> GetQuestions(Guid themeId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _quizCatalog.GetQuestionsAsync(themeId, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{themeId:guid}/questions")]
    public async Task<IActionResult> CreateQuestion(
        Guid themeId,
        [FromBody] CreateQuestionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var question = await _quizCatalog.CreateQuestionAsync(
                themeId,
                request.Text,
                request.Difficulty,
                request.Options,
                request.CorrectOptionIndex,
                cancellationToken);

            return Created($"/api/v1/quiz-themes/{themeId}/questions/{question.Id}", question);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
