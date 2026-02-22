// ===== COMMON =====
namespace Finance.Domain.Common
{
    public interface IDomainEvent { Guid EventId { get; } DateTime OccurredAt { get; } string EventType { get; } }
    public abstract record DomainEvent : IDomainEvent { public Guid EventId { get; } = Guid.NewGuid(); public DateTime OccurredAt { get; } = DateTime.UtcNow; public abstract string EventType { get; } }
    public abstract class AggregateRoot { private readonly List<IDomainEvent> _domainEvents = new(); public Guid Id { get; protected set; } public int Version { get; protected set; } public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly(); protected void RaiseDomainEvent(IDomainEvent e) { _domainEvents.Add(e); Version++; } public void ClearDomainEvents() => _domainEvents.Clear(); }
    public abstract class ValueObject { protected abstract IEnumerable<object> GetEqualityComponents(); public override bool Equals(object? obj) { if (obj is null || obj.GetType() != GetType()) return false; return GetEqualityComponents().SequenceEqual(((ValueObject)obj).GetEqualityComponents()); } public override int GetHashCode() => GetEqualityComponents().Aggregate(1, (c, o) => c * 23 + (o?.GetHashCode() ?? 0)); } }

// ===== ENUMS =====
namespace Finance.Domain.Enums
{
    public enum AccountStatus { Active, Suspended, Closed }
    public enum TransactionType { Deposit, Withdrawal, Transfer, Refund }
}

// ===== EXCEPTIONS =====
namespace Finance.Domain.Exceptions
{
    public class DomainException : Exception { public DomainException(string message) : base(message) { } }
    public class AccountNotFoundException : DomainException { public AccountNotFoundException(Guid id) : base($"Account '{id}' was not found.") { } }
    public class InsufficientFundsException : DomainException { public InsufficientFundsException(Guid id) : base($"Account '{id}' has insufficient funds.") { } }
}

// ===== VALUE OBJECTS =====
namespace Finance.Domain.ValueObjects
{
    using Finance.Domain.Common;
    using Finance.Domain.Exceptions;
    public class Money : ValueObject { public decimal Amount { get; } public string Currency { get; } private Money(decimal amount, string currency) { Amount = amount; Currency = currency.ToUpper(); }
    private Money() { Amount = 0; Currency = "ARS"; } public static Money Of(decimal amount, string currency = "ARS") { if (amount < 0) throw new DomainException("Negative amount."); return new Money(amount, currency); } public static Money Zero(string currency = "ARS") => new(0, currency); public Money Add(Money o) { Check(o); return new Money(Amount + o.Amount, Currency); } public Money Subtract(Money o) { Check(o); if (Amount < o.Amount) throw new DomainException("Insufficient funds."); return new Money(Amount - o.Amount, Currency); } public bool IsGreaterThan(Money o) { Check(o); return Amount > o.Amount; } void Check(Money o) { if (Currency != o.Currency) throw new DomainException($"Currency mismatch."); } protected override IEnumerable<object> GetEqualityComponents() { yield return Amount; yield return Currency; } }
    public class AccountNumber : ValueObject { public string Value { get; } private AccountNumber(string value) { Value = value; }
    private AccountNumber() { Value = string.Empty; } public static AccountNumber Generate() => new($"WF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}"); public static AccountNumber Of(string v) => new(v); protected override IEnumerable<object> GetEqualityComponents() { yield return Value; } }
}

// ===== EVENTS =====
namespace Finance.Domain.Events
{
    using Finance.Domain.Common;
    public record AccountCreatedEvent(Guid AccountId, Guid OwnerId, string AccountNumber, string Currency) : DomainEvent { public override string EventType => "account.created"; }
    public record MoneyDepositedEvent(Guid AccountId, Guid TransactionId, decimal Amount, string Currency, decimal BalanceAfter) : DomainEvent { public override string EventType => "money.deposited"; }
    public record MoneyWithdrawnEvent(Guid AccountId, Guid TransactionId, decimal Amount, string Currency, decimal BalanceAfter) : DomainEvent { public override string EventType => "money.withdrawn"; }
    public record MoneyTransferredEvent(Guid SourceAccountId, Guid DestinationAccountId, Guid TransactionId, decimal Amount, string Currency, decimal SourceBalanceAfter) : DomainEvent { public override string EventType => "money.transferred"; }
    public record AccountSuspendedEvent(Guid AccountId, string Reason) : DomainEvent { public override string EventType => "account.suspended"; }
}

