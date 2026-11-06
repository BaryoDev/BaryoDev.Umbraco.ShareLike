namespace BaryoDev.Umbraco.ShareLike;

public sealed class ShareLikeOptions
{
    public const string Section = "BaryoDev:ShareLike";

    /// <summary>Document type aliases that accept likes. Empty means none.</summary>
    public string[] LikeContentTypes { get; set; } = [];

    public string ReadingTimeProperty { get; set; } = "body";
    public int WordsPerMinute { get; set; } = 200;
    public string ReadingTimeFormat { get; set; } = "{0} min read";

    /// <summary>Render order. Empty means the default list. facebook, linkedin, x, bluesky, mastodon, email, copy, native.</summary>
    public string[] ShareTargets { get; set; } = [];

    public static readonly string[] DefaultShareTargets = ["linkedin", "x", "facebook", "email", "native", "copy"];

    public string MastodonInstance { get; set; } = "mastodon.social";

    public int LikePermitLimit { get; set; } = 10;
    public int LikeWindowSeconds { get; set; } = 60;
    public string LikeButtonLabel { get; set; } = "Like this post";
    public string LikeButtonCssClass { get; set; } = "";
    public string ShareLabel { get; set; } = "Share";
}
