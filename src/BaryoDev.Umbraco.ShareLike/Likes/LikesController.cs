using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BaryoDev.Umbraco.ShareLike.Likes;

public sealed class LikesController(ILikeStore store, ILikeableContentLookup content) : Controller
{
    public const string CookiePrefix = "bd_liked_";

    [HttpPost]
    [Route("api/sharelike/likes/{key:guid}")]
    [EnableRateLimiting(LikeRateLimit.PolicyName)]
    public IActionResult Like(Guid key)
    {
        if (!content.Exists(key))
        {
            return NotFound();
        }

        var cookie = CookiePrefix + key;
        if (Request.Cookies.ContainsKey(cookie))
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
}
