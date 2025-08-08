using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Jellyfin.Plugin.Suggester.Configuration;
using Jellyfin.Plugin.Suggester.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.Suggester.Tests;

public class MetadataExtractionServiceTests
{
    private readonly MetadataExtractionService _svc;
    private readonly PluginConfiguration _config = new();

    public MetadataExtractionServiceTests()
    {
        var http = new HttpClient(); // not used by heuristic implementation
        var logger = NullLogger<MetadataExtractionService>.Instance;
        _svc = new MetadataExtractionService(http, logger);
    }

    [Theory]
    [InlineData("I want a comedy from the 90s", new[] { "Comedy" }, 1990, 1999)]
    [InlineData("Show me sci-fi after 2010", new[] { "Sci Fi" }, 2010, null)]
    [InlineData("Give me any drama", new[] { "Drama" }, null, null)]
    [InlineData("thriller before 2005", new[] { "Thriller" }, null, 2005)]
    public async Task ExtractMetadata_Parses_Genres_And_Years(string prompt, string[] genres, int? from, int? to)
    {
        var filters = await _svc.ExtractMetadataAsync(prompt, _config);

        // Genres: order not guaranteed; compare as sets
        if (genres.Length == 0)
        {
            (filters.Genres == null || !filters.Genres.Any()).Should().BeTrue();
        }
        else
        {
            filters.Genres.Should().NotBeNull();
            filters.Genres!.Select(g => g).Should().BeEquivalentTo(genres);
        }

        filters.YearFrom.Should().Be(from);
        filters.YearTo.Should().Be(to);
    }

    [Fact]
    public async Task ExtractMetadata_NoMatches_ReturnsEmptyFilters()
    {
        var filters = await _svc.ExtractMetadataAsync("something cozy for a rainy day", _config);
        (filters.Genres == null || !filters.Genres.Any()).Should().BeTrue();
        filters.YearFrom.Should().BeNull();
        filters.YearTo.Should().BeNull();
    }
}
