using Finance.Application.DTOs; using Finance.Domain.Exceptions; using Finance.Domain.Interfaces; using Finance.Domain.ValueObjects; using MediatR;
namespace Finance.Application.Commands;
public class DepositCommandHandler : IRequestHandler<DepositCommand, TransactionDto> {
    private readonly IAccountRepository _repository; private readonly IEventStore _eventStore;
    public DepositCommandHandler(IAccountRepository repository, IEventStore eventStore) { _repository = repository; _eventStore = eventStore; }
    public async Task<TransactionDto> Handle(DepositCommand request, CancellationToken ct) {
        var account = await _repository.GetByIdAsync(request.AccountId, ct) ?? throw new AccountNotFoundException(request.AccountId);
        var transaction = account.Deposit(Money.Of(request.Amount, request.Currency), request.Description);
        await _repository.UpdateAsync(account, ct);
        await _eventStore.SaveEventsAsync(account.Id, account.DomainEvents, account.Version - 1, ct);
        account.ClearDomainEvents();
        return new TransactionDto(transaction.Id, transaction.AccountId, transaction.DestinationAccountId, transaction.Amount.Amount, transaction.Amount.Currency, transaction.BalanceAfter.Amount, transaction.Type.ToString(), transaction.Description, transaction.CreatedAt);
    }
}
