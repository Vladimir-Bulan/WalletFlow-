using Finance.Application.DTOs; using Finance.Domain.Aggregates; using Finance.Domain.Interfaces; using MediatR;
namespace Finance.Application.Commands;
public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, AccountDto> {
    private readonly IAccountRepository _repository; private readonly IEventStore _eventStore;
    public CreateAccountCommandHandler(IAccountRepository repository, IEventStore eventStore) { _repository = repository; _eventStore = eventStore; }
    public async Task<AccountDto> Handle(CreateAccountCommand request, CancellationToken ct) {
        var account = Account.Create(request.OwnerId, request.Currency);
        await _repository.AddAsync(account, ct);
        await _eventStore.SaveEventsAsync(account.Id, account.DomainEvents, 0, ct);
        account.ClearDomainEvents();
        return new AccountDto(account.Id, account.OwnerId, account.AccountNumber.Value, account.Balance.Amount, account.Balance.Currency, account.Status.ToString(), account.CreatedAt);
    }
}
