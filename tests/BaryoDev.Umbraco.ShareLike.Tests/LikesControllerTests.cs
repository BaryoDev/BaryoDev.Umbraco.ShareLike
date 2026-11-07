using BaryoDev.Umbraco.ShareLike.Likes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;

namespace BaryoDev.Umbraco.ShareLike.Tests;

public class LikesControllerTests
{
    private sealed class FakeStore : ILikeStore
    {
        public Dictionary<Guid, int> Counts { get; } = new();

        public int Get(Guid key) => Counts.GetValueOrDefault(key);

        public int Increment(Guid key) => Counts[key] = Get(key) + 1;

        public IReadOnlyList<LikeTotal> Top(int page, int pageSize) =>
            Counts.Select(c => new LikeTotal(c.Key, c.Value)).OrderByDescending(t => t.Count).ToList();
    }

    private sealed class FakeContent(bool exists) : ILikeableContentLookup
    {
        public bool Allows(string contentTypeAlias) => exists;

        public bool Exists(Guid key) => exists;
    }

    private static readonly Guid Post = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static (LikesController Controller, FakeStore Store) Build(
        Action<HttpRequest>? request = null, bool exists = true)
    {
        var store = new FakeStore();
        var http = new DefaultHttpContext();
        http.Request.Host = new HostString("example.com");
        request?.Invoke(http.Request);
        var controller = new LikesController(store, new FakeContent(exists))
        {
            ControllerContext = new ControllerContext(new ActionContext(http, new RouteData(), new ControllerActionDescriptor())),
        };
        return (controller, store);
    }

    private static int Status(IActionResult result) => ((IStatusCodeActionResult)result).StatusCode ?? 0;

    [Fact]
    public void Cross_site_fetch_is_refused()
    {
        var (controller, store) = Build(r => r.Headers["Sec-Fetch-Site"] = "cross-site");

        var result = controller.Like(Post);

        Assert.Equal(403, Status(result));
        Assert.Equal(0, store.Get(Post));
    }

    [Theory]
    [InlineData("same-origin")]
    [InlineData("none")]
    public void Same_origin_fetch_is_allowed(string site)
    {
        var (controller, store) = Build(r => r.Headers["Sec-Fetch-Site"] = site);

        Assert.Equal(200, Status(controller.Like(Post)));
        Assert.Equal(1, store.Get(Post));
    }

    [Fact]
    public void Origin_from_another_host_is_refused()
    {
        var (controller, store) = Build(r => r.Headers["Origin"] = "https://evil.example");

        Assert.Equal(403, Status(controller.Like(Post)));
        Assert.Equal(0, store.Get(Post));
    }

    [Fact]
    public void Origin_with_a_malformed_value_is_refused()
    {
        var (controller, _) = Build(r => r.Headers["Origin"] = "not a url");

        Assert.Equal(403, Status(controller.Like(Post)));
    }

    [Fact]
    public void Origin_from_the_same_host_is_allowed()
    {
        var (controller, store) = Build(r => r.Headers["Origin"] = "https://example.com");

        Assert.Equal(200, Status(controller.Like(Post)));
        Assert.Equal(1, store.Get(Post));
    }

    [Fact]
    public void Request_without_either_header_is_allowed()
    {
        var (controller, store) = Build();

        Assert.Equal(200, Status(controller.Like(Post)));
        Assert.Equal(1, store.Get(Post));
    }

    [Fact]
    public void Unknown_content_is_not_found_and_not_counted()
    {
        var (controller, store) = Build(exists: false);

        Assert.Equal(404, Status(controller.Like(Post)));
        Assert.Equal(0, store.Get(Post));
    }

    [Fact]
    public void A_second_like_with_the_cookie_is_a_conflict()
    {
        var (controller, store) = Build(r => r.Headers.Cookie = LikesController.CookiePrefix + Post + "=1");

        Assert.Equal(409, Status(controller.Like(Post)));
        Assert.Equal(0, store.Get(Post));
    }
}
