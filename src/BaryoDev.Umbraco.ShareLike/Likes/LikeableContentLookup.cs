using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.PublishedCache;

namespace BaryoDev.Umbraco.ShareLike.Likes;

public interface ILikeableContentLookup
{
    bool Allows(string contentTypeAlias);

    /// <summary>True only for a published node whose type is in LikeContentTypes.</summary>
    bool Exists(Guid key);
}

public sealed class LikeableContentLookup(IPublishedContentCache cache, IOptionsMonitor<ShareLikeOptions> options)
    : ILikeableContentLookup
{
    public bool Allows(string contentTypeAlias) =>
        options.CurrentValue.LikeContentTypes.Contains(contentTypeAlias, StringComparer.OrdinalIgnoreCase);

    public bool Exists(Guid key)
    {
        var node = cache.GetById(key);
        return node is not null && Allows(node.ContentType.Alias);
    }
}
