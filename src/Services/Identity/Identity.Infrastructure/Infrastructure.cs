using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Identity.Domain.Aggregates;
using Identity.Domain.Enums;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using Identity.Application.Ports;

// ── DbContext ─────────────────────────────────────────────────────────────────
namespace Identity.Infrastructure.Persistence
{
    public class IdentityDbContext : DbContext
    {
        public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }
        public DbSet<User> Users => Set<User>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Ignore<Identity.Domain.Common.DomainEvent>();
            mb.Entity<User>(b =>
            {
                b.HasKey(u => u.Id);
                b.OwnsOne(u => u.Email, e => { e.Property(x => x.Value).HasColumnName("Email").IsRequired(); });
                b.OwnsOne(u => u.Name, n => {
                    n.Property(x => x.FirstName).HasColumnName("FirstName").IsRequired();
                    n.Property(x => x.LastName).HasColumnName("LastName").IsRequired();
                });
                b.Property(u => u.PasswordHash).IsRequired();
                b.Property(u => u.Role).HasConversion<string>();
                b.Property(u => u.Status).HasConversion<string>();
                b.HasMany(u => u.RefreshTokens).WithOne().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
                // unique index handled via owned type column directly
            });

            mb.Entity<RefreshToken>(b =>
            {
                b.HasKey(t => t.Id);
                b.Property(t => t.Token).IsRequired();
                b.HasIndex(t => t.Token).IsUnique();
            });
        }
    }
}

// ── Repository ────────────────────────────────────────────────────────────────
namespace Identity.Infrastructure.Repositories
{
    using Identity.Infrastructure.Persistence;

    public class UserRepository : IUserRepository
    {
        private readonly IdentityDbContext _db;
        public UserRepository(IdentityDbContext db) { _db = db; }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Users.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.Id == id, ct);

        public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
            await _db.Users.Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => EF.Property<string>(u, "Email") == email.ToLowerInvariant(), ct);

        public async Task<bool> ExistsAsync(string email, CancellationToken ct = default) =>
            await _db.Users.AnyAsync(u => EF.Property<string>(u, "Email") == email.ToLowerInvariant(), ct);

        public async Task AddAsync(User user, CancellationToken ct = default)
        {
            await _db.Users.AddAsync(user, ct);
            await _db.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(User user, CancellationToken ct = default)
        {
            _db.Users.Update(user);
            await _db.SaveChangesAsync(ct);
        }
    }
}

// ── Token Service ─────────────────────────────────────────────────────────────
namespace Identity.Infrastructure.Services
{
    public class JwtTokenService : ITokenService
    {
        private readonly string _secret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expiryMinutes;

        public JwtTokenService(IConfiguration config)
        {
            _secret = config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured");
            _issuer = config["Jwt:Issuer"] ?? "WalletFlow.Identity";
            _audience = config["Jwt:Audience"] ?? "WalletFlow";
            _expiryMinutes = int.Parse(config["Jwt:ExpiryMinutes"] ?? "60");
        }

        public string GenerateAccessToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email.Value),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("firstName", user.Name.FirstName),
                new Claim("lastName", user.Name.LastName),
            };
            var token = new JwtSecurityToken(_issuer, _audience, claims,
                expires: DateTime.UtcNow.AddMinutes(_expiryMinutes), signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var validation = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false
            };
            try
            {
                return new JwtSecurityTokenHandler().ValidateToken(token, validation, out _);
            }
            catch { return null; }
        }
    }

    public class BcryptPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, 12);
        public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
    }
}

// ── DI ────────────────────────────────────────────────────────────────────────
namespace Identity.Infrastructure
{
    using Identity.Infrastructure.Persistence;
    using Identity.Infrastructure.Repositories;
    using Identity.Infrastructure.Services;

    public static class DependencyInjection
    {
        public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<IdentityDbContext>(opts =>
                opts.UseNpgsql(configuration.GetConnectionString("Identity")));

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITokenService, JwtTokenService>();
            services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();

            return services;
        }
    }
}



