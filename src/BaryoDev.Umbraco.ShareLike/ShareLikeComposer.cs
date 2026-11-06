using System.Threading.RateLimiting;
using BaryoDev.Umbraco.ShareLike.Likes;
using BaryoDev.Umbraco.ShareLike.ReadingTime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace BaryoDev.Umbraco.ShareLike;

public sealed class ShareLikeComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddOptions<ShareLikeOptions>().BindConfiguration(ShareLikeOptions.Section);
        // The binder appends to array defaults, so the default list is applied here when nothing is configured.
        builder.Services.PostConfigure<ShareLikeOptions>(o =>
        {
            o.ShareTargets = o.ShareTargets.Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
            if (o.ShareTargets.Length == 0)
            {
                o.ShareTargets = ShareLikeOptions.DefaultShareTargets;
            }
        });
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<DailySalt>();
        builder.Services.AddScoped<ILikeStore, LikeStore>();
        builder.Services.AddScoped<ILikeableContentLookup, LikeableContentLookup>();
        builder.Services.AddSingleton<IReadingTimeCalculator, ReadingTimeCalculator>();

        builder.Services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(LikeRateLimit.PolicyName, ctx =>
            {
                var cfg = ctx.RequestServices.GetRequiredService<IOptionsMonitor<ShareLikeOptions>>().CurrentValue;
                var salt = ctx.RequestServices.GetRequiredService<DailySalt>().Current();
                return RateLimitPartition.GetFixedWindowLimiter(
                    LikeRateLimit.PartitionKey(ctx.Connection.RemoteIpAddress, salt),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(1, cfg.LikePermitLimit),
                        Window = TimeSpan.FromSeconds(Math.Max(1, cfg.LikeWindowSeconds)),
                        QueueLimit = 0,
                    });
            });
        });

        // The limiter must run after routing so it can see [EnableRateLimiting] on the endpoint.
        builder.Services.Configure<UmbracoPipelineOptions>(o =>
            o.AddFilter(new UmbracoPipelineFilter("BaryoDevShareLikeRateLimiter")
            {
                PostRouting = app => app.UseRateLimiter(),
            }));
    }
}
