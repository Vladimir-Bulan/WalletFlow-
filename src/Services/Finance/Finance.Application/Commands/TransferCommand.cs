using Finance.Application.DTOs; using MediatR;
namespace Finance.Application.Commands;
public record TransferCommand(Guid SourceAccountId, Guid DestinationAccountId, decimal Amount, string Currency = "ARS", string Description = "Transfer") : IRequest<TransactionDto>;
