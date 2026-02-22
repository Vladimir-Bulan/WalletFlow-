using Finance.Domain.Aggregates;
using Finance.Domain.Enums;
using Finance.Domain.Exceptions;
using Finance.Domain.ValueObjects;

namespace Finance.Tests.Domain;

public class AccountTests
{
    [Fact]
    public void CreateAccount_ShouldRaiseAccountCreatedEvent()
    {
        var account = Account.Create(Guid.NewGuid(), "ARS");
        Assert.NotNull(account);
        Assert.Single(account.DomainEvents);
        Assert.Equal(AccountStatus.Active, account.Status);
    }

    [Fact]
    public void Deposit_ShouldIncreaseBalance()
    {
        var account = Account.Create(Guid.NewGuid(), "ARS");
        account.ClearDomainEvents();
        account.Deposit(Money.Of(1000, "ARS"), "Test deposit");
        Assert.Equal(1000, account.Balance.Amount);
        Assert.Single(account.DomainEvents);
    }

    [Fact]
    public void Withdraw_ShouldDecreaseBalance()
    {
        var account = Account.Create(Guid.NewGuid(), "ARS");
        account.Deposit(Money.Of(1000, "ARS"), "deposit");
        account.ClearDomainEvents();
        account.Withdraw(Money.Of(400, "ARS"), "withdrawal");
        Assert.Equal(600, account.Balance.Amount);
    }

    [Fact]
    public void Withdraw_InsufficientFunds_ShouldThrow()
    {
        var account = Account.Create(Guid.NewGuid(), "ARS");
        Assert.Throws<InsufficientFundsException>(() =>
            account.Withdraw(Money.Of(100, "ARS"), "test"));
    }

    [Fact]
    public void Money_Of_NegativeAmount_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => Money.Of(-1, "ARS"));
    }

    [Fact]
    public void Transfer_ShouldDebitSourceAndCreditDestination()
    {
        var source = Account.Create(Guid.NewGuid(), "ARS");
        var destination = Account.Create(Guid.NewGuid(), "ARS");
        source.Deposit(Money.Of(1000, "ARS"), "deposit");
        source.ClearDomainEvents();
        destination.ClearDomainEvents();

        source.Transfer(destination, Money.Of(300, "ARS"), "transfer");

        Assert.Equal(700, source.Balance.Amount);
        Assert.Equal(300, destination.Balance.Amount);
    }

    [Fact]
    public void Suspend_ShouldChangeSatus()
    {
        var account = Account.Create(Guid.NewGuid(), "ARS");
        account.Suspend("test reason");
        Assert.Equal(AccountStatus.Suspended, account.Status);
    }

    [Fact]
    public void Withdraw_OnSuspendedAccount_ShouldThrow()
    {
        var account = Account.Create(Guid.NewGuid(), "ARS");
        account.Deposit(Money.Of(500, "ARS"), "deposit");
        account.Suspend("suspended");
        Assert.Throws<DomainException>(() =>
            account.Withdraw(Money.Of(100, "ARS"), "test"));
    }
}
