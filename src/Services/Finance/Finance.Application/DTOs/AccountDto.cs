namespace Finance.Application.DTOs;
public record AccountDto(Guid Id, Guid OwnerId, string AccountNumber, decimal Balance, string Currency, string Status, DateTime CreatedAt);
