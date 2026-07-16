using Microsoft.AspNetCore.Mvc;

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
        return Ok(await _quizCatalog.GetQuizesAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuiz(
        [FromBody] CreateQuizRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var quiz = await _quizCatalog.CreateQuizAsync(
                request.Title,
                request.ThemeId,
                request.QuestionsPerGame,
                cancellationToken);
            return Created($"/api/v1/quizes/{quiz.Id}", quiz);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
