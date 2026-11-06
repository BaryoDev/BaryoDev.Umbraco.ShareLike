using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace BaryoDev.Umbraco.ShareLike.Likes;

/// <summary>
/// Remembers who liked what until the daily salt changes. Holds only hashes of (salt, address, key),
/// never the address, and never more than MaxEntries.
/// </summary>
public sealed class LikeDedupe(DailySalt salt)
{
    public const int MaxEntries = 100_000;

    private readonly object _gate = new();
    private readonly HashSet<string> _seen = [];
    private byte[]? _saltInUse;

    /// <summary>True the first time this address likes this key today. False for a repeat.</summary>
    public bool TryAdd(IPAddress? ip, Guid key)
    {
        var current = salt.Current();
        var hash = Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(LikeRateLimit.PartitionKey(ip, current) + key)));
        lock (_gate)
        {
            if (!ReferenceEquals(_saltInUse, current))
            {
                _seen.Clear();
                _saltInUse = current;
            }

            // Full set: accept the like rather than refuse it. The cookie and rate limit still apply.
            return _seen.Count >= MaxEntries || _seen.Add(hash);
        }
    }
}
