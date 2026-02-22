using Finance.Application.DTOs; using MediatR;
namespace Finance.Application.Commands;
public record DepositCommand(Guid AccountId, decimal Amount, string Currency = "ARS", string Description = "Deposit") : IRequest<TransactionDto>;
