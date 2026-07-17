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
        var themes = await _quizCatalog.GetThemesAsync(cancellationToken);
        return Ok(themes.Select(QuizMapper.ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> CreateTheme(
        [FromBody] CreateQuizThemeRequest request,
        CancellationToken cancellationToken)
    {
        var theme = await _quizCatalog.CreateThemeAsync(request.Name, cancellationToken);
        return Created($"/api/v1/quiz-themes/{theme.Id}", QuizMapper.ToDto(theme));
    }

    [HttpGet("{themeId:guid}/questions")]
    public async Task<IActionResult> GetQuestions(Guid themeId, CancellationToken cancellationToken)
    {
        var questions = await _quizCatalog.GetQuestionsAsync(themeId, cancellationToken);
        return Ok(questions.Select(QuizMapper.ToDto));
    }

    [HttpPost("{themeId:guid}/questions")]
    public async Task<IActionResult> CreateQuestion(
        Guid themeId,
        [FromBody] CreateQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var question = await _quizCatalog.CreateQuestionAsync(
            themeId,
            request.Text,
            request.CodeContext,
            request.Explanation,
            request.Difficulty,
            request.Options,
            request.CorrectOptionIndex,
            cancellationToken);

        return Created($"/api/v1/quiz-themes/{themeId}/questions/{question.Id}", QuizMapper.ToDto(question));
    }
}
