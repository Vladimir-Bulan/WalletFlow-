using Finance.Application.DTOs; using MediatR;
namespace Finance.Application.Commands;
public record CreateAccountCommand(Guid OwnerId, string Currency = "ARS") : IRequest<AccountDto>;
