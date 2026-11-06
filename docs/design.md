# Design

BaryoDev.Umbraco.ShareLike is share buttons, a rate-limited like count and reading time, lifted from the baryo.dev site so other Umbraco 17 sites can use them.

## Lifted from baryo.dev

- Like store, table and rate limit key: `Likes/LikeDto`, `LikeStore`, `LikeRateLimit` (with `DailySalt`), `LikesMigration`.
- Like endpoint: `Controllers/LikesController`.
- Published check: `PublishedPostLookup`.
- Markup and scripts: `_Like.cshtml`, `_ShareLinks.cshtml`, `like.js`, `share.js`.
- Look: the share and like rules in `site.css` and the values in `tokens.css`.

Reading time is new. baryo.dev has no reading time code, so it was written for the package.

## Kept as is

- Table `bdLike` (columns `PostKey`, `Count`) and cookie `bd_liked_{key}`. A site that already runs the baryo.dev code keeps its counts and its visitors keep their "liked" state.
- The abuse model: the IP is hashed with a salt that is held in memory and replaced every UTC day. The address is never stored. IPv6 is cut to the /64. One like per visitor per content item through the `bd_liked_{key}` cookie. The cookie is readable by JS and holds no secret, so `like.js` can show the liked state and the HTML is the same for every visitor (safe behind a CDN or output cache).
- Look: same layout, spacing and mono type. Colours come from CSS custom properties with the baryo.dev value as the last fallback, for example `var(--bdsl-green, var(--green, #0b7a55))`. A site that already has baryo.dev style tokens renders the same. Others set `--bdsl-*`.

## Generalised

- No document type alias in code. `LikeContentTypes` lists the aliases that allow likes. The default is empty, so nothing accepts likes until the site opts in.
- `ReadingTimeProperty` names the property that is counted.
- The upsert is portable. The baryo.dev SQL uses `ON CONFLICT`, which SQL Server does not have. The package runs update, then insert if no row changed, then update again if the insert lost a race.
- The rate limit window and permit count are options. The limiter is registered by the package and its middleware runs after routing.
- Share targets are a list in the options. The order is the render order.
- Class names are prefixed `bdsl-` so they do not collide with a site's own `.share` or `.like`. `LikeButtonCssClass` adds a site class (for example `btn`) to the like button.

## Cross-site check

`POST` is refused with 403 when `Sec-Fetch-Site` is present and is not `same-origin` or `none`, or when there is no `Sec-Fetch-Site` and `Origin` does not match the request host. With neither header the request is allowed, so old clients and no-JS still work.

## No per-address dedupe

An in-memory set of hashed (address, key) pairs was tried and removed. It locked out later visitors behind one NAT, CGNAT or proxy address, refused or stopped recording once full, and recorded the hash before the increment so a database failure locked the visitor out. The cookie and the rate limit are the guard, as on baryo.dev. Clearing cookies allows another like, within the rate limit.

## Options

Section `BaryoDev:ShareLike`.

| Option | Default |
| --- | --- |
| `LikeContentTypes` | empty |
| `ReadingTimeProperty` | `body` |
| `WordsPerMinute` | 200 |
| `ReadingTimeFormat` | `{0} min read` |
| `ShareTargets` | empty, which means `linkedin, x, facebook, email, native, copy` |
| `MastodonInstance` | `mastodon.social` |
| `LikePermitLimit` | 10 |
| `LikeWindowSeconds` | 60 |
| `LikeButtonLabel` | `Like this post` |
| `LikeButtonCssClass` | empty |
| `ShareLabel` | `Share` |

Targets: `facebook`, `linkedin`, `x`, `bluesky`, `mastodon`, `email`, `copy`, `native`. When both `native` and `copy` are listed, the browser shows native share if it has it, else copy.

## Pieces

- View components `ShareLinks`, `Like`, `ReadingTime`.
- Tag helper `<bdsl-reading-time content="@Model" />` and `Html.ReadingTime(content)`.
- Assets under `/App_Plugins/BaryoDev.ShareLike/`: `sharelike.css`, `like.js`, `share.js`.
- Content dashboard "Most liked", backed by a Management API endpoint in the Content section.

## Paid-tier candidates

Not built. Ideas only.

- Per-day like history and trend charts in the dashboard.
- Like and share counts exported to analytics or a webhook.
- Share click counting per target.
- Reactions beyond one like (several emoji, per-user likes tied to members).
- Social cards: generated Open Graph images per page.
- Auto-post to social networks on publish.
- Multi-site and per-culture like totals.
