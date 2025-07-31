using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.Suggester.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Suggester.Services;

/// <summary>
/// Service for generating movie recommendations using OpenAI's API.
/// Handles communication with OpenAI and formats responses for Jellyfin users.
/// </summary>
public class OpenAiRecommendationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiRecommendationService> _logger;
    private const string OpenAiApiUrl = "https://api.openai.com/v1/chat/completions";

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiRecommendationService"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for API requests.</param>
    /// <param name="logger">Logger instance.</param>
    public OpenAiRecommendationService(HttpClient httpClient, ILogger<OpenAiRecommendationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Generates movie recommendations based on a user's library.
    /// </summary>
    /// <param name="userMovies">List of movies in the user's library.</param>
    /// <param name="config">Plugin configuration with OpenAI settings.</param>
    /// <returns>A list of movie recommendations.</returns>
    public async Task<List<MovieRecommendation>> GenerateRecommendationsAsync(
        List<MovieInfo> userMovies, 
        PluginConfiguration config)
    {
        try
        {
            if (string.IsNullOrEmpty(config.OpenAiApiKey))
            {
                throw new InvalidOperationException("OpenAI API key is not configured");
            }

            if (!userMovies.Any())
            {
                _logger.LogWarning("No movies provided for recommendation generation");
                return new List<MovieRecommendation>();
            }

            _logger.LogInformation("Generating recommendations for {MovieCount} movies using model {Model}", 
                userMovies.Count, config.OpenAiModel);

            var prompt = BuildPrompt(userMovies, config);
            var response = await CallOpenAiApiAsync(prompt, config);
            var recommendations = ParseRecommendations(response, config.IncludeDescriptions);

            _logger.LogInformation("Generated {RecommendationCount} recommendations", recommendations.Count);
            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recommendations");
            throw;
        }
    }

    /// <summary>
    /// Builds the prompt for OpenAI based on user's movie library and configuration.
    /// </summary>
    /// <param name="userMovies">User's movie collection.</param>
    /// <param name="config">Plugin configuration.</param>
    /// <returns>Formatted prompt string.</returns>
    private string BuildPrompt(List<MovieInfo> userMovies, PluginConfiguration config)
    {
        // TODO: Implement intelligent movie selection for prompt
        // Consider factors like: recent additions, high ratings, genre diversity
        var selectedMovies = userMovies
            .OrderByDescending(m => m.CommunityRating ?? 0)
            .Take(20) // Limit to avoid token limits
            .ToList();

        var movieList = string.Join(", ", selectedMovies.Select(m => m.ToString()));
        
        var prompt = config.PromptTemplate
            .Replace("{movies}", movieList)
            .Replace("{count}", config.MaxRecommendations.ToString());

        if (config.IncludeDescriptions)
        {
            prompt += " Include a brief explanation for each recommendation.";
        }

        prompt += " Format the response as a numbered list with movie titles and years.";

        _logger.LogDebug("Generated prompt with {SelectedMovieCount} movies", selectedMovies.Count);
        return prompt;
    }

    /// <summary>
    /// Calls the OpenAI API with the generated prompt.
    /// </summary>
    /// <param name="prompt">The prompt to send to OpenAI.</param>
    /// <param name="config">Plugin configuration.</param>
    /// <returns>The API response content.</returns>
    private async Task<string> CallOpenAiApiAsync(string prompt, PluginConfiguration config)
    {
        // TODO: Implement proper error handling and retry logic
        // TODO: Add request/response logging for debugging
        // TODO: Implement token usage tracking and cost monitoring

        var requestBody = new
        {
            model = config.OpenAiModel,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "You are a movie recommendation expert. Provide thoughtful, diverse recommendations based on the user's existing library."
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            max_tokens = 1000,
            temperature = 0.7
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.OpenAiApiKey}");

        var response = await _httpClient.PostAsync(OpenAiApiUrl, content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("OpenAI API request failed: {StatusCode} - {Error}", 
                response.StatusCode, errorContent);
            throw new HttpRequestException($"OpenAI API request failed: {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        return responseJson
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    /// <summary>
    /// Parses the OpenAI response into structured movie recommendations.
    /// </summary>
    /// <param name="response">Raw response from OpenAI.</param>
    /// <param name="includeDescriptions">Whether descriptions are included.</param>
    /// <returns>List of parsed movie recommendations.</returns>
    private List<MovieRecommendation> ParseRecommendations(string response, bool includeDescriptions)
    {
        // TODO: Implement robust parsing logic
        // TODO: Handle various response formats from OpenAI
        // TODO: Extract movie titles, years, and descriptions accurately
        // TODO: Add validation for parsed data

        var recommendations = new List<MovieRecommendation>();
        
        if (string.IsNullOrEmpty(response))
        {
            _logger.LogWarning("Empty response from OpenAI");
            return recommendations;
        }

        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || !char.IsDigit(trimmedLine[0]))
                continue;

            // Basic parsing - this needs improvement
            var recommendation = new MovieRecommendation
            {
                Title = ExtractTitleFromLine(trimmedLine),
                Year = ExtractYearFromLine(trimmedLine),
                Description = includeDescriptions ? ExtractDescriptionFromLine(trimmedLine) : null,
                Source = "OpenAI"
            };

            if (!string.IsNullOrEmpty(recommendation.Title))
            {
                recommendations.Add(recommendation);
            }
        }

        _logger.LogDebug("Parsed {Count} recommendations from response", recommendations.Count);
        return recommendations;
    }

    /// <summary>
    /// Extracts movie title from a recommendation line.
    /// </summary>
    /// <param name="line">The recommendation line.</param>
    /// <returns>Extracted movie title.</returns>
    private string ExtractTitleFromLine(string line)
    {
        // TODO: Implement robust title extraction
        // Handle formats like: "1. Movie Title (2023) - Description"
        
        // Simple implementation for now
        var parts = line.Split('-', 2);
        var titlePart = parts[0].Trim();
        
        // Remove numbering
        var dotIndex = titlePart.IndexOf('.');
        if (dotIndex >= 0 && dotIndex < 5)
        {
            titlePart = titlePart.Substring(dotIndex + 1).Trim();
        }

        // Remove year in parentheses for title extraction
        var parenIndex = titlePart.LastIndexOf('(');
        if (parenIndex > 0)
        {
            titlePart = titlePart.Substring(0, parenIndex).Trim();
        }

        return titlePart;
    }

    /// <summary>
    /// Extracts release year from a recommendation line.
    /// </summary>
    /// <param name="line">The recommendation line.</param>
    /// <returns>Extracted year or null if not found.</returns>
    private int? ExtractYearFromLine(string line)
    {
        // TODO: Implement robust year extraction
        // Look for patterns like (2023) or (1999)
        
        var parenStart = line.LastIndexOf('(');
        var parenEnd = line.IndexOf(')', parenStart);
        
        if (parenStart >= 0 && parenEnd > parenStart)
        {
            var yearText = line.Substring(parenStart + 1, parenEnd - parenStart - 1);
            if (int.TryParse(yearText, out var year) && year >= 1900 && year <= DateTime.Now.Year + 5)
            {
                return year;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts description from a recommendation line.
    /// </summary>
    /// <param name="line">The recommendation line.</param>
    /// <returns>Extracted description or null if not found.</returns>
    private string? ExtractDescriptionFromLine(string line)
    {
        // TODO: Implement robust description extraction
        // Look for text after " - " or similar separators
        
        var dashIndex = line.IndexOf(" - ");
        if (dashIndex >= 0 && dashIndex < line.Length - 3)
        {
            return line.Substring(dashIndex + 3).Trim();
        }

        return null;
    }
}

/// <summary>
/// Represents a movie recommendation generated by the AI service.
/// </summary>
public class MovieRecommendation
{
    /// <summary>
    /// Gets or sets the recommended movie title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the movie's release year.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the recommendation description/reasoning.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the source of the recommendation (e.g., "OpenAI").
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confidence score for this recommendation.
    /// </summary>
    public float? ConfidenceScore { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the recommendation.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
