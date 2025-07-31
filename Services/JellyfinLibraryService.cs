using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Suggester.Services;

/// <summary>
/// Service for interacting with the Jellyfin library to retrieve movie information.
/// Provides methods to fetch user's movie collection for recommendation analysis.
/// </summary>
public class JellyfinLibraryService
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<JellyfinLibraryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinLibraryService"/> class.
    /// </summary>
    /// <param name="libraryManager">The Jellyfin library manager.</param>
    /// <param name="logger">The logger instance.</param>
    public JellyfinLibraryService(ILibraryManager libraryManager, ILogger<JellyfinLibraryService> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a user's movie library for recommendation analysis.
    /// </summary>
    /// <param name="userId">The user ID to get movies for.</param>
    /// <param name="maxMovies">Maximum number of movies to retrieve (default: 100).</param>
    /// <returns>A list of movie information suitable for AI analysis.</returns>
    public async Task<List<MovieInfo>> GetUserMoviesAsync(Guid userId, int maxMovies = 100)
    {
        try
        {
            _logger.LogInformation("Retrieving movies for user {UserId}, max count: {MaxMovies}", userId, maxMovies);

            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie },
                Recursive = true,
                Limit = maxMovies,
                OrderBy = new[] { (ItemSortBy.Random, SortOrder.Ascending) }
            };
            
            // TODO: Implement proper user-specific filtering
            // For now, get all movies and filter later if needed

            var movies = _libraryManager.GetItemsResult(query);
            
            var movieInfos = movies.Items
                .OfType<Movie>()
                .Select(movie => new MovieInfo
                {
                    Title = movie.Name,
                    Year = movie.ProductionYear,
                    Overview = movie.Overview,
                    Genres = movie.Genres?.ToList() ?? new List<string>(),
                    CommunityRating = movie.CommunityRating,
                    OfficialRating = movie.OfficialRating,
                    Studios = movie.Studios?.ToList() ?? new List<string>()
                })
                .Where(info => !string.IsNullOrEmpty(info.Title))
                .ToList();

            _logger.LogInformation("Retrieved {Count} movies for user {UserId}", movieInfos.Count, userId);
            return movieInfos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving movies for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Gets recently added movies for a user.
    /// Useful for generating recommendations based on recent viewing patterns.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="maxMovies">Maximum number of recent movies to retrieve.</param>
    /// <returns>A list of recently added movie information.</returns>
    public async Task<List<MovieInfo>> GetRecentlyAddedMoviesAsync(Guid userId, int maxMovies = 20)
    {
        try
        {
            _logger.LogInformation("Retrieving recently added movies for user {UserId}", userId);

            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie },
                Recursive = true,
                Limit = maxMovies,
                OrderBy = new[] { (ItemSortBy.DateCreated, SortOrder.Descending) }
            };
            
            // TODO: Implement proper user-specific filtering
            // For now, get all movies and filter later if needed

            var movies = _libraryManager.GetItemsResult(query);
            
            var movieInfos = movies.Items
                .OfType<Movie>()
                .Select(movie => new MovieInfo
                {
                    Title = movie.Name,
                    Year = movie.ProductionYear,
                    Overview = movie.Overview,
                    Genres = movie.Genres?.ToList() ?? new List<string>(),
                    CommunityRating = movie.CommunityRating,
                    DateAdded = movie.DateCreated
                })
                .Where(info => !string.IsNullOrEmpty(info.Title))
                .ToList();

            _logger.LogInformation("Retrieved {Count} recently added movies for user {UserId}", movieInfos.Count, userId);
            return movieInfos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recently added movies for user {UserId}", userId);
            throw;
        }
    }
}

/// <summary>
/// Represents movie information for recommendation analysis.
/// Contains essential metadata needed by the AI recommendation service.
/// </summary>
public class MovieInfo
{
    /// <summary>
    /// Gets or sets the movie title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the production year.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the movie overview/plot summary.
    /// </summary>
    public string? Overview { get; set; }

    /// <summary>
    /// Gets or sets the list of genres.
    /// </summary>
    public List<string> Genres { get; set; } = new();

    /// <summary>
    /// Gets or sets the community rating (e.g., IMDb rating).
    /// </summary>
    public float? CommunityRating { get; set; }

    /// <summary>
    /// Gets or sets the official rating (e.g., PG-13, R).
    /// </summary>
    public string? OfficialRating { get; set; }

    /// <summary>
    /// Gets or sets the list of production studios.
    /// </summary>
    public List<string> Studios { get; set; } = new();

    /// <summary>
    /// Gets or sets the date the movie was added to the library.
    /// </summary>
    public DateTime DateAdded { get; set; }

    /// <summary>
    /// Returns a formatted string representation for AI analysis.
    /// </summary>
    /// <returns>A formatted movie description.</returns>
    public override string ToString()
    {
        var parts = new List<string> { Title };
        
        if (Year.HasValue)
            parts.Add($"({Year})");
            
        if (Genres.Any())
            parts.Add($"[{string.Join(", ", Genres)}]");
            
        if (CommunityRating.HasValue)
            parts.Add($"Rating: {CommunityRating:F1}");

        return string.Join(" ", parts);
    }
}
