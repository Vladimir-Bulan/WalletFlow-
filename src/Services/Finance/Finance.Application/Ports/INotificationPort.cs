namespace Finance.Application.Ports;
public interface INotificationPort { Task SendTransactionNotificationAsync(Guid userId, string message, CancellationToken ct = default); }
