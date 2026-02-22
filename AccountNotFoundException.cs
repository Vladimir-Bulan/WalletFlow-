namespace Finance.Domain.Exceptions;

public class AccountNotFoundException : DomainException
{
    public AccountNotFoundException(Guid accountId)
        : base($"Account '{accountId}' was not found.") { }
}
