using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace BaryoDev.Umbraco.ShareLike.ReadingTime;

public interface IReadingTimeCalculator
{
    /// <summary>Whole minutes, at least 1. Null when the property is missing or empty.</summary>
    int? Minutes(IPublishedContent content, string? propertyAlias = null);

    string? Format(IPublishedContent content, string? propertyAlias = null);
}

public sealed partial class ReadingTimeCalculator(IOptionsMonitor<ShareLikeOptions> options) : IReadingTimeCalculator
{
    public int? Minutes(IPublishedContent content, string? propertyAlias = null)
    {
        var o = options.CurrentValue;
        var value = content.GetProperty(propertyAlias ?? o.ReadingTimeProperty)?.GetValue();
        return value is null ? null : MinutesFor(value.ToString(), o.WordsPerMinute);
    }

    public string? Format(IPublishedContent content, string? propertyAlias = null)
    {
        var minutes = Minutes(content, propertyAlias);
        return minutes is null ? null : string.Format(options.CurrentValue.ReadingTimeFormat, minutes);
    }

    public static int? MinutesFor(string? html, int wordsPerMinute)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var text = System.Net.WebUtility.HtmlDecode(Tags().Replace(html, " "));
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        if (words == 0)
        {
            return null;
        }

        return Math.Max(1, (int)Math.Ceiling(words / (double)Math.Max(1, wordsPerMinute)));
    }

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex Tags();
}
