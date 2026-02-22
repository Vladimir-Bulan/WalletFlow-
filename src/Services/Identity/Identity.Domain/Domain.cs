using System.Text.RegularExpressions;

// ── Common ──────────────────────────────────────────────────────────────────
namespace Identity.Domain.Common
{
    public abstract class ValueObject
    {
        protected abstract IEnumerable<object> GetEqualityComponents();
        public override bool Equals(object? obj) => obj is ValueObject v && GetEqualityComponents().SequenceEqual(v.GetEqualityComponents());
        public override int GetHashCode() => GetEqualityComponents().Aggregate(0, HashCode.Combine);
    }

    public abstract class DomainEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public abstract string EventType { get; }
    }

    public abstract class AggregateRoot
    {
        private readonly List<DomainEvent> _events = new();
        public Guid Id { get; protected set; }
        public IReadOnlyList<DomainEvent> DomainEvents => _events.AsReadOnly();
        protected void RaiseDomainEvent(DomainEvent e) => _events.Add(e);
        public void ClearDomainEvents() => _events.Clear();
    }
}

// ── Exceptions ───────────────────────────────────────────────────────────────
namespace Identity.Domain.Exceptions
{
    public class DomainException : Exception { public DomainException(string msg) : base(msg) { } }
    public class UserAlreadyExistsException : DomainException { public UserAlreadyExistsException(string email) : base($"User with email '{email}' already exists.") { } }
    public class InvalidCredentialsException : DomainException { public InvalidCredentialsException() : base("Invalid email or password.") { } }
    public class UserNotFoundException : DomainException { public UserNotFoundException(Guid id) : base($"User '{id}' not found.") { } }
}

// ── Value Objects ─────────────────────────────────────────────────────────────
namespace Identity.Domain.ValueObjects
{
    using Identity.Domain.Common;
    using Identity.Domain.Exceptions;

    public class Email : ValueObject
    {
        public string Value { get; }
        private Email(string v) { Value = v.ToLowerInvariant(); }
        private Email() { Value = string.Empty; }
        public static Email Of(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email cannot be empty.");
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) throw new DomainException($"'{email}' is not a valid email.");
            return new Email(email);
        }
        protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
        public override string ToString() => Value;
    }

    public class FullName : ValueObject
    {
        public string FirstName { get; }
        public string LastName { get; }
        public string DisplayName => $"{FirstName} {LastName}";
        private FullName(string first, string last) { FirstName = first; LastName = last; }
        private FullName() { FirstName = string.Empty; LastName = string.Empty; }
        public static FullName Of(string first, string last)
        {
            if (string.IsNullOrWhiteSpace(first)) throw new DomainException("First name cannot be empty.");
            if (string.IsNullOrWhiteSpace(last)) throw new DomainException("Last name cannot be empty.");
            return new FullName(first.Trim(), last.Trim());
        }
        protected override IEnumerable<object> GetEqualityComponents() { yield return FirstName; yield return LastName; }
    }
}

// ── Enums ─────────────────────────────────────────────────────────────────────
namespace Identity.Domain.Enums
{
    public enum UserRole { User, Admin }
    public enum UserStatus { Active, Suspended, Deleted }
}

// ── Events ────────────────────────────────────────────────────────────────────
namespace Identity.Domain.Events
{
    using Identity.Domain.Common;
    public class UserRegisteredEvent : DomainEvent { public Guid UserId { get; } public string Email { get; } public string FullName { get; } public UserRegisteredEvent(Guid userId, string email, string fullName) { UserId = userId; Email = email; FullName = fullName; } public override string EventType => "identity.user.registered"; }
    public class UserLoggedInEvent : DomainEvent { public Guid UserId { get; } public string Email { get; } public DateTime LoginAt { get; } public UserLoggedInEvent(Guid userId, string email, DateTime loginAt) { UserId = userId; Email = email; LoginAt = loginAt; } public override string EventType => "identity.user.loggedin"; }
    public class UserSuspendedEvent : DomainEvent { public Guid UserId { get; } public string Reason { get; } public UserSuspendedEvent(Guid userId, string reason) { UserId = userId; Reason = reason; } public override string EventType => "identity.user.suspended"; }
    public class RefreshTokenRotatedEvent : DomainEvent { public Guid UserId { get; } public string OldToken { get; } public string NewToken { get; } public RefreshTokenRotatedEvent(Guid userId, string oldToken, string newToken) { UserId = userId; OldToken = oldToken; NewToken = newToken; } public override string EventType => "identity.token.rotated"; }
}

