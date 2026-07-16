using Microsoft.AspNetCore.Mvc;

using Backend.Features.Quizes.Dtos;

namespace Backend.Features.Quizes;

[ApiController]
[Route("api/quizes")]
public class QuizesController : ControllerBase
{
    private readonly QuizCatalog _quizCatalog;

    public QuizesController(QuizCatalog quizCatalog)
    {
        _quizCatalog = quizCatalog;
    }

    [HttpGet]
    public IActionResult GetQuizes()
    {
        return Ok(_quizCatalog.GetQuizes());
    }

    [HttpPost]
    public IActionResult CreateQuiz([FromBody] CreateQuizRequest request)
    {
        try
        {
            var quiz = _quizCatalog.CreateQuiz(request.Title, request.ThemeId, request.QuestionsPerGame);
            return Created($"/api/quizes/{quiz.Id}", quiz);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
