using Asp.Versioning;
using BaryoDev.Umbraco.ShareLike.Likes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Api.Management.Services.PermissionFilter;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Authorization;

namespace BaryoDev.Umbraco.ShareLike.Api;

public sealed record MostLikedItem(Guid Key, string Name, int Count);

/// <summary>Most-liked content for the dashboard. Content section only, checked on the server.</summary>
[ApiVersion("1.0")]
[VersionedApiBackOfficeRoute("share-like")]
[ApiExplorerSettings(GroupName = "Share Like")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessContent)]
public sealed class ShareLikeController(
    ILikeStore store,
    IBackOfficeSecurityAccessor security,
    IEntityService entityService,
    IDocumentPermissionFilterService browseFilter,
    AppCaches appCaches) : ManagementApiControllerBase
{
    private const int MaxPages = 10;

    [HttpGet("top")]
    [ProducesResponseType<IReadOnlyList<MostLikedItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Top(int take = 20)
    {
        var user = security.BackOfficeSecurity?.CurrentUser;
        if (user is null)
        {
            return Forbid();
        }

        take = Math.Clamp(take, 1, 50);
        // A start node of -1 means the user has root access.
        var starts = user.CalculateContentStartNodeIds(entityService, appCaches) ?? [];
        var root = starts.Contains(Constants.System.Root);

        // Page through the totals until there are enough rows this user may see, so a restricted
        // editor still gets a full list. Capped so a user who sees almost nothing costs a bounded scan.
        var items = new List<MostLikedItem>();
        for (var page = 1; page <= MaxPages && items.Count < take; page++)
        {
            var totals = store.Top(page, take);
            if (totals.Count == 0)
            {
                break;
            }

            var entities = entityService.GetAll(UmbracoObjectTypes.Document, totals.Select(x => x.Key).ToArray())
                .Where(e => !e.Trashed && (root || InStartNodes(e.Path, starts)))
                .ToArray();
            var allowed = (await browseFilter.FilterAsync(entities, entities.Length)).Entities
                .ToDictionary(e => e.Key);
            foreach (var total in totals)
            {
                if (allowed.TryGetValue(total.Key, out var entity) && items.Count < take)
                {
                    items.Add(new MostLikedItem(total.Key, entity.Name ?? total.Key.ToString(), total.Count));
                }
            }

            if (totals.Count < take)
            {
                break;
            }
        }

        return Ok(items);
    }

    private static bool InStartNodes(string path, int[] starts)
    {
        var ids = path.Split(',');
        return starts.Any(s => ids.Contains(s.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }
}
