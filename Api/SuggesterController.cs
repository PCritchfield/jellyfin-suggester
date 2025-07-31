using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.Suggester.Services;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Suggester.Api;

/// <summary>
/// API controller for movie recommendation endpoints.
/// Provides methods for generating and retrieving movie suggestions.
/// TODO: Implement proper Jellyfin API endpoint registration
/// </summary>
public class SuggesterController
{
    private readonly JellyfinLibraryService _libraryService;
    private readonly OpenAiRecommendationService _recommendationService;
    private readonly ILogger<SuggesterController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuggesterController"/> class.
    /// </summary>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="logger">Logger instance.</param>
    public SuggesterController(ILibraryManager libraryManager, ILogger<SuggesterController> logger)
    {
        // TODO: Implement proper dependency injection for services
        // For now, create loggers manually using LoggerFactory pattern
        var loggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        
        _libraryService = new JellyfinLibraryService(libraryManager, 
            loggerFactory.CreateLogger<JellyfinLibraryService>());
        
        // TODO: Inject HttpClient through DI container
        // For now, create a basic HttpClient - this should be improved
        var httpClient = new System.Net.Http.HttpClient();
        _recommendationService = new OpenAiRecommendationService(httpClient, 
            loggerFactory.CreateLogger<OpenAiRecommendationService>());
        
        _logger = logger;
    }

    /// <summary>
    /// Generates movie recommendations for a specific user.
    /// </summary>
    /// <param name="request">The recommendation request.</param>
    /// <returns>A list of movie recommendations.</returns>
    public async Task<RecommendationResponse> Post(GenerateRecommendationsRequest request)
    {
        try
        {
            _logger.LogInformation("Generating recommendations for user {UserId}", request.UserId);

            // Validate request
            if (request.UserId == Guid.Empty)
            {
                throw new ArgumentException("Valid UserId is required", nameof(request.UserId));
            }

            // Get plugin configuration
            var config = Plugin.Instance?.Configuration;
            if (config == null)
            {
                throw new InvalidOperationException("Plugin configuration not available");
            }

            if (string.IsNullOrEmpty(config.OpenAiApiKey))
            {
                return new RecommendationResponse
                {
                    Success = false,
                    ErrorMessage = "OpenAI API key is not configured. Please configure the plugin settings."
                };
            }

            // Get user's movie library
            var userMovies = await _libraryService.GetUserMoviesAsync(
                request.UserId, 
                request.MaxLibraryMovies ?? 100);

            if (!userMovies.Any())
            {
                return new RecommendationResponse
                {
                    Success = false,
                    ErrorMessage = "No movies found in user's library"
                };
            }

            // Generate recommendations using OpenAI
            var recommendations = await _recommendationService.GenerateRecommendationsAsync(
                userMovies, config);

            _logger.LogInformation("Successfully generated {Count} recommendations for user {UserId}", 
                recommendations.Count, request.UserId);

            return new RecommendationResponse
            {
                Success = true,
                Recommendations = recommendations,
                GeneratedAt = DateTime.UtcNow,
                BasedOnMovieCount = userMovies.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recommendations for user {UserId}", request.UserId);
            
            return new RecommendationResponse
            {
                Success = false,
                ErrorMessage = $"Failed to generate recommendations: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets cached recommendations for a user (placeholder for future caching implementation).
    /// </summary>
    /// <param name="request">The get recommendations request.</param>
    /// <returns>Cached recommendations or empty response.</returns>
    public async Task<RecommendationResponse> Get(GetRecommendationsRequest request)
    {
        // TODO: Implement caching mechanism for recommendations
        // TODO: Store recommendations in Jellyfin database or external cache
        // TODO: Add expiration logic for cached recommendations
        
        _logger.LogInformation("Getting cached recommendations for user {UserId}", request.UserId);
        
        // For now, return empty response indicating no cached recommendations
        return new RecommendationResponse
        {
            Success = true,
            Recommendations = new List<MovieRecommendation>(),
            ErrorMessage = "No cached recommendations available. Use POST to generate new recommendations."
        };
    }
}

/// <summary>
/// Request model for generating new movie recommendations.
/// </summary>
public class GenerateRecommendationsRequest
{
    /// <summary>
    /// Gets or sets the user ID to generate recommendations for.
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of movies to analyze from user's library.
    /// </summary>
    public int? MaxLibraryMovies { get; set; }

    /// <summary>
    /// Gets or sets whether to force regeneration even if cached recommendations exist.
    /// </summary>
    public bool ForceRegenerate { get; set; } = false;
}

/// <summary>
/// Request model for getting existing recommendations.
/// </summary>
public class GetRecommendationsRequest
{
    /// <summary>
    /// Gets or sets the user ID to get recommendations for.
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets whether to include expired cached recommendations.
    /// </summary>
    public bool IncludeExpired { get; set; } = false;
}

/// <summary>
/// Response model for recommendation requests.
/// </summary>
public class RecommendationResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the request was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the list of movie recommendations.
    /// </summary>
    public List<MovieRecommendation> Recommendations { get; set; } = new();

    /// <summary>
    /// Gets or sets the error message if the request failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets when the recommendations were generated.
    /// </summary>
    public DateTime? GeneratedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of movies the recommendations were based on.
    /// </summary>
    public int BasedOnMovieCount { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the recommendation process.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
