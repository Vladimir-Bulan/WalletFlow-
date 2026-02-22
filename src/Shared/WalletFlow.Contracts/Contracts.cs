namespace WalletFlow.Contracts.Events
{
    // Identity → Finance
    public record UserRegisteredIntegrationEvent
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Currency { get; init; } = "ARS";
        public DateTime RegisteredAt { get; init; }
    }

    // Finance → otros servicios
    public record AccountCreatedIntegrationEvent
    {
        public Guid AccountId { get; init; }
        public Guid OwnerId { get; init; }
        public string AccountNumber { get; init; } = string.Empty;
        public string Currency { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }

    public record MoneyTransferredIntegrationEvent
    {
        public Guid SourceAccountId { get; init; }
        public Guid DestinationAccountId { get; init; }
        public Guid TransactionId { get; init; }
        public decimal Amount { get; init; }
        public string Currency { get; init; } = string.Empty;
        public DateTime OccurredAt { get; init; }
    }
}
