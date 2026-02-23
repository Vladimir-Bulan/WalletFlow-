using Microsoft.AspNetCore.SignalR;
using Finance.API.Hubs;

namespace Finance.API.Services
{
    public class SignalRNotificationService
    {
        private readonly IHubContext<WalletHub> _hub;
        private readonly ILogger<SignalRNotificationService> _logger;

        public SignalRNotificationService(IHubContext<WalletHub> hub, ILogger<SignalRNotificationService> logger)
        {
            _hub = hub;
            _logger = logger;
        }

        public async Task NotifyDeposit(Guid userId, decimal amount, string currency, decimal newBalance)
        {
            await _hub.Clients.Group($"user-{userId}").SendAsync("BalanceUpdated", new
            {
                Type = "Deposit",
                Amount = amount,
                Currency = currency,
                NewBalance = newBalance,
                Timestamp = DateTime.UtcNow
            });
            _logger.LogInformation("Notified user {UserId} of deposit {Amount}", userId, amount);
        }

        public async Task NotifyWithdrawal(Guid userId, decimal amount, string currency, decimal newBalance)
        {
            await _hub.Clients.Group($"user-{userId}").SendAsync("BalanceUpdated", new
            {
                Type = "Withdrawal",
                Amount = amount,
                Currency = currency,
                NewBalance = newBalance,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task NotifyTransfer(Guid senderId, Guid receiverId, decimal amount, string currency, decimal senderBalance)
        {
            await _hub.Clients.Group($"user-{senderId}").SendAsync("BalanceUpdated", new
            {
                Type = "TransferSent",
                Amount = amount,
                Currency = currency,
                NewBalance = senderBalance,
                Timestamp = DateTime.UtcNow
            });

            await _hub.Clients.Group($"user-{receiverId}").SendAsync("BalanceUpdated", new
            {
                Type = "TransferReceived",
                Amount = amount,
                Currency = currency,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
