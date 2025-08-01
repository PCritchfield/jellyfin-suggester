using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Suggester.Configuration;

/// <summary>
/// Plugin configuration for Movie Suggester.
/// Stores OpenAI API settings and recommendation preferences.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the OpenAI API key.
    /// Required for generating movie recommendations.
    /// </summary>
    public string OpenAiApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OpenAI model to use for recommendations.
    /// Default is gpt-3.5-turbo for cost efficiency.
    /// </summary>
    public string OpenAiModel { get; set; } = "gpt-3.5-turbo";

    /// <summary>
    /// Gets or sets the maximum number of recommendations to generate.
    /// Default is 5 recommendations per request.
    /// </summary>
    public int MaxRecommendations { get; set; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether to include movie descriptions in recommendations.
    /// Default is true to provide context for suggestions.
    /// </summary>
    public bool IncludeDescriptions { get; set; } = true;

    /// <summary>
    /// Gets or sets the recommendation prompt template.
    /// Used to customize how the AI generates recommendations.
    /// Supports placeholders: {movies}, {prompt}, {count}
    /// </summary>
    public string PromptTemplate { get; set; } = 
        "You are a movie sommelier with expertise in film recommendations. " +
        "Based on this catalog from the user's library: {movies}. " +
        "When a user says '{prompt}', recommend {count} films from their library that best match their request. " +
        "Focus on matching their mood, genre preferences, and specific criteria mentioned.";
}
