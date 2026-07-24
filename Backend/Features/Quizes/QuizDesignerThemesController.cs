using Backend.Features.Quizes.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Quizes;

[ApiController]
[Route("api/v1/admin/quiz-themes")]
[Authorize(Policy = "Authoring")]
public class QuizDesignerThemesController(QuizDesigner quizDesigner) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<QuizThemeDto>>> GetThemes(
        CancellationToken cancellationToken)
    {
        return Ok(await quizDesigner.GetThemesAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<QuizThemeDto>> CreateTheme(
        [FromBody] CreateQuizThemeRequest request,
        CancellationToken cancellationToken)
    {
        var theme = await quizDesigner.CreateThemeAsync(request, cancellationToken);
        return Created($"/api/v1/admin/quiz-themes/{theme.Id}", theme);
    }

    [HttpPut("{themeId:guid}")]
    public async Task<ActionResult<QuizThemeDto>> UpdateTheme(
        Guid themeId,
        [FromBody] UpdateQuizThemeRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await quizDesigner.UpdateThemeAsync(themeId, request, cancellationToken));
    }

    [HttpDelete("{themeId:guid}")]
    public async Task<IActionResult> DeleteTheme(Guid themeId, CancellationToken cancellationToken)
    {
        await quizDesigner.DeleteThemeAsync(themeId, cancellationToken);
        return NoContent();
    }
}
