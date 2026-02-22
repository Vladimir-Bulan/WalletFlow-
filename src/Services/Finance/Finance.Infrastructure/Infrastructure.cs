using MassTransit;
using WalletFlow.Contracts.Events;
// ===== DB CONTEXT =====
namespace Finance.Infrastructure.Persistence
{
    using Finance.Domain.Aggregates;
    using Microsoft.EntityFrameworkCore;

    public class FinanceDbContext : DbContext
    {
        public FinanceDbContext(DbContextOptions<FinanceDbContext> options) : base(options) { }
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<EventRecord> EventRecords => Set<EventRecord>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<Account>(e => {
                e.HasKey(a => a.Id);
                e.Property(a => a.OwnerId).IsRequired();
                e.Property(a => a.Status).HasConversion<string>();
                e.OwnsOne(a => a.Balance, b => { b.Property(m => m.Amount).HasColumnName("Balance"); b.Property(m => m.Currency).HasColumnName("Currency"); });
                e.OwnsOne(a => a.AccountNumber, n => { n.Property(x => x.Value).HasColumnName("AccountNumber"); });
                e.HasMany(a => a.Transactions).WithOne().HasForeignKey(t => t.AccountId);
            });
            mb.Entity<Transaction>(e => {
                e.HasKey(t => t.Id);
                e.Property(t => t.Type).HasConversion<string>();
                e.OwnsOne(t => t.Amount, b => { b.Property(m => m.Amount).HasColumnName("Amount"); b.Property(m => m.Currency).HasColumnName("Currency"); });
                e.OwnsOne(t => t.BalanceAfter, b => { b.Property(m => m.Amount).HasColumnName("BalanceAfter"); b.Property(m => m.Currency).HasColumnName("BalanceCurrency"); });
            });
            mb.Entity<EventRecord>(e => {
                e.HasKey(r => r.Id);
                e.HasIndex(r => new { r.AggregateId, r.Version });
            });
        }
    }

    public class EventRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AggregateId { get; set; }
        public int Version { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }
}

// ===== REPOSITORY =====
namespace Finance.Infrastructure.Repositories
{
    using Finance.Domain.Aggregates;
    using Finance.Domain.Interfaces;
    using Finance.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;

    public class AccountRepository : IAccountRepository
    {
        private readonly FinanceDbContext _db;
        public AccountRepository(FinanceDbContext db) => _db = db;

        public async Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Accounts.Include(a => a.Transactions).FirstOrDefaultAsync(a => a.Id == id, ct);

        public async Task<Account?> GetByOwnerIdAsync(Guid ownerId, CancellationToken ct = default)
            => await _db.Accounts.Include(a => a.Transactions).FirstOrDefaultAsync(a => a.OwnerId == ownerId, ct);

        public async Task AddAsync(Account account, CancellationToken ct = default)
        { await _db.Accounts.AddAsync(account, ct); await _db.SaveChangesAsync(ct); }

        public async Task UpdateAsync(Account account, CancellationToken ct = default)
        { _db.Accounts.Update(account); await _db.SaveChangesAsync(ct); }
    }
}

// ===== EVENT STORE =====
namespace Finance.Infrastructure.EventSourcing
{
    using Finance.Domain.Common;
    using Finance.Domain.Interfaces;
    using Finance.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;

    public class PostgresEventStore : IEventStore
    {
        private readonly FinanceDbContext _db;
        public PostgresEventStore(FinanceDbContext db) => _db = db;

        public async Task SaveEventsAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, int expectedVersion, CancellationToken ct = default)
        {
            var version = expectedVersion;
            foreach (var e in events)
            {
                var record = new EventRecord
                {
                    AggregateId = aggregateId,
                    Version = ++version,
                    EventType = e.EventType,
                    Payload = JsonConvert.SerializeObject(e, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All }),
                    OccurredAt = e.OccurredAt
                };
                await _db.EventRecords.AddAsync(record, ct);
            }
            await _db.SaveChangesAsync(ct);
        }

        public async Task<IEnumerable<IDomainEvent>> GetEventsAsync(Guid aggregateId, CancellationToken ct = default)
        {
            var records = await _db.EventRecords
                .Where(r => r.AggregateId == aggregateId)
                .OrderBy(r => r.Version)
                .ToListAsync(ct);
            return records.Select(r => JsonConvert.DeserializeObject<IDomainEvent>(r.Payload,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All })!);
        }

        private async Task<List<EventRecord>> ToListAsync(System.Linq.IQueryable<EventRecord> q, CancellationToken ct)
            => await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(q, ct);
    }
}

// ===== REDIS CACHE ADAPTER =====
namespace Finance.Infrastructure.Adapters
{
    using Finance.Application.Ports;
    using Microsoft.Extensions.Caching.Distributed;
    using System.Text.Json;

    public class RedisCacheAdapter : ICachePort
    {
        private readonly IDistributedCache _cache;
        public RedisCacheAdapter(IDistributedCache cache) => _cache = cache;

        public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            var data = await _cache.GetStringAsync(key, ct);
            return data is null ? default : JsonSerializer.Deserialize<T>(data);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(5) };
            await _cache.SetStringAsync(key, JsonSerializer.Serialize(value), options, ct);
        }

        public async Task RemoveAsync(string key, CancellationToken ct = default)
            => await _cache.RemoveAsync(key, ct);
    }

    public class ConsoleNotificationAdapter : Finance.Application.Ports.INotificationPort
    {
        public Task SendTransactionNotificationAsync(Guid userId, string message, CancellationToken ct = default)
        {
            Console.WriteLine($"[NOTIFICATION] User {userId}: {message}");
            return Task.CompletedTask;
        }
    }
}

// ===== DEPENDENCY INJECTION =====
namespace Finance.Infrastructure
{
    using Finance.Application.Ports;
    using Finance.Domain.Interfaces;
    using Finance.Infrastructure.Adapters;
    using Finance.Infrastructure.EventSourcing;
    using Finance.Infrastructure.Persistence;
    using Finance.Infrastructure.Repositories;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public static class DependencyInjection
    {
        public static IServiceCollection AddFinanceInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<FinanceDbContext>(opts =>
                opts.UseNpgsql(config.GetConnectionString("Finance")));

            services.AddStackExchangeRedisCache(opts =>
                opts.Configuration = config.GetConnectionString("Redis"));

            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<IEventStore, PostgresEventStore>();
            services.AddScoped<ICachePort, RedisCacheAdapter>();
            services.AddScoped<INotificationPort, ConsoleNotificationAdapter>();

            services.AddMassTransit(x =>
            {
                x.AddConsumers(typeof(DependencyInjection).Assembly);
                x.UsingRabbitMq((ctx, cfg) =>
                {
                    cfg.Host(config["RabbitMQ:Host"] ?? "localhost", "/", h =>
                    {
                        h.Username(config["RabbitMQ:Username"] ?? "guest");
                        h.Password(config["RabbitMQ:Password"] ?? "guest");
                    });
                    cfg.ConfigureEndpoints(ctx);
                });
            });

            return services;
        }
    }
}





