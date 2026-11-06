using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace BaryoDev.Umbraco.ShareLike.Likes;

[TableName(TableName)]
[PrimaryKey("PostKey", AutoIncrement = false)]
[ExplicitColumns]
public sealed class LikeDto
{
    // Same table as the baryo.dev site, so existing counts carry over.
    public const string TableName = "bdLike";

    [Column("PostKey")]
    [PrimaryKeyColumn(AutoIncrement = false)]
    public Guid PostKey { get; set; }

    [Column("Count")]
    public int Count { get; set; }
}
