using BaryoDev.Umbraco.ShareLike.Likes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace BaryoDev.Umbraco.ShareLike.Components;

public sealed record LikeModel(Guid Key, int Count, bool Liked, string Label, string ButtonClass);

/// <summary>Renders nothing unless the content type is listed in LikeContentTypes.</summary>
public sealed class LikeViewComponent(
    ILikeStore store, ILikeableContentLookup lookup, IOptionsMonitor<ShareLikeOptions> options) : ViewComponent
{
    public IViewComponentResult Invoke(IPublishedContent content)
    {
        if (!lookup.Allows(content.ContentType.Alias))
        {
            return Content(string.Empty);
        }

        var o = options.CurrentValue;
        var liked = Request.Cookies.ContainsKey(LikesController.CookiePrefix + content.Key);
        return View(new LikeModel(content.Key, store.Get(content.Key), liked, o.LikeButtonLabel, o.LikeButtonCssClass));
    }
}
