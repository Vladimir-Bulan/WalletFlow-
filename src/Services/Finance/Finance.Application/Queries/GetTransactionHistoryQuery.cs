using Finance.Application.DTOs; using Finance.Domain.Exceptions; using Finance.Domain.Interfaces; using MediatR;
namespace Finance.Application.Queries;
public record GetTransactionHistoryQuery(Guid AccountId) : IRequest<IEnumerable<TransactionDto>>;
public class GetTransactionHistoryQueryHandler : IRequestHandler<GetTransactionHistoryQuery, IEnumerable<TransactionDto>> {
    private readonly IAccountRepository _repository;
    public GetTransactionHistoryQueryHandler(IAccountRepository repository) { _repository = repository; }
    public async Task<IEnumerable<TransactionDto>> Handle(GetTransactionHistoryQuery request, CancellationToken ct) {
        var account = await _repository.GetByIdAsync(request.AccountId, ct) ?? throw new AccountNotFoundException(request.AccountId);
        return account.Transactions.Select(t => new TransactionDto(t.Id, t.AccountId, t.DestinationAccountId, t.Amount.Amount, t.Amount.Currency, t.BalanceAfter.Amount, t.Type.ToString(), t.Description, t.CreatedAt));
    }
}
