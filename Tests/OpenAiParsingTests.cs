using System.Collections.Generic;
using FluentAssertions;
using Jellyfin.Plugin.Suggester.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.Suggester.Tests;

public class OpenAiParsingTests
{
    private readonly OpenAiRecommendationService _svc;

    public OpenAiParsingTests()
    {
        var http = new System.Net.Http.HttpClient(); // not used here
        var logger = NullLogger<OpenAiRecommendationService>.Instance;
        _svc = new OpenAiRecommendationService(http, logger);
    }

    [Fact]
    public void ParseRecommendations_WithDescriptions_ParsesTitleYearAndDescription()
    {
        var response = @"1. The Matrix (1999) - A mind-bending sci-fi classic
2. Arrival (2016) - Thoughtful first-contact drama
3. Airplane! (1980) - Absurdist comedy";

        var list = _svc.ParseRecommendations(response, includeDescriptions: true);

        list.Should().HaveCount(3);
        list[0].Title.Should().Be("The Matrix");
        list[0].Year.Should().Be(1999);
        list[0].Description.Should().NotBeNull();
        list[0].Description!.ToLowerInvariant().Should().Contain("mind-bending");
    }

    [Fact]
    public void ParseRecommendations_WithoutDescriptions_SetsDescriptionNull()
    {
        var response = @"1. The Matrix (1999) - A mind-bending sci-fi classic
2. Arrival (2016) - Thoughtful first-contact drama";

        var list = _svc.ParseRecommendations(response, includeDescriptions: false);

        list.Should().HaveCount(2);
        list[1].Title.Should().Be("Arrival");
        list[1].Year.Should().Be(2016);
        list[1].Description.Should().BeNull();
    }

    [Fact]
    public void ParseRecommendations_Ignores_NonNumbered_Lines()
    {
        var response = @"Summary: Here are some picks
1. The Godfather (1972)
- bullet
2. The Dark Knight (2008)";

        var list = _svc.ParseRecommendations(response, includeDescriptions: false);

        list.Should().HaveCount(2);
        list[0].Title.Should().Be("The Godfather");
        list[1].Title.Should().Be("The Dark Knight");
    }
}
