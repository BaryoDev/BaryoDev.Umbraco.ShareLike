using Asp.Versioning;
using BaryoDev.Umbraco.ShareLike.Likes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Authorization;

namespace BaryoDev.Umbraco.ShareLike.Api;

public sealed record MostLikedItem(Guid Key, string Name, int Count);

/// <summary>Most-liked content for the dashboard. Content section only, checked on the server.</summary>
[ApiVersion("1.0")]
[VersionedApiBackOfficeRoute("share-like")]
[ApiExplorerSettings(GroupName = "Share Like")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessContent)]
public sealed class ShareLikeController(ILikeStore store, IContentService contentService) : ManagementApiControllerBase
{
    [HttpGet("top")]
    [ProducesResponseType<IReadOnlyList<MostLikedItem>>(StatusCodes.Status200OK)]
    public IActionResult Top(int take = 20)
    {
        var items = store.Top(take)
            .Select(t => (Total: t, Content: contentService.GetById(t.Key)))
            .Where(x => x.Content is not null)
            .Select(x => new MostLikedItem(x.Total.Key, x.Content!.Name ?? x.Total.Key.ToString(), x.Total.Count))
            .ToList();
        return Ok(items);
    }
}