// ===== AGGREGATES =====
namespace Finance.Domain.Aggregates
{
    using Finance.Domain.Common;
    using Finance.Domain.Enums;
    using Finance.Domain.Events;
    using Finance.Domain.Exceptions;
    using Finance.Domain.ValueObjects;
    public class Transaction { public Guid Id { get; private set; } public Guid AccountId { get; private set; } public Guid? DestinationAccountId { get; private set; } public Money Amount { get; private set; } = null!; public Money BalanceAfter { get; private set; } = null!; public TransactionType Type { get; private set; } public string Description { get; private set; } = ""; public DateTime CreatedAt { get; private set; } private Transaction() { } internal static Transaction Create(Guid accountId, Money amount, TransactionType type, string desc, Money balanceAfter, Guid? destId = null) => new() { Id = Guid.NewGuid(), AccountId = accountId, DestinationAccountId = destId, Amount = amount, BalanceAfter = balanceAfter, Type = type, Description = desc, CreatedAt = DateTime.UtcNow }; }
    public class Account : AggregateRoot { private readonly List<Transaction> _tx = new(); public Guid OwnerId { get; private set; } public AccountNumber AccountNumber { get; private set; } = null!; public Money Balance { get; private set; } = null!; public AccountStatus Status { get; private set; } public DateTime CreatedAt { get; private set; } public DateTime? UpdatedAt { get; private set; } public IReadOnlyList<Transaction> Transactions => _tx.AsReadOnly(); private Account() { } public static Account Create(Guid ownerId, string currency = "ARS") { var a = new Account { Id = Guid.NewGuid(), OwnerId = ownerId, AccountNumber = AccountNumber.Generate(), Balance = Money.Zero(currency), Status = AccountStatus.Active, CreatedAt = DateTime.UtcNow }; a.RaiseDomainEvent(new AccountCreatedEvent(a.Id, ownerId, a.AccountNumber.Value, currency)); return a; } public Transaction Deposit(Money amount, string desc = "Deposit") { Active(); Balance = Balance.Add(amount); UpdatedAt = DateTime.UtcNow; var t = Transaction.Create(Id, amount, TransactionType.Deposit, desc, Balance); _tx.Add(t); RaiseDomainEvent(new MoneyDepositedEvent(Id, t.Id, amount.Amount, amount.Currency, Balance.Amount)); return t; } public Transaction Withdraw(Money amount, string desc = "Withdrawal") { Active(); if (amount.IsGreaterThan(Balance)) throw new InsufficientFundsException(Id); Balance = Balance.Subtract(amount); UpdatedAt = DateTime.UtcNow; var t = Transaction.Create(Id, amount, TransactionType.Withdrawal, desc, Balance); _tx.Add(t); RaiseDomainEvent(new MoneyWithdrawnEvent(Id, t.Id, amount.Amount, amount.Currency, Balance.Amount)); return t; } public Transaction Transfer(Account dest, Money amount, string desc = "Transfer") { Active(); dest.Active(); if (amount.IsGreaterThan(Balance)) throw new InsufficientFundsException(Id); Balance = Balance.Subtract(amount); dest.Balance = dest.Balance.Add(amount); UpdatedAt = dest.UpdatedAt = DateTime.UtcNow; var t = Transaction.Create(Id, amount, TransactionType.Transfer, desc, Balance, dest.Id); _tx.Add(t); RaiseDomainEvent(new MoneyTransferredEvent(Id, dest.Id, t.Id, amount.Amount, amount.Currency, Balance.Amount)); return t; } public void Suspend(string reason) { if (Status == AccountStatus.Closed) throw new DomainException("Cannot suspend closed."); Status = AccountStatus.Suspended; UpdatedAt = DateTime.UtcNow; RaiseDomainEvent(new AccountSuspendedEvent(Id, reason)); } void Active() { if (Status != AccountStatus.Active) throw new DomainException($"Account not active."); } }
}

// ===== INTERFACES =====
namespace Finance.Domain.Interfaces
{
    using Finance.Domain.Aggregates;
    using Finance.Domain.Common;
    public interface IAccountRepository { Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default); Task<Account?> GetByOwnerIdAsync(Guid ownerId, CancellationToken ct = default); Task AddAsync(Account account, CancellationToken ct = default); Task UpdateAsync(Account account, CancellationToken ct = default); }
    public interface IEventStore { Task SaveEventsAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, int expectedVersion, CancellationToken ct = default); Task<IEnumerable<IDomainEvent>> GetEventsAsync(Guid aggregateId, CancellationToken ct = default); }
}


