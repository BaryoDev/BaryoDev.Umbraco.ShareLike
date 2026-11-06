using Microsoft.AspNetCore.Razor.TagHelpers;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace BaryoDev.Umbraco.ShareLike.ReadingTime;

/// <summary>&lt;bdsl-reading-time content="@Model" /&gt; renders a span, or nothing when there is no text.</summary>
[HtmlTargetElement("bdsl-reading-time", TagStructure = TagStructure.WithoutEndTag)]
public sealed class ReadingTimeTagHelper(IReadingTimeCalculator calculator) : TagHelper
{
    public IPublishedContent? Content { get; set; }

    /// <summary>Overrides the ReadingTimeProperty option.</summary>
    public string? Property { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var text = Content is null ? null : calculator.Format(Content, Property);
        if (text is null)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.Add("class", "bdsl-reading-time");
        output.Content.SetContent(text);
    }
}
