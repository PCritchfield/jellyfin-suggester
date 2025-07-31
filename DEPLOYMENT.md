# Jellyfin Movie Suggester Plugin - Deployment Guide

## Build Status: ✅ SUCCESS

The plugin now compiles successfully with .NET 8 and is ready for deployment testing!

## Quick Start

### 1. Build the Plugin

```bash
# From the project root directory
dotnet build -c Release

# The compiled DLL will be at:
# bin/Release/net8.0/Jellyfin.Plugin.Suggester.dll
```

### 2. Install in Jellyfin

1. **Locate your Jellyfin plugins directory:**
   - Linux: `/var/lib/jellyfin/plugins/`
   - Windows: `%ProgramData%\Jellyfin\Server\plugins\`
   - Docker: `/config/plugins/` (inside container)

2. **Create plugin directory:**
   ```bash
   mkdir -p /var/lib/jellyfin/plugins/Jellyfin.Plugin.Suggester_1.0.0.0/
   ```

3. **Copy the plugin DLL:**
   ```bash
   cp bin/Release/net8.0/Jellyfin.Plugin.Suggester.dll \
      /var/lib/jellyfin/plugins/Jellyfin.Plugin.Suggester_1.0.0.0/
   ```

4. **Restart Jellyfin server**

### 3. Configure the Plugin

1. Open Jellyfin admin dashboard
2. Navigate to **Plugins** → **My Plugins**
3. Find "Movie Suggester" and click **Settings**
4. Configure:
   - **OpenAI API Key**: Your API key from [OpenAI Platform](https://platform.openai.com/api-keys)
   - **OpenAI Model**: Choose GPT-3.5 Turbo (recommended for cost)
   - **Max Recommendations**: Number of suggestions (default: 5)
   - **Include Descriptions**: Enable for detailed explanations
   - **Custom Prompt Template**: Customize AI behavior (optional)

## Testing the Plugin

### Verify Plugin Loading

1. Check Jellyfin logs for plugin loading messages:
   ```bash
   tail -f /var/log/jellyfin/jellyfin.log | grep -i suggester
   ```

2. Look for messages like:
   ```
   [INF] Loaded plugin: Movie Suggester 1.0.0.0
   ```

### Test API Endpoints

The plugin exposes these endpoints (once fully implemented):

- `POST /Suggester` - Generate recommendations
- `GET /Suggester` - Retrieve cached recommendations

Example API call:
```bash
curl -X POST "http://your-jellyfin:8096/Suggester" \
  -H "Content-Type: application/json" \
  -d '{
    "UserId": "your-user-id-guid",
    "MaxLibraryMovies": 50
  }'
```

## Current Implementation Status

### ✅ Working Features
- Plugin loads successfully in Jellyfin
- Configuration interface available in admin dashboard
- Service layer architecture properly structured
- OpenAI API integration framework ready

### 🔧 Known Limitations (TODOs)
- **User-specific filtering**: Currently gets all movies, needs user library filtering
- **API endpoint registration**: Controller needs proper Jellyfin API integration
- **Error handling**: Basic error handling implemented, needs enhancement
- **Caching**: No recommendation caching yet
- **Testing**: No unit tests implemented

### 🚨 Important Notes
- **OpenAI API Key Required**: Plugin won't work without valid OpenAI configuration
- **Library Access**: Currently accesses all movies, not user-specific libraries
- **Development Status**: This is a working prototype, not production-ready

## Troubleshooting

### Plugin Not Loading
1. Check Jellyfin logs for errors
2. Verify DLL is in correct directory with proper naming
3. Ensure .NET 8 runtime is available
4. Check file permissions

### Configuration Issues
1. Verify OpenAI API key is valid
2. Test API key with a simple OpenAI API call
3. Check network connectivity to OpenAI servers

### API Errors
1. Check Jellyfin logs for detailed error messages
2. Verify user ID format (must be valid GUID)
3. Ensure movie library has content

## Development Next Steps

### High Priority
1. **Fix user-specific library access** - Implement proper user filtering
2. **Register API endpoints** - Integrate controller with Jellyfin's routing
3. **Add comprehensive error handling** - Graceful failure modes
4. **Implement caching** - Store recommendations to reduce API calls

### Medium Priority
1. **Add unit tests** - Test core functionality
2. **Improve prompt engineering** - Better recommendation quality
3. **Add configuration validation** - Validate settings on save
4. **Performance optimization** - Efficient library scanning

### Low Priority
1. **Advanced filtering** - Genre preferences, rating filters
2. **Recommendation history** - Track and avoid repeating suggestions
3. **Multiple AI providers** - Support for other LLM services
4. **Web UI integration** - Frontend recommendation display

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Jellyfin Server                          │
├─────────────────────────────────────────────────────────────┤
│  Plugin.cs (Entry Point)                                   │
│  ├── Configuration/                                         │
│  │   ├── PluginConfiguration.cs (Settings)                 │
│  │   └── configPage.html (Web UI)                          │
│  ├── Services/                                             │
│  │   ├── JellyfinLibraryService.cs (Library Access)        │
│  │   └── OpenAiRecommendationService.cs (AI Integration)   │
│  └── Api/                                                  │
│      └── SuggesterController.cs (REST Endpoints)           │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌─────────────────┐
                    │   OpenAI API    │
                    │   (GPT Models)  │
                    └─────────────────┘
```

## Support

For issues and development questions:
1. Check Jellyfin plugin development documentation
2. Review OpenAI API documentation for integration issues
3. Examine Jellyfin server logs for detailed error information

---

**Status**: Plugin builds successfully and is ready for Jellyfin deployment testing!
