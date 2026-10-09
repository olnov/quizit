using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.GameSessions;

[ApiController]
[Route("api/v1/admin/quizes/{quizId:guid}/statistics")]
[Authorize(Policy = "Authoring")]
public sealed class QuizStatisticsController(QuizStatisticsService statistics) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<QuizStatisticsReportDto>> GetReport(
        Guid quizId, [FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (from.Kind != DateTimeKind.Utc || to.Kind != DateTimeKind.Utc || from >= to)
            return BadRequest("Provide a valid UTC interval with from before to.");
        return Ok(await statistics.GetReportAsync(quizId, from, to, page, pageSize, cancellationToken));
    }

    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<QuizAttemptDetailDto>> GetDetail(
        Guid quizId, Guid sessionId, CancellationToken cancellationToken)
    {
        return Ok(await statistics.GetDetailAsync(quizId, sessionId, cancellationToken));
    }

    [HttpGet("detailed")]
    public async Task<ActionResult<QuizDetailedReportDto>> GetDetailedReport(
        Guid quizId, [FromQuery] DateTime from, [FromQuery] DateTime to,
        CancellationToken cancellationToken)
    {
        if (from.Kind != DateTimeKind.Utc || to.Kind != DateTimeKind.Utc || from >= to)
            return BadRequest("Provide a valid UTC interval with from before to.");
        return Ok(await statistics.GetDetailedReportAsync(quizId, from, to, cancellationToken));
    }
}
