using Finance.Application.DTOs; using Finance.Domain.Exceptions; using Finance.Domain.Interfaces; using Finance.Domain.ValueObjects; using MediatR;
namespace Finance.Application.Commands;
public class TransferCommandHandler : IRequestHandler<TransferCommand, TransactionDto> {
    private readonly IAccountRepository _repository; private readonly IEventStore _eventStore;
    public TransferCommandHandler(IAccountRepository repository, IEventStore eventStore) { _repository = repository; _eventStore = eventStore; }
    public async Task<TransactionDto> Handle(TransferCommand request, CancellationToken ct) {
        var source = await _repository.GetByIdAsync(request.SourceAccountId, ct) ?? throw new AccountNotFoundException(request.SourceAccountId);
        var destination = await _repository.GetByIdAsync(request.DestinationAccountId, ct) ?? throw new AccountNotFoundException(request.DestinationAccountId);
        var transaction = source.Transfer(destination, Money.Of(request.Amount, request.Currency), request.Description);
        await _repository.UpdateAsync(source, ct); await _repository.UpdateAsync(destination, ct);
        await _eventStore.SaveEventsAsync(source.Id, source.DomainEvents, source.Version - 1, ct);
        source.ClearDomainEvents();
        return new TransactionDto(transaction.Id, transaction.AccountId, transaction.DestinationAccountId, transaction.Amount.Amount, transaction.Amount.Currency, transaction.BalanceAfter.Amount, transaction.Type.ToString(), transaction.Description, transaction.CreatedAt);
    }
}
