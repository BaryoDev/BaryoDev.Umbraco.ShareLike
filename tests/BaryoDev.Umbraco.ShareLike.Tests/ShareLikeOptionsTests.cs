using BaryoDev.Umbraco.ShareLike;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.DependencyInjection;

namespace BaryoDev.Umbraco.ShareLike.Tests;

public class ShareLikeOptionsTests
{
    // IUmbracoBuilder is an interface, so a proxy that only answers Services is enough for Compose.
    public class BuilderProxy : System.Reflection.DispatchProxy
    {
        public IServiceCollection? Services { get; set; }

        protected override object? Invoke(System.Reflection.MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_Services")
            {
                return Services;
            }

            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    private static ShareLikeOptions Resolve(Action<IServiceCollection>? arrange = null, Dictionary<string, string?>? config = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(config ?? []).Build());
        var builder = System.Reflection.DispatchProxy.Create<IUmbracoBuilder, BuilderProxy>();
        ((BuilderProxy)(object)builder).Services = services;

        new ShareLikeComposer().Compose(builder);
        arrange?.Invoke(services);

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<ShareLikeOptions>>().Value;
    }

    [Fact]
    public void No_share_targets_configured_gives_the_default_list()
    {
        var options = Resolve();

        Assert.Equal(["linkedin", "x", "facebook", "email", "native", "copy"], options.ShareTargets);
    }

    [Fact]
    public void Configured_targets_replace_the_default()
    {
        var options = Resolve(config: new() { ["BaryoDev:ShareLike:ShareTargets:0"] = "bluesky" });

        Assert.Equal(["bluesky"], options.ShareTargets);
    }

    [Fact]
    public void Null_and_blank_targets_are_dropped()
    {
        var options = Resolve(s => s.Configure<ShareLikeOptions>(o => o.ShareTargets = [null!, "", "  ", "x", "copy"]));

        Assert.Equal(["x", "copy"], options.ShareTargets);
    }

    [Fact]
    public void Only_blank_targets_fall_back_to_the_default_list()
    {
        var options = Resolve(s => s.Configure<ShareLikeOptions>(o => o.ShareTargets = [null!, " "]));

        Assert.Equal(ShareLikeOptions.DefaultShareTargets, options.ShareTargets);
    }
}
