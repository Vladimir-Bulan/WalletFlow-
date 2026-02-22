namespace Finance.Application.Ports;
public interface ICachePort { Task<T?> GetAsync<T>(string key, CancellationToken ct = default); Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default); Task RemoveAsync(string key, CancellationToken ct = default); }
