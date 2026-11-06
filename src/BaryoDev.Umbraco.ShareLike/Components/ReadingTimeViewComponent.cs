using BaryoDev.Umbraco.ShareLike.ReadingTime;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace BaryoDev.Umbraco.ShareLike.Components;

public sealed class ReadingTimeViewComponent(IReadingTimeCalculator calculator) : ViewComponent
{
    public IViewComponentResult Invoke(IPublishedContent content, string? property = null)
    {
        var text = calculator.Format(content, property);
        return text is null ? Content(string.Empty) : View((object)text);
    }
}
