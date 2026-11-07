using BaryoDev.Umbraco.ShareLike.Likes;
using NPoco;
using NUnit.Framework;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Packaging;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BaryoDev.Umbraco.ShareLike.IntegrationTests;

// baryoweb's own table and plan, copied from baryoweb/Likes so the upgrade path is tested with its real shape.
[TableName("bdLike")]
[PrimaryKey("PostKey", AutoIncrement = false)]
[ExplicitColumns]
public sealed class BaryoWebLikeDto
{
    [Column("PostKey")]
    [PrimaryKeyColumn(AutoIncrement = false)]
    public Guid PostKey { get; set; }

    [Column("Count")]
    public int Count { get; set; }
}

public sealed class BaryoWebLikesMigrationPlan : PackageMigrationPlan
{
    public BaryoWebLikesMigrationPlan() : base("BaryoWeb.Likes")
    {
    }

    protected override void DefinePlan() => To<BaryoWebCreateLikeTable>("baryoweb-likes-create-table");
}

public sealed class BaryoWebCreateLikeTable : AsyncMigrationBase
{
    public BaryoWebCreateLikeTable(IMigrationContext context) : base(context)
    {
    }

    protected override Task MigrateAsync()
    {
        if (!TableExists("bdLike"))
        {
            Create.Table<BaryoWebLikeDto>().Do();
        }

        return Task.CompletedTask;
    }
}

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class LikesTests : UmbracoIntegrationTest
{
    private async Task RunPlan(PackageMigrationPlan plan)
    {
        var executor = Services.GetRequiredService<IMigrationPlanExecutor>();
        await executor.ExecutePlanAsync(plan, string.Empty);
    }

    [Test]
    public async Task Existing_bdLike_table_and_counts_survive_the_migration()
    {
        var post = Guid.NewGuid();
        await RunPlan(new BaryoWebLikesMigrationPlan());
        using (var scope = ScopeProvider.CreateScope())
        {
            scope.Database.Insert(new BaryoWebLikeDto { PostKey = post, Count = 7 });
            scope.Complete();
        }

        await RunPlan(new ShareLikeMigrationPlan());

        var store = new LikeStore(ScopeProvider);
        Assert.That(store.Get(post), Is.EqualTo(7));
        Assert.That(store.Increment(post), Is.EqualTo(8));
    }

    [Test]
    public async Task Migration_on_a_fresh_database_creates_a_usable_table()
    {
        await RunPlan(new ShareLikeMigrationPlan());

        var store = new LikeStore(ScopeProvider);
        var post = Guid.NewGuid();
        Assert.That(store.Get(post), Is.EqualTo(0));
        Assert.That(store.Increment(post), Is.EqualTo(1));
        Assert.That(store.Increment(post), Is.EqualTo(2));
    }

    [Test]
    public async Task Fifty_concurrent_likes_count_fifty()
    {
        await RunPlan(new ShareLikeMigrationPlan());
        var store = new LikeStore(ScopeProvider);
        var post = Guid.NewGuid();

        // Dedicated threads released together by a barrier, so the increments really overlap.
        using var barrier = new Barrier(50);
        var tasks = Enumerable.Range(0, 50)
            .Select(_ => Task.Factory.StartNew(
                () =>
                {
                    Assert.That(barrier.SignalAndWait(TimeSpan.FromSeconds(30)), Is.True);
                    store.Increment(post);
                },
                TaskCreationOptions.LongRunning))
            .ToArray();
        await Task.WhenAll(tasks);

        Assert.That(store.Get(post), Is.EqualTo(50));
    }

    [Test]
    public async Task Tied_counts_page_in_post_key_order()
    {
        await RunPlan(new ShareLikeMigrationPlan());
        var store = new LikeStore(ScopeProvider);

        // Inserted in reverse, so insertion order alone would page them backwards.
        var keys = Enumerable.Range(1, 5).Select(i => Guid.Parse($"00000000-0000-0000-0000-00000000000{i}")).ToList();
        foreach (var key in Enumerable.Reverse(keys))
        {
            store.Increment(key);
        }

        var paged = store.Top(1, 2).Concat(store.Top(2, 2)).Concat(store.Top(3, 2)).Select(t => t.Key).ToList();
        Assert.That(paged, Is.EqualTo(keys));
    }
}
