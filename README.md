# Jellyfin Movie Suggester Plugin

A Jellyfin native plugin that integrates with OpenAI to generate intelligent movie recommendations based on your library.

## Overview

This plugin analyzes your Jellyfin movie library and uses OpenAI's API to generate personalized movie recommendations. It provides a clean separation between Jellyfin library interaction, OpenAI communication, and configuration management.

## Features

- **Native Jellyfin Integration**: Built as a proper Jellyfin plugin using .NET 8
- **OpenAI-Powered Recommendations**: Uses GPT models to analyze your library and suggest similar movies
- **Configurable Settings**: Web-based configuration for API keys, models, and recommendation preferences
- **Clean Architecture**: Separated services for library access, AI communication, and API endpoints

## Project Structure

```
├── Api/
│   └── SuggesterController.cs     # API endpoints for recommendations
├── Configuration/
│   ├── PluginConfiguration.cs     # Plugin settings model
│   └── configPage.html           # Web configuration interface
├── Services/
│   ├── JellyfinLibraryService.cs  # Jellyfin library interaction
│   └── OpenAiRecommendationService.cs # OpenAI API communication
├── Plugin.cs                      # Main plugin entry point
├── build.yaml                     # Plugin manifest
└── Jellyfin.Plugin.Suggester.csproj # .NET project file
```

## Development Status

✅ **Completed:**
- Project scaffolding with proper .NET structure
- Plugin manifest and metadata configuration
- Configuration system with web interface
- Service layer architecture (Jellyfin + OpenAI)
- API controller structure with placeholder endpoints
- Comprehensive documentation and TODOs

🔧 **In Progress:**
- Fixing Jellyfin API compatibility issues
- Resolving build errors and dependency conflicts

📋 **TODO:**
- Update Jellyfin library service to use current API patterns
- Implement proper dependency injection
- Add robust error handling and validation
- Implement recommendation caching
- Add unit tests
- Create deployment documentation

## Configuration

The plugin requires the following configuration:

- **OpenAI API Key**: Your OpenAI API key from [OpenAI Platform](https://platform.openai.com/api-keys)
- **OpenAI Model**: Choose between GPT-3.5 Turbo (recommended), GPT-4, or GPT-4 Turbo
- **Max Recommendations**: Number of suggestions to generate (1-20)
- **Include Descriptions**: Whether to include explanations for recommendations
- **Custom Prompt Template**: Customize how the AI generates recommendations

## Building

```bash
# Install .NET 8 SDK (if not already installed)
# Build the plugin
dotnet build

# Publish for deployment
dotnet publish -c Release
```

## Installation

1. Build the plugin using the commands above
2. Copy the compiled DLL to your Jellyfin plugins directory
3. Restart Jellyfin
4. Configure the plugin through the Jellyfin admin interface

## API Endpoints

- `POST /Suggester` - Generate new recommendations for a user
- `GET /Suggester` - Retrieve cached recommendations (planned)

## Architecture Notes

### Service Layer
- **JellyfinLibraryService**: Handles all interactions with the Jellyfin library, including movie retrieval and filtering
- **OpenAiRecommendationService**: Manages OpenAI API communication, prompt building, and response parsing
- **SuggesterController**: Provides API endpoints and orchestrates the recommendation process

### Configuration Management
- Plugin settings are managed through Jellyfin's built-in configuration system
- Web-based configuration page provides user-friendly setup
- Settings include API keys, model selection, and recommendation preferences

### Error Handling
- Comprehensive logging throughout all services
- Graceful degradation when OpenAI API is unavailable
- Input validation and sanitization

## Contributing

This plugin follows standard .NET development practices:

- Use meaningful variable and method names
- Add XML documentation for public APIs
- Include comprehensive error handling
- Write unit tests for core functionality
- Follow Jellyfin plugin development guidelines

## License

See LICENSE file for details.
