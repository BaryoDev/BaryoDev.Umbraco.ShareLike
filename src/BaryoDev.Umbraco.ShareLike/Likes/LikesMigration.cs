using Umbraco.Cms.Core.Packaging;
using Umbraco.Cms.Infrastructure.Migrations;

namespace BaryoDev.Umbraco.ShareLike.Likes;

/// <summary>Creates the like table on first boot. Umbraco finds package plans on its own.</summary>
public sealed class ShareLikeMigrationPlan() : PackageMigrationPlan("BaryoDev.Umbraco.ShareLike")
{
    protected override void DefinePlan() => To<CreateLikeTable>("3b8d5f0a-7c21-4e6b-9a14-5d2e8c7f1b36");
}

public sealed class CreateLikeTable(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!TableExists(LikeDto.TableName))
        {
            Create.Table<LikeDto>().Do();
        }

        return Task.CompletedTask;
    }
}
