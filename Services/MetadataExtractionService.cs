using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading.Tasks;
using Jellyfin.Plugin.Suggester.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Suggester.Services;

/// <summary>
/// Service that extracts structured metadata filters from a user's natural language prompt.
/// Currently uses a simple heuristic parsing; can be replaced with an OpenAI call for more sophisticated extraction.
/// </summary>
public class MetadataExtractionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MetadataExtractionService> _logger;

    public MetadataExtractionService(HttpClient httpClient, ILogger<MetadataExtractionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Extracts metadata filters from the user's prompt.
    /// </summary>
    /// <param name="prompt">The free‑form user prompt.</param>
    /// <param name="config">Plugin configuration (currently unused, but kept for future OpenAI integration).</param>
    /// <returns>A <see cref="MetadataFilters"/> instance containing parsed criteria.</returns>
    public async Task<MetadataFilters> ExtractMetadataAsync(string prompt, PluginConfiguration config)
    {
        // NOTE: This is a lightweight heuristic implementation. In a production scenario you would
        // call the OpenAI API with a structured prompt and parse the JSON response.
        // For now we attempt to pull out years and simple genre keywords.
        var filters = new MetadataFilters();

        try
        {
            // Extract year range like "1990-2000", decades like "90s", or "after 2010"/"before 2005".
            var yearRangeMatch = Regex.Match(prompt, @"(\d{4})\s*[-–]\s*(\d{4})");
            if (yearRangeMatch.Success)
            {
                if (int.TryParse(yearRangeMatch.Groups[1].Value, out var from))
                    filters.YearFrom = from;
                if (int.TryParse(yearRangeMatch.Groups[2].Value, out var to))
                    filters.YearTo = to;
            }
            else
            {
                // e.g., "90s" -> 1990-1999. We only handle two-digit decades here.
                var decadeMatch = Regex.Match(prompt, @"\b(\d{2})s\b", RegexOptions.IgnoreCase);
                if (decadeMatch.Success && int.TryParse(decadeMatch.Groups[1].Value, out var decadePrefix))
                {
                    // Interpret 00..20 as 2000s..2020s, otherwise 1900s (e.g., 90s -> 1990s)
                    var baseCentury = decadePrefix <= 20 ? 2000 : 1900;
                    filters.YearFrom = baseCentury + decadePrefix; // 90 -> 1990, 00 -> 2000
                    filters.YearTo = filters.YearFrom + 9;
                }
                else
                {
                    var afterMatch = Regex.Match(prompt, @"after\s+(\d{4})", RegexOptions.IgnoreCase);
                    if (afterMatch.Success && int.TryParse(afterMatch.Groups[1].Value, out var afterYear))
                    {
                        // Inclusive lower bound to align with tests
                        filters.YearFrom = afterYear;
                    }
                    var beforeMatch = Regex.Match(prompt, @"before\s+(\d{4})", RegexOptions.IgnoreCase);
                    if (beforeMatch.Success && int.TryParse(beforeMatch.Groups[1].Value, out var beforeYear))
                    {
                        // Inclusive upper bound
                        filters.YearTo = beforeYear;
                    }
                }
            }

            // Genre extraction: map regex patterns to canonical names to avoid double spaces.
            var genrePatterns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [@"\baction\b"] = "Action",
                [@"\bcomedy\b"] = "Comedy",
                [@"\bdrama\b"] = "Drama",
                [@"\bthriller\b"] = "Thriller",
                [@"\bhorror\b"] = "Horror",
                [@"\bsci[- ]?fi\b"] = "Sci Fi",
                [@"\bfantasy\b"] = "Fantasy",
                [@"\bromance\b"] = "Romance",
                [@"\bdocumentary\b"] = "Documentary",
                [@"\banimation\b"] = "Animation",
            };
            var genresFound = new List<string>();
            foreach (var kv in genrePatterns)
            {
                if (Regex.IsMatch(prompt, kv.Key, RegexOptions.IgnoreCase))
                {
                    genresFound.Add(kv.Value);
                }
            }
            if (genresFound.Count > 0)
                filters.Genres = genresFound;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract metadata filters from prompt: {Prompt}", prompt);
        }

        // Placeholder for future OpenAI based extraction – keep async signature.
        await Task.CompletedTask;
        return filters;
    }
}

/// <summary>
/// Simple DTO representing the filters extracted from a user prompt.
/// </summary>
public class MetadataFilters
{
    /// <summary>
    /// List of genre names to filter by. Empty or null means no genre restriction.
    /// </summary>
    public List<string>? Genres { get; set; }

    /// <summary>
    /// Minimum release year (inclusive). Null means no lower bound.
    /// </summary>
    public int? YearFrom { get; set; }

    /// <summary>
    /// Maximum release year (inclusive). Null means no upper bound.
    /// </summary>
    public int? YearTo { get; set; }

    // Additional fields such as Persons, Keywords could be added later.
}
