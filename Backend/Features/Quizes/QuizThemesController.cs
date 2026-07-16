using Microsoft.AspNetCore.Mvc;
using Backend.Features.Quizes.Dtos;

namespace Backend.Features.Quizes;

[ApiController]
[Route("api/quiz-themes")]
public class QuizThemesController : ControllerBase
{
    private readonly QuizCatalog _quizCatalog;

    public QuizThemesController(QuizCatalog quizCatalog)
    {
        _quizCatalog = quizCatalog;
    }

    [HttpGet]
    public IActionResult GetThemes()
    {
        return Ok(_quizCatalog.GetThemes());
    }

    [HttpPost]
    public IActionResult CreateTheme([FromBody] CreateQuizThemeRequest request)
    {
        var theme = _quizCatalog.CreateTheme(request.Name);
        return Created($"/api/quiz-themes/{theme.Id}", theme);
    }

    [HttpGet("{themeId:guid}/questions")]
    public IActionResult GetQuestions(Guid themeId)
    {
        try
        {
            return Ok(_quizCatalog.GetQuestions(themeId));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{themeId:guid}/questions")]
    public IActionResult CreateQuestion(Guid themeId, [FromBody] CreateQuestionRequest request)
    {
        try
        {
            var question = _quizCatalog.CreateQuestion(
                themeId,
                request.Text,
                request.Difficulty,
                request.Options,
                request.CorrectOptionIndex);

            return Created($"/api/quiz-themes/{themeId}/questions/{question.Id}", question);
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
