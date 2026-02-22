using Finance.Application.DTOs; using Finance.Application.Ports; using Finance.Domain.Interfaces; using MediatR;
namespace Finance.Application.Queries;
public record GetAccountByIdQuery(Guid AccountId) : IRequest<AccountDto?>;
public class GetAccountByIdQueryHandler : IRequestHandler<GetAccountByIdQuery, AccountDto?> {
    private readonly IAccountRepository _repository; private readonly ICachePort _cache;
    public GetAccountByIdQueryHandler(IAccountRepository repository, ICachePort cache) { _repository = repository; _cache = cache; }
    public async Task<AccountDto?> Handle(GetAccountByIdQuery request, CancellationToken ct) {
        var cacheKey = $"account:{request.AccountId}";
        var cached = await _cache.GetAsync<AccountDto>(cacheKey, ct);
        if (cached is not null) return cached;
        var account = await _repository.GetByIdAsync(request.AccountId, ct);
        if (account is null) return null;
        var dto = new AccountDto(account.Id, account.OwnerId, account.AccountNumber.Value, account.Balance.Amount, account.Balance.Currency, account.Status.ToString(), account.CreatedAt);
        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5), ct);
        return dto;
    }
}