// ── Aggregates ────────────────────────────────────────────────────────────────
namespace Identity.Domain.Aggregates
{
    using Identity.Domain.Common;
    using Identity.Domain.Enums;
    using Identity.Domain.Events;
    using Identity.Domain.Exceptions;
    using Identity.Domain.ValueObjects;

    public class RefreshToken
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public string Token { get; private set; } = string.Empty;
        public DateTime ExpiresAt { get; private set; }
        public bool IsRevoked { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public string? ReplacedByToken { get; private set; }
        private RefreshToken() { }
        public static RefreshToken Create(Guid userId, string token, int expiryDays = 7) =>
            new() { Id = Guid.NewGuid(), UserId = userId, Token = token, ExpiresAt = DateTime.UtcNow.AddDays(expiryDays), CreatedAt = DateTime.UtcNow };
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsActive => !IsRevoked && !IsExpired;
        public void Revoke(string? replacedBy = null) { IsRevoked = true; ReplacedByToken = replacedBy; }
    }

    public class User : AggregateRoot
    {
        private readonly List<RefreshToken> _refreshTokens = new();
        public Email Email { get; private set; } = null!;
        public FullName Name { get; private set; } = null!;
        public string PasswordHash { get; private set; } = string.Empty;
        public UserRole Role { get; private set; }
        public UserStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public IReadOnlyList<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
        private User() { }

        public static User Create(string email, string firstName, string lastName, string passwordHash, UserRole role = UserRole.User)
        {
            var u = new User
            {
                Id = Guid.NewGuid(),
                Email = Email.Of(email),
                Name = FullName.Of(firstName, lastName),
                PasswordHash = passwordHash,
                Role = role,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            u.RaiseDomainEvent(new UserRegisteredEvent(u.Id, email, u.Name.DisplayName));
            return u;
        }

        public RefreshToken GenerateRefreshToken(string token)
        {
            var rt = RefreshToken.Create(Id, token);
            _refreshTokens.Add(rt);
            RaiseDomainEvent(new UserLoggedInEvent(Id, Email.Value, DateTime.UtcNow));
            return rt;
        }

        public RefreshToken RotateRefreshToken(string oldToken, string newToken)
        {
            var existing = _refreshTokens.FirstOrDefault(t => t.Token == oldToken)
                ?? throw new DomainException("Refresh token not found.");
            if (!existing.IsActive) throw new DomainException("Refresh token is not active.");
            existing.Revoke(newToken);
            var rt = RefreshToken.Create(Id, newToken);
            _refreshTokens.Add(rt);
            RaiseDomainEvent(new RefreshTokenRotatedEvent(Id, oldToken, newToken));
            return rt;
        }

        public void RevokeAllTokens()
        {
            foreach (var t in _refreshTokens.Where(t => t.IsActive))
                t.Revoke();
        }

        public void Suspend(string reason)
        {
            if (Status == UserStatus.Deleted) throw new DomainException("Cannot suspend deleted user.");
            Status = UserStatus.Suspended;
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new UserSuspendedEvent(Id, reason));
        }

        public void UpdatePassword(string newHash) { PasswordHash = newHash; UpdatedAt = DateTime.UtcNow; }
    }
}

// ── Interfaces ────────────────────────────────────────────────────────────────
namespace Identity.Domain.Interfaces
{
    using Identity.Domain.Aggregates;
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<bool> ExistsAsync(string email, CancellationToken ct = default);
        Task AddAsync(User user, CancellationToken ct = default);
        Task UpdateAsync(User user, CancellationToken ct = default);
    }

    public interface ITokenService
    {
        string GenerateAccessToken(Aggregates.User user);
        string GenerateRefreshToken();
        System.Security.Claims.ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}

