using Finance.Application.DTOs; using MediatR;
namespace Finance.Application.Commands;
public record WithdrawCommand(Guid AccountId, decimal Amount, string Currency = "ARS", string Description = "Withdrawal") : IRequest<TransactionDto>;
