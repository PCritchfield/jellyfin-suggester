using System.Collections.Generic;
using FluentAssertions;
using Jellyfin.Plugin.Suggester.Services;
using Xunit;

namespace Jellyfin.Plugin.Suggester.Tests;

public class MovieInfoTests
{
    [Fact]
    public void ToString_Formats_With_Title_Year_Genres_Rating()
    {
        var mi = new MovieInfo
        {
            Title = "The Matrix",
            Year = 1999,
            Genres = new List<string> { "Action", "Sci-Fi" },
            CommunityRating = 8.7f
        };

        var s = mi.ToString();

        s.Should().Contain("The Matrix");
        s.Should().Contain("(1999)");
        s.Should().Contain("[Action, Sci-Fi]");
        s.Should().Contain("Rating: 8.7");
    }

    [Fact]
    public void ToString_Skips_Missing_Optional_Fields()
    {
        var mi = new MovieInfo
        {
            Title = "Primer"
        };

        var s = mi.ToString();

        s.Should().Be("Primer");
    }
}
