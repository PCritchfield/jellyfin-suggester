using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jellyfin.Plugin.Suggester.Api;
using Jellyfin.Plugin.Suggester.Services;
using Xunit;

namespace Jellyfin.Plugin.Suggester.Tests;

public class ApplyFiltersTests
{
    private static List<MovieInfo> SampleMovies() => new()
    {
        new MovieInfo { Title = "The Matrix", Year = 1999, Genres = new List<string>{ "Action", "Sci-Fi" } },
        new MovieInfo { Title = "The Godfather", Year = 1972, Genres = new List<string>{ "Crime", "Drama" } },
        new MovieInfo { Title = "Arrival", Year = 2016, Genres = new List<string>{ "Sci Fi", "Drama" } },
        new MovieInfo { Title = "Airplane!", Year = 1980, Genres = new List<string>{ "Comedy" } },
        new MovieInfo { Title = "Unknown Year", Genres = new List<string>{ "Comedy" } },
    };

    [Fact]
    public void Filters_By_Genre_Only()
    {
        var movies = SampleMovies();
        var filters = new MetadataFilters { Genres = new List<string> { "Comedy" } };

        var result = SuggesterController.ApplyFilters(movies, filters);

        result.Select(m => m.Title).Should().BeEquivalentTo(new[]{ "Airplane!", "Unknown Year" });
    }

    [Fact]
    public void Filters_By_YearFrom_Inclusive()
    {
        var movies = SampleMovies();
        var filters = new MetadataFilters { YearFrom = 1999 };

        var result = SuggesterController.ApplyFilters(movies, filters);

        result.Select(m => m.Title).Should().BeEquivalentTo(new[]{ "The Matrix", "Arrival" });
    }

    [Fact]
    public void Filters_By_YearTo_Inclusive()
    {
        var movies = SampleMovies();
        var filters = new MetadataFilters { YearTo = 1980 };

        var result = SuggesterController.ApplyFilters(movies, filters);

        result.Select(m => m.Title).Should().BeEquivalentTo(new[]{ "The Godfather", "Airplane!" });
    }

    [Fact]
    public void Filters_By_Genre_And_Year_Range()
    {
        var movies = SampleMovies();
        var filters = new MetadataFilters { Genres = new List<string> { "Sci Fi" }, YearFrom = 1990, YearTo = 2010 };

        var result = SuggesterController.ApplyFilters(movies, filters);

        result.Select(m => m.Title).Should().BeEquivalentTo(new[]{ "The Matrix" });
    }

    [Fact]
    public void Case_Insensitive_Genre_Match()
    {
        var movies = SampleMovies();
        var filters = new MetadataFilters { Genres = new List<string> { "sCi fI" } };

        var result = SuggesterController.ApplyFilters(movies, filters);

        result.Select(m => m.Title).Should().BeEquivalentTo(new[]{ "The Matrix", "Arrival" });
    }

    [Fact]
    public void Movies_Without_Year_Are_Excluded_When_Year_Filter_Present()
    {
        var movies = SampleMovies();
        var filters = new MetadataFilters { Genres = new List<string> { "Comedy" }, YearFrom = 1970 };

        var result = SuggesterController.ApplyFilters(movies, filters);

        result.Select(m => m.Title).Should().BeEquivalentTo(new[]{ "Airplane!" });
    }
}
