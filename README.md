# BaryoDev.Umbraco.ShareLike

Share buttons, a rate-limited like count and reading time for Umbraco 17. Lifted from the baryo.dev site.

## Install

```
dotnet add package BaryoDev.Umbraco.ShareLike
```

The package creates its own table (`bdLike`) on first boot through a package migration plan.

## Configure

`appsettings.json`:

```json
{
  "BaryoDev": {
    "ShareLike": {
      "LikeContentTypes": [ "post" ],
      "ReadingTimeProperty": "body",
      "ShareTargets": [ "linkedin", "x", "bluesky", "mastodon", "email", "native", "copy" ]
    }
  }
}
```

Likes are off for every document type until it is listed in `LikeContentTypes`. The other options and their defaults are in `docs/design.md`.

## Use

Add the assets to your layout:

```html
<link rel="stylesheet" href="/App_Plugins/BaryoDev.ShareLike/sharelike.css">
<script src="/App_Plugins/BaryoDev.ShareLike/like.js" defer></script>
<script src="/App_Plugins/BaryoDev.ShareLike/share.js" defer></script>
```

View components, in a view with an `IPublishedContent` model:

```cshtml
@await Component.InvokeAsync("ReadingTime", new { content = Model })
@await Component.InvokeAsync("Like", new { content = Model })
@await Component.InvokeAsync("ShareLinks", new { content = Model })
@await Component.InvokeAsync("ShareLinks", new { url = "https://example.com/a", title = "A page" })
```

Reading time as a tag helper (add `@addTagHelper *, BaryoDev.Umbraco.ShareLike` to `_ViewImports.cshtml`) or an HtmlHelper:

```cshtml
<bdsl-reading-time content="@Model" />
@Html.ReadingTime(Model)
```

Reading time counts the words in the configured property after stripping HTML. It works for text and rich text properties. It renders nothing when the property is empty.

## Look

The CSS uses custom properties with the baryo.dev value as the fallback. Override `--bdsl-green`, `--bdsl-ink`, `--bdsl-ink-mute`, `--bdsl-paper`, `--bdsl-yellow`, `--bdsl-on-dark`, `--bdsl-font-mono`, `--bdsl-font-sans` and `--bdsl-radius-pill`. If your site already defines `--green`, `--ink` and the rest, those are used.

## Likes

- `POST /api/sharelike/likes/{key}` adds one like. The content must be published and its type listed in `LikeContentTypes`.
- One like per visitor per item, through an HttpOnly cookie (`bd_liked_{key}`).
- Rate limited per address (default 10 per minute). The address is hashed with a salt that lives in memory and changes every UTC day. It is never stored. IPv6 is cut to the /64.
- Behind a proxy, set up forwarded headers so the real address reaches the limiter.

## Backoffice

A "Most liked" dashboard in the Content section lists the top 20 items by likes.

## License

MIT
