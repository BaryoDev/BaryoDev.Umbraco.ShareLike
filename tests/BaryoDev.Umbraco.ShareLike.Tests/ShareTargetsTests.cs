using BaryoDev.Umbraco.ShareLike.Share;
using Xunit;

namespace BaryoDev.Umbraco.ShareLike.Tests;

public class ShareTargetsTests
{
    [Fact]
    public void X_link_uses_the_x_intent_with_encoded_url_and_text()
    {
        var link = ShareTargets.Build("x", "https://example.com/a b?c=1&d=2", "Tom & Jerry", "mastodon.social");

        Assert.Equal("https://x.com/intent/tweet?url=https%3A%2F%2Fexample.com%2Fa%20b%3Fc%3D1%26d%3D2&text=Tom%20%26%20Jerry", link!.Href);
    }
}
