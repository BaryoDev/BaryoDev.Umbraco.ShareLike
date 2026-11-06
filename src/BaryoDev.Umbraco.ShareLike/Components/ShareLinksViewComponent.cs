using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using BaryoDev.Umbraco.ShareLike.Share;

namespace BaryoDev.Umbraco.ShareLike.Components;

public sealed record ShareLinksModel(
    string Label, string Url, string Title, IReadOnlyList<ShareLink> Links, bool Native, bool Copy);

/// <summary>Pass content, or pass url and title.</summary>
public sealed class ShareLinksViewComponent(
    IOptionsMonitor<ShareLikeOptions> options, IPublishedUrlProvider urls) : ViewComponent
{
    public IViewComponentResult Invoke(IPublishedContent? content = null, string? url = null, string? title = null)
    {
        var o = options.CurrentValue;
        url ??= content is null ? null : urls.GetUrl(content, UrlMode.Absolute);
        title ??= content?.Name;
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(title))
        {
            return Content(string.Empty);
        }

        var links = o.ShareTargets
            .Select(t => ShareTargets.Build(t, url, title, o.MastodonInstance))
            .OfType<ShareLink>()
            .ToList();
        var targets = o.ShareTargets.Select(t => t.ToLowerInvariant()).ToList();
        return View(new ShareLinksModel(
            o.ShareLabel, url, title, links, targets.Contains("native"), targets.Contains("copy")));
    }
}
