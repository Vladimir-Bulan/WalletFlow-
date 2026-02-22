using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Identity.Domain.Aggregates;
using Identity.Domain.Exceptions;
using Identity.Domain.Interfaces;

// ── DTOs ─────────────────────────────────────────────────────────────────────
namespace Identity.Application.DTOs
{
    public record RegisterRequest(string Email, string FirstName, string LastName, string Password, string ConfirmPassword);
    public record LoginRequest(string Email, string Password);
    public record RefreshTokenRequest(string AccessToken, string RefreshToken);
    public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserDto User);
    public record UserDto(Guid Id, string Email, string FirstName, string LastName, string Role, string Status);
    public record ChangePasswordRequest(Guid UserId, string CurrentPassword, string NewPassword, string ConfirmPassword);
}

// ── Commands ──────────────────────────────────────────────────────────────────
namespace Identity.Application.Commands
{
    using Identity.Application.DTOs;
    using MediatR;

    public record RegisterCommand(string Email, string FirstName, string LastName, string Password) : IRequest<AuthResponse>;
    public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
    public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<AuthResponse>;
    public record LogoutCommand(Guid UserId, string RefreshToken) : IRequest<bool>;
    public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest<bool>;
}

// ── Queries ───────────────────────────────────────────────────────────────────
namespace Identity.Application.Queries
{
    using Identity.Application.DTOs;
    using MediatR;

    public record GetUserByIdQuery(Guid UserId) : IRequest<UserDto?>;
    public record GetUserByEmailQuery(string Email) : IRequest<UserDto?>;
}

// ── Validators ────────────────────────────────────────────────────────────────
namespace Identity.Application.Validators
{
    using Identity.Application.Commands;
    using FluentValidation;

    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
                .Matches("[A-Z]").WithMessage("Password must contain uppercase.")
                .Matches("[0-9]").WithMessage("Password must contain a digit.");
        }
    }

    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(x => x.AccessToken).NotEmpty();
            RuleFor(x => x.RefreshToken).NotEmpty();
        }
    }
}

// ── Ports ─────────────────────────────────────────────────────────────────────
namespace Identity.Application.Ports
{
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string hash);
    }
}

// ── Handlers ──────────────────────────────────────────────────────────────────
namespace Identity.Application.Handlers
{
    using Identity.Application.Commands;
    using Identity.Application.DTOs;
    using Identity.Application.Ports;
    using Identity.Domain.Exceptions;
    using Identity.Domain.Interfaces;
    using MediatR;

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
    {
        private readonly IUserRepository _users;
        private readonly IPasswordHasher _hasher;
        private readonly ITokenService _tokens;

        public RegisterCommandHandler(IUserRepository users, IPasswordHasher hasher, ITokenService tokens)
        {
            _users = users; _hasher = hasher; _tokens = tokens;
        }

        public async Task<AuthResponse> Handle(RegisterCommand cmd, CancellationToken ct)
        {
            if (await _users.ExistsAsync(cmd.Email, ct))
                throw new UserAlreadyExistsException(cmd.Email);

            var hash = _hasher.Hash(cmd.Password);
            var user = User.Create(cmd.Email, cmd.FirstName, cmd.LastName, hash);
            await _users.AddAsync(user, ct);

            var accessToken = _tokens.GenerateAccessToken(user);
            var refreshToken = _tokens.GenerateRefreshToken();
            user.GenerateRefreshToken(refreshToken);
            await _users.UpdateAsync(user, ct);

            return new AuthResponse(accessToken, refreshToken, DateTime.UtcNow.AddHours(1),
                new UserDto(user.Id, user.Email.Value, user.Name.FirstName, user.Name.LastName, user.Role.ToString(), user.Status.ToString()));
        }
    }

    public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
    {
        private readonly IUserRepository _users;
        private readonly IPasswordHasher _hasher;
        private readonly ITokenService _tokens;

        public LoginCommandHandler(IUserRepository users, IPasswordHasher hasher, ITokenService tokens)
        {
            _users = users; _hasher = hasher; _tokens = tokens;
        }

        public async Task<AuthResponse> Handle(LoginCommand cmd, CancellationToken ct)
        {
            var user = await _users.GetByEmailAsync(cmd.Email, ct)
                ?? throw new InvalidCredentialsException();

            if (!_hasher.Verify(cmd.Password, user.PasswordHash))
                throw new InvalidCredentialsException();

            var accessToken = _tokens.GenerateAccessToken(user);
            var refreshToken = _tokens.GenerateRefreshToken();
            user.GenerateRefreshToken(refreshToken);
            await _users.UpdateAsync(user, ct);

            return new AuthResponse(accessToken, refreshToken, DateTime.UtcNow.AddHours(1),
                new UserDto(user.Id, user.Email.Value, user.Name.FirstName, user.Name.LastName, user.Role.ToString(), user.Status.ToString()));
        }
    }

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
    {
        private readonly IUserRepository _users;
        private readonly ITokenService _tokens;

        public RefreshTokenCommandHandler(IUserRepository users, ITokenService tokens)
        {
            _users = users; _tokens = tokens;
        }

        public async Task<AuthResponse> Handle(RefreshTokenCommand cmd, CancellationToken ct)
        {
            var principal = _tokens.GetPrincipalFromExpiredToken(cmd.AccessToken)
                ?? throw new DomainException("Invalid access token.");

            var userId = Guid.Parse(principal.FindFirst("sub")?.Value ?? throw new DomainException("Invalid token claims."));
            var user = await _users.GetByIdAsync(userId, ct)
                ?? throw new UserNotFoundException(userId);

            var newAccessToken = _tokens.GenerateAccessToken(user);
            var newRefreshToken = _tokens.GenerateRefreshToken();
            user.RotateRefreshToken(cmd.RefreshToken, newRefreshToken);
            await _users.UpdateAsync(user, ct);

            return new AuthResponse(newAccessToken, newRefreshToken, DateTime.UtcNow.AddHours(1),
                new UserDto(user.Id, user.Email.Value, user.Name.FirstName, user.Name.LastName, user.Role.ToString(), user.Status.ToString()));
        }
    }

    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, bool>
    {
        private readonly IUserRepository _users;
        public LogoutCommandHandler(IUserRepository users) { _users = users; }

        public async Task<bool> Handle(LogoutCommand cmd, CancellationToken ct)
        {
            var user = await _users.GetByIdAsync(cmd.UserId, ct);
            if (user == null) return false;
            user.RevokeAllTokens();
            await _users.UpdateAsync(user, ct);
            return true;
        }
    }

    public class GetUserByIdQueryHandler : IRequestHandler<Queries.GetUserByIdQuery, UserDto?>
    {
        private readonly IUserRepository _users;
        public GetUserByIdQueryHandler(IUserRepository users) { _users = users; }

        public async Task<UserDto?> Handle(Queries.GetUserByIdQuery query, CancellationToken ct)
        {
            var user = await _users.GetByIdAsync(query.UserId, ct);
            return user == null ? null :
                new UserDto(user.Id, user.Email.Value, user.Name.FirstName, user.Name.LastName, user.Role.ToString(), user.Status.ToString());
        }
    }
}

// ── DI ───────────────────────────────────────────────────────────────────────
namespace Identity.Application
{
    using FluentValidation;
    using MediatR;
    using Microsoft.Extensions.DependencyInjection;

    public static class DependencyInjection
    {
        public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
            return services;
        }
    }
}
