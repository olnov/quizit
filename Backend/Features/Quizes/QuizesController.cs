using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using Backend.Features.Quizes.Dtos;

namespace Backend.Features.Quizes;

[ApiController]
[Route("api/v1/quizes")]
public class QuizesController : ControllerBase
{
    private readonly QuizCatalog _quizCatalog;

    public QuizesController(QuizCatalog quizCatalog)
    {
        _quizCatalog = quizCatalog;
    }

    [HttpGet]
    public async Task<IActionResult> GetQuizes(CancellationToken cancellationToken)
    {
        var quizes = await _quizCatalog.GetQuizesAsync(cancellationToken);
        return Ok(quizes.Select(QuizMapper.ToDto));
    }

    [HttpPost]
    [Authorize(Policy = "Authoring")]
    public async Task<IActionResult> CreateQuiz(
        [FromBody] CreateQuizRequest request,
        CancellationToken cancellationToken)
    {
        var quiz = await _quizCatalog.CreateQuizAsync(
            request.Title,
            request.ThemeId,
            request.QuestionsPerGame,
            cancellationToken);
        return Created($"/api/v1/quizes/{quiz.Id}", QuizMapper.ToDto(quiz));
    }

    [HttpGet("{quizId:guid}/difficulty-counts")]
    public async Task<IActionResult> GetDifficultyCounts(Guid quizId, CancellationToken cancellationToken)
    {
        return Ok(await _quizCatalog.GetDifficultyCountsAsync(quizId, cancellationToken));
    }
}
