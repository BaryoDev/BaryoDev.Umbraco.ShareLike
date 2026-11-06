namespace BaryoDev.Umbraco.ShareLike.Share;

public sealed record ShareLink(string Name, string Label, string Href, bool External);

public static class ShareTargets
{
    public static ShareLink? Build(string target, string url, string title, string mastodonInstance)
    {
        var u = Uri.EscapeDataString(url);
        var t = Uri.EscapeDataString(title);
        var both = Uri.EscapeDataString(title + " " + url);
        return target.ToLowerInvariant() switch
        {
            "linkedin" => new("linkedin", "LinkedIn", $"https://www.linkedin.com/sharing/share-offsite/?url={u}", true),
            "x" => new("x", "X", $"https://twitter.com/intent/tweet?url={u}&text={t}", true),
            "facebook" => new("facebook", "Facebook", $"https://www.facebook.com/sharer/sharer.php?u={u}", true),
            "bluesky" => new("bluesky", "Bluesky", $"https://bsky.app/intent/compose?text={both}", true),
            "mastodon" => new("mastodon", "Mastodon", $"https://{mastodonInstance}/share?text={both}", true),
            "email" => new("email", "Email", $"mailto:?subject={t}&body={u}", false),
            _ => null,
        };
    }
}
