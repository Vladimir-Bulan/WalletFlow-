namespace Finance.Application.DTOs;
public record TransactionDto(Guid Id, Guid AccountId, Guid? DestinationAccountId, decimal Amount, string Currency, decimal BalanceAfter, string Type, string Description, DateTime CreatedAt);
