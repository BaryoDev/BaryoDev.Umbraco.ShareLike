using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace BaryoDev.Umbraco.ShareLike.Likes;

/// <summary>Random bytes held in memory, replaced when the UTC date changes. Never persisted.</summary>
public sealed class DailySalt(TimeProvider time)
{
    private readonly object _gate = new();
    private DateOnly _day;
    private byte[] _salt = [];

    public byte[] Current()
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        lock (_gate)
        {
            if (_salt.Length == 0 || today != _day)
            {
                _salt = RandomNumberGenerator.GetBytes(32);
                _day = today;
            }

            return _salt;
        }
    }
}

public static class LikeRateLimit
{
    public const string PolicyName = "bdsl-likes";

    /// <summary>SHA256 of the address and the day's salt, hex. The address itself is never kept.</summary>
    public static string PartitionKey(IPAddress? ip, byte[] salt)
    {
        if (ip is not null && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv4MappedToIPv6)
            {
                ip = ip.MapToIPv4();
            }
            else
            {
                // Keep the /64: one subscriber can rotate through the whole host half.
                var bytes = ip.GetAddressBytes();
                Array.Clear(bytes, 8, 8);
                ip = new IPAddress(bytes);
            }
        }

        var raw = Encoding.UTF8.GetBytes(ip?.ToString() ?? "unknown");
        var input = new byte[raw.Length + salt.Length];
        raw.CopyTo(input, 0);
        salt.CopyTo(input, raw.Length);
        return Convert.ToHexString(SHA256.HashData(input));
    }
}
