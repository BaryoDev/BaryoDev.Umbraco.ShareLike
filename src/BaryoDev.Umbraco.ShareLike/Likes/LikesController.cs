using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BaryoDev.Umbraco.ShareLike.Likes;

public sealed class LikesController(ILikeStore store, ILikeableContentLookup content, LikeDedupe dedupe) : Controller
{
    public const string CookiePrefix = "bd_liked_";

    [HttpPost]
    [Route("api/sharelike/likes/{key:guid}")]
    [EnableRateLimiting(LikeRateLimit.PolicyName)]
    public IActionResult Like(Guid key)
    {
        if (!IsSameSite())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!content.Exists(key))
        {
            return NotFound();
        }

        var cookie = CookiePrefix + key;
        if (Request.Cookies.ContainsKey(cookie))
        {
            return StatusCode(StatusCodes.Status409Conflict, new { count = store.Get(key) });
        }

        if (!dedupe.TryAdd(HttpContext.Connection.RemoteIpAddress, key))
        {
            return StatusCode(StatusCodes.Status409Conflict, new { count = store.Get(key) });
        }

        var count = store.Increment(key);
        Response.Cookies.Append(cookie, "1", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(365),
            Path = "/",
        });
        return Ok(new { count });
    }

    // Refuse cross-site requests. With neither header (old clients, no-JS) the request is allowed.
    private bool IsSameSite()
    {
        if (Request.Headers.TryGetValue("Sec-Fetch-Site", out var site) && site.Count > 0)
        {
            return site == "same-origin" || site == "none";
        }

        if (Request.Headers.TryGetValue("Origin", out var origin) && origin.Count > 0)
        {
            return Uri.TryCreate(origin.ToString(), UriKind.Absolute, out var uri)
                && string.Equals(uri.Authority, Request.Host.Value, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }
}
