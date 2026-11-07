using Umbraco.Cms.Infrastructure.Scoping;

namespace BaryoDev.Umbraco.ShareLike.Likes;

public sealed record LikeTotal(Guid Key, int Count);

public interface ILikeStore
{
    int Get(Guid key);

    /// <summary>Adds one like and returns the new count.</summary>
    int Increment(Guid key);

    /// <summary>One page of totals, most liked first. Page is 1-based.</summary>
    IReadOnlyList<LikeTotal> Top(int page, int pageSize);
}

public sealed class LikeStore(IScopeProvider scopes) : ILikeStore
{
    public int Get(Guid key)
    {
        using var scope = scopes.CreateScope(autoComplete: true);
        var s = scope.Database.SqlContext.SqlSyntax;
        return scope.Database.ExecuteScalar<int?>(
            $"SELECT {s.GetQuotedColumnName("Count")} FROM {s.GetQuotedTableName(LikeDto.TableName)} WHERE {s.GetQuotedColumnName("PostKey")} = @0",
            key) ?? 0;
    }

    // ON CONFLICT is not portable to SQL Server, so: update, insert if no row changed,
    // and update again if the insert lost a race to another request.
    public int Increment(Guid key)
    {
        if (!TryUpdate(key))
        {
            try
            {
                Insert(key);
            }
            catch (Exception)
            {
                if (!TryUpdate(key))
                {
                    throw;
                }
            }
        }

        return Get(key);
    }

    public IReadOnlyList<LikeTotal> Top(int page, int pageSize)
    {
        using var scope = scopes.CreateScope(autoComplete: true);
        var s = scope.Database.SqlContext.SqlSyntax;
        var result = scope.Database.Page<LikeDto>(
            Math.Max(1, page),
            Math.Clamp(pageSize, 1, 200),
            $"SELECT * FROM {s.GetQuotedTableName(LikeDto.TableName)} ORDER BY {s.GetQuotedColumnName("Count")} DESC, {s.GetQuotedColumnName("PostKey")} ASC");
        return result.Items.Select(r => new LikeTotal(r.PostKey, r.Count)).ToList();
    }

    private bool TryUpdate(Guid key)
    {
        using var scope = scopes.CreateScope();
        var s = scope.Database.SqlContext.SqlSyntax;
        var c = s.GetQuotedColumnName("Count");
        var changed = scope.Database.Execute(
            $"UPDATE {s.GetQuotedTableName(LikeDto.TableName)} SET {c} = {c} + 1 WHERE {s.GetQuotedColumnName("PostKey")} = @0",
            key);
        scope.Complete();
        return changed > 0;
    }

    private void Insert(Guid key)
    {
        using var scope = scopes.CreateScope();
        scope.Database.Insert(new LikeDto { PostKey = key, Count = 1 });
        scope.Complete();
    }
}
