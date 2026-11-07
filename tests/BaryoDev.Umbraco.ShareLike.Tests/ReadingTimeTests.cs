using BaryoDev.Umbraco.ShareLike.ReadingTime;

namespace BaryoDev.Umbraco.ShareLike.Tests;

public class ReadingTimeTests
{
    private static string Words(int n) => string.Join(' ', Enumerable.Repeat("word", n));

    [Theory]
    [InlineData(1, 1)]
    [InlineData(200, 1)]
    [InlineData(201, 2)]
    [InlineData(400, 2)]
    [InlineData(1000, 5)]
    public void Reading_time_is_200_wpm_and_at_least_1(int words, int minutes)
    {
        Assert.Equal(minutes, ReadingTimeCalculator.MinutesFor(Words(words), 200));
    }

    [Fact]
    public void Tags_and_entities_are_not_counted_as_words()
    {
        var html = "<p>" + Words(200) + "</p><img src=\"a.png\" alt=\"x y z\">&nbsp;";

        Assert.Equal(1, ReadingTimeCalculator.MinutesFor(html, 200));
    }

    [Fact]
    public void Zero_words_per_minute_is_treated_as_one()
    {
        Assert.Equal(3, ReadingTimeCalculator.MinutesFor(Words(3), 0));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<p></p>")]
    public void Empty_content_has_no_reading_time(string? html)
    {
        Assert.Null(ReadingTimeCalculator.MinutesFor(html, 200));
    }
}
