using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Jellyfin.Plugin.Suggester.Services;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Suggester.Api;

/// <summary>
/// API controller for movie recommendation endpoints.
/// Provides methods for generating and retrieving movie suggestions.
/// </summary>
[ApiController]
[Route("Suggester")]
[Authorize]
public class SuggesterController : ControllerBase
{
    private readonly JellyfinLibraryService _libraryService;
    private readonly OpenAiRecommendationService _recommendationService;
    private readonly MetadataExtractionService _metadataExtractionService;
    private readonly ILogger<SuggesterController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuggesterController"/> class.
    /// </summary>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="logger">Logger instance.</param>
    public SuggesterController(
        ILibraryManager libraryManager, 
        ILogger<SuggesterController> logger)
    {
        // Create appropriate logger instances for each service
        var loggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        
        _libraryService = new JellyfinLibraryService(libraryManager, 
            loggerFactory.CreateLogger<JellyfinLibraryService>());
        
        // Create HttpClient directly - Jellyfin plugins don't have IHttpClientFactory by default
        var httpClient = new System.Net.Http.HttpClient();
        _recommendationService = new OpenAiRecommendationService(httpClient, 
            loggerFactory.CreateLogger<OpenAiRecommendationService>());
        
        // Instantiate metadata extraction service
        _metadataExtractionService = new MetadataExtractionService(httpClient, 
            loggerFactory.CreateLogger<MetadataExtractionService>());
        
        _logger = logger;
    }

    /// <summary>
    /// Generates movie recommendations for a specific user.
    /// </summary>
    /// <param name="request">The recommendation request.</param>
    /// <returns>A list of movie recommendations.</returns>
    [HttpPost("Generate")]
    public async Task<ActionResult<RecommendationResponse>> Post([FromBody] GenerateRecommendationsRequest request)
    {
        try
        {
            // Extract user ID from claims if not provided in request
            var userId = request.UserId;
            if (userId == Guid.Empty)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out var parsedUserId))
                {
                    userId = parsedUserId;
                }
                else
                {
                    return BadRequest(new RecommendationResponse
                    {
                        Success = false,
                        ErrorMessage = "Unable to determine user ID from authentication context"
                    });
                }
            }

            _logger.LogInformation("Generating recommendations for user {UserId} with prompt: {Prompt}", userId, request.Prompt);

            // Validate prompt
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest(new RecommendationResponse
                {
                    Success = false,
                    ErrorMessage = "Prompt is required. Please describe what you're in the mood for."
                });
            }

            // Update request with resolved user ID
            request.UserId = userId;

            // Get plugin configuration
            var config = Plugin.Instance?.Configuration;
            if (config == null)
            {
                throw new InvalidOperationException("Plugin configuration not available");
            }

            if (string.IsNullOrEmpty(config.OpenAiApiKey))
            {
                return BadRequest(new RecommendationResponse
                {
                    Success = false,
                    ErrorMessage = "OpenAI API key is not configured. Please set it in the plugin settings."
                });
            }

            // Extract metadata filters from user prompt
            var metadataFilters = await _metadataExtractionService.ExtractMetadataAsync(request.Prompt, config);

            // Retrieve user's movies (respect MaxLibraryMovies if provided)
            int maxMovies = request.MaxLibraryMovies ?? config.MaxLibraryMovies;
            var userMovies = await _libraryService.GetUserMoviesAsync(userId, maxMovies);

            // Apply filters to the movie list
            var filteredMovies = ApplyFilters(userMovies, metadataFilters);

            // Generate recommendations using filtered movies and original prompt
            var recommendations = await _recommendationService.GenerateRecommendationsAsync(filteredMovies, config, request.Prompt);

            return Ok(new RecommendationResponse
            {
                Success = true,
                Recommendations = recommendations,
                GeneratedAt = DateTime.UtcNow,
                BasedOnMovieCount = filteredMovies.Count,
                Metadata = new Dictionary<string, object>
                {
                    { "MetadataFilters", metadataFilters }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recommendations for user {UserId}", request.UserId);
            
            // TODO: Implement fallback to cached recommendations on API failure
            return StatusCode(500, new RecommendationResponse
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred while generating recommendations."
            });
        }
    }

    internal static List<MovieInfo> ApplyFilters(List<MovieInfo> movies, MetadataFilters metadataFilters)
    {
        // Simple filtering based on genres and year range
        var filtered = movies.Where(m =>
            (metadataFilters.Genres == null || !metadataFilters.Genres.Any() || m.Genres.Any(g => metadataFilters.Genres.Contains(g, StringComparer.OrdinalIgnoreCase))) &&
            (metadataFilters.YearFrom == null || (m.Year.HasValue && m.Year >= metadataFilters.YearFrom)) &&
            (metadataFilters.YearTo == null || (m.Year.HasValue && m.Year <= metadataFilters.YearTo))
        ).ToList();
        return filtered;
    }

    /// <summary>
    /// Gets cached recommendations for a user (placeholder for future caching implementation).
    /// </summary>
    /// <param name="request">The get recommendations request.</param>
    /// <returns>Cached recommendations or empty response.</returns>
    [HttpGet("Cached")]
    public async Task<ActionResult<RecommendationResponse>> Get([FromQuery] GetRecommendationsRequest request)
    {
        // Extract user ID from claims if not provided in request
        var userId = request.UserId;
        if (userId == Guid.Empty)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var parsedUserId))
            {
                userId = parsedUserId;
            }
            else
            {
                return BadRequest(new RecommendationResponse
                {
                    Success = false,
                    ErrorMessage = "Unable to determine user ID from authentication context"
                });
            }
        }

        // TODO: Implement caching mechanism for recommendations
        // TODO: Store recommendations in Jellyfin database or external cache
        // TODO: Add expiration logic for cached recommendations
        
        _logger.LogInformation("Getting cached recommendations for user {UserId}", userId);
        
        // For now, return empty response indicating no cached recommendations
        return Ok(new RecommendationResponse
        {
            Success = true,
            Recommendations = new List<MovieRecommendation>(),
            ErrorMessage = "No cached recommendations available. Use POST to generate new recommendations."
        });
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
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the user's natural language prompt describing what they want to watch.
    /// </summary>
    [Required]
    public string Prompt { get; set; } = string.Empty;

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
