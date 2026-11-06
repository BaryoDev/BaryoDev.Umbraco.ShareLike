using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace BaryoDev.Umbraco.ShareLike.ReadingTime;

public static class ReadingTimeHtmlExtensions
{
    /// <summary>Renders the reading time span, or empty content when there is no text.</summary>
    public static IHtmlContent ReadingTime(this IHtmlHelper html, IPublishedContent content, string? property = null)
    {
        var calculator = html.ViewContext.HttpContext.RequestServices.GetRequiredService<IReadingTimeCalculator>();
        var text = calculator.Format(content, property);
        if (text is null)
        {
            return HtmlString.Empty;
        }

        var span = new TagBuilder("span");
        span.AddCssClass("bdsl-reading-time");
        span.InnerHtml.Append(text);
        return span;
    }
}
