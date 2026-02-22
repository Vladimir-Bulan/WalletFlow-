using Finance.Application.Commands; using FluentValidation;
namespace Finance.Application.Validators;
public class DepositCommandValidator : AbstractValidator<DepositCommand> {
    public DepositCommandValidator() { RuleFor(x => x.AccountId).NotEmpty(); RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(1_000_000); RuleFor(x => x.Currency).NotEmpty().Length(3); }
}
public class WithdrawCommandValidator : AbstractValidator<WithdrawCommand> {
    public WithdrawCommandValidator() { RuleFor(x => x.AccountId).NotEmpty(); RuleFor(x => x.Amount).GreaterThan(0); RuleFor(x => x.Currency).NotEmpty().Length(3); }
}
public class TransferCommandValidator : AbstractValidator<TransferCommand> {
    public TransferCommandValidator() { RuleFor(x => x.SourceAccountId).NotEmpty(); RuleFor(x => x.DestinationAccountId).NotEmpty().NotEqual(x => x.SourceAccountId); RuleFor(x => x.Amount).GreaterThan(0); }
}
