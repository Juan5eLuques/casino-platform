using Casino.Application.Features.Transactions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;
    public TransactionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        var result = await _mediator.Send(new TransferCommand(request.ToUserId, request.Amount));
        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<IActionResult> History()
    {
        var result = await _mediator.Send(new TransferHistoryQuery());
        return Ok(result);
    }

    [Authorize(Roles = "CASHIER,SUPERADMIN")]
    [HttpPost("load")]
    public async Task<IActionResult> Load([FromBody] LoadChipsCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(result);
    }

    [Authorize(Roles = "CASHIER,SUPERADMIN")]
    [HttpPost("unload")]
    public async Task<IActionResult> Unload([FromBody] UnloadChipsCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(result);
    }
}
