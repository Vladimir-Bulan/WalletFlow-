using Finance.Application.Commands;
using Finance.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Finance.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;
    public AccountsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountCommand cmd, CancellationToken ct)
    { var result = await _mediator.Send(cmd, ct); return CreatedAtAction(nameof(GetById), new { id = result.Id }, result); }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    { var result = await _mediator.Send(new GetAccountByIdQuery(id), ct); return result is null ? NotFound() : Ok(result); }

    [HttpPost("{id:guid}/deposit")]
    public async Task<IActionResult> Deposit(Guid id, [FromBody] DepositRequest req, CancellationToken ct)
    { var result = await _mediator.Send(new DepositCommand(id, req.Amount, req.Currency, req.Description), ct); return Ok(result); }

    [HttpPost("{id:guid}/withdraw")]
    public async Task<IActionResult> Withdraw(Guid id, [FromBody] WithdrawRequest req, CancellationToken ct)
    { var result = await _mediator.Send(new WithdrawCommand(id, req.Amount, req.Currency, req.Description), ct); return Ok(result); }

    [HttpPost("{id:guid}/transfer")]
    public async Task<IActionResult> Transfer(Guid id, [FromBody] TransferRequest req, CancellationToken ct)
    { var result = await _mediator.Send(new TransferCommand(id, req.DestinationAccountId, req.Amount, req.Currency, req.Description), ct); return Ok(result); }

    [HttpGet("{id:guid}/transactions")]
    public async Task<IActionResult> GetTransactions(Guid id, CancellationToken ct)
    { var result = await _mediator.Send(new GetTransactionHistoryQuery(id), ct); return Ok(result); }
}

public record DepositRequest(decimal Amount, string Currency = "ARS", string Description = "Deposit");
public record WithdrawRequest(decimal Amount, string Currency = "ARS", string Description = "Withdrawal");
public record TransferRequest(Guid DestinationAccountId, decimal Amount, string Currency = "ARS", string Description = "Transfer");
