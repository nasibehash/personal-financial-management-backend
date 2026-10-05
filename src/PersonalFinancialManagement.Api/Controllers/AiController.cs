using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinancialManagement.Api.Contracts;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Features.Commands.AddTransactionFromText;
using PersonalFinancialManagement.Application.Features.Commands.AddTransactionFromVoice;
using PersonalFinancialManagement.Application.Features.Queries.GetFinancialInsights;

namespace PersonalFinancialManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private const long MaxAudioBytes = 10 * 1024 * 1024;

    private readonly ISender _sender;

    public AiController(ISender sender) => _sender = sender;

    // POST api/ai/transactions/text
    [HttpPost("transactions/text")]
    public async Task<ActionResult<AiTransactionResultDto>> AddFromText(TextTransactionRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new AddTransactionFromTextCommand(request.Text, request.AccountId, request.Preview),
            cancellationToken);

        return result.Transaction is null ? Ok(result) : Created($"/api/transactions/{result.Transaction.Id}", result);
    }

    // POST api/ai/transactions/voice (multipart/form-data)
    [HttpPost("transactions/voice")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxAudioBytes)]
    public async Task<ActionResult<AiTransactionResultDto>> AddFromVoice([FromForm] VoiceTransactionRequest request, CancellationToken cancellationToken)
    {
        if (request.Audio is null || request.Audio.Length == 0)
            return Problem("Attach a recording in the 'audio' form field.", statusCode: StatusCodes.Status400BadRequest);

        if (request.Audio.Length > MaxAudioBytes)
            return Problem("The recording is larger than 10 MB.", statusCode: StatusCodes.Status400BadRequest);

        await using var audio = request.Audio.OpenReadStream();

        var result = await _sender.Send(
            new AddTransactionFromVoiceCommand(
                audio,
                request.Audio.FileName,
                request.Audio.ContentType,
                request.AccountId,
                request.Preview),
            cancellationToken);

        return result.Transaction is null ? Ok(result) : Created($"/api/transactions/{result.Transaction.Id}", result);
    }

    // GET api/ai/insights?from=2026-10-01&to=2026-10-31
    [HttpGet("insights")]
    public async Task<ActionResult<FinancialInsightsDto>> Insights(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetFinancialInsightsQuery(from, to), cancellationToken));
}
