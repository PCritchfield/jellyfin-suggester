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
    /// </summary>
    public string PromptTemplate { get; set; } = 
        "Based on the following movies in the user's library: {movies}, " +
        "recommend {count} similar movies that the user might enjoy. " +
        "Focus on genre, themes, and quality. Provide brief explanations for each recommendation.";
}
