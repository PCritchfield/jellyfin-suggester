# Jellyfin Movie Suggester Plugin — PRD (v1.2)

## 1. Objective
Provide any authenticated user on your Jellyfin server a conversational “what to watch” experience:
- **Separate plugin page** where users type a free-form prompt (e.g. “Buddy Cop action”)
- Returns AI-driven movie suggestions drawn **only** from their own library

## 2. Technical Architecture

### 2.1. .NET Target & Packaging
- **TargetFramework:** `net8.0`
- **Build Source:** keep `build.yaml` in repo; compile to include `plugin.json`

### 2.2. DI & Startup (in `Plugin.cs`)
Override `OnApplicationStarted` on your `BasePlugin<PluginConfiguration>`:

```csharp
public override void OnApplicationStarted(IApplicationBuilder app, IServiceCollection services)
{
    // 1) Register controllers & DI
    services.AddControllers()
            .AddApplicationPart(typeof(SuggesterController).Assembly)
            .AddControllersAsServices();

    services.AddSingleton<JellyfinLibraryService>();
    services.AddHttpClient<OpenAiRecommendationService>();

    // 2) Wire up routing
    app.UseRouting();
    app.UseEndpoints(e => e.MapControllers());
}
```

- **No separate Startup class**—this merges into Jellyfin’s host and DI container.

## 3. API Endpoints

| Method | Route                 | Body / Query                              | Auth           | Description                                                   |
|--------|-----------------------|-------------------------------------------|----------------|---------------------------------------------------------------|
| POST   | `/Suggester/Generate` | `{ UserId, Prompt, MaxLibraryMovies?, ForceRegenerate? }` | `[Authorize]` | Build prompt + fetch random subset + call OpenAI → recommendations |
| GET    | `/Suggester/Cached`   | `{ UserId, IncludeExpired? }`             | `[Authorize]` | Return last cached recommendations (in-memory Phase 1)        |

## 4. Functional Requirements

1. **Subset Strategy**
   - **Default:** Select a **random** subset of up to `MaxLibraryMovies` (e.g. 20) for context.
   - **Override:** If user’s prompt mentions “highest rated” or “recent,” allow that to drive selection logic (future enhancement).

2. **Dynamic Prompting**
   - **Configurable base template** (override in UI):
     > “You are a movie sommelier… catalog: {movies}. When a user says “{prompt},” recommend {count} films.”
   - Runtime: replace `{movies}`, `{prompt}`, `{count}`.

3. **Result Format**
   - Return a list of **mini-cards** (JSON) with:
     - Title
     - Release Year
     - Optional one-line description (if `IncludeDescriptions`)
     - Source = “OpenAI”

## 5. UI Design

### 5.1. Plugin Page Location
- **Navigation:** `Plugins → Movie Suggester` in Jellyfin sidebar.

### 5.2. Page Layout

```html
<div class="pluginConfigurationPage">
  <h2>Movie Suggester</h2>
  <div id="suggester-app">
    <!-- INPUT AREA -->
    <input type="text" id="txtUserPrompt"
           placeholder="I’m in the mood for buddy-cop action…" />
    <button id="btnSuggest">Suggest</button>

    <!-- RESULTS AREA -->
    <div id="suggestions-container"></div>
  </div>
</div>
```

- **Behavior:**
  1. User enters their natural-language prompt.
  2. Clicks **Suggest**.
  3. Front-end JS POSTs to `/Suggester/Generate`.
  4. Renders each recommendation as a mini-card:
```html
<div class="mini-card">
  <h3>Movie Title (Year)</h3>
  <p class="desc">“Because it combines…”</p> <!-- if enabled -->
</div>
```
- **Auth:** Page and API only available to **authenticated** users (`[Authorize]`).

## 6. Data Flow

1. **Request**: Browser → `/Suggester/Generate`
2. **Controller**: 
   - Extract `UserId` from `User` claims
   - Read user’s prompt and `MaxLibraryMovies`
3. **LibraryService**: 
   - Fetch random subset of movies
   - Map to `MovieInfo` DTO
4. **RecommendationService**:
   - Build prompt with `{movies}`, `{prompt}`, `{count}`
   - Call OpenAI REST API
   - Parse numbered list into `MovieRecommendation` list
5. **Response**: JSON array of mini-card data → Browser
6. **Render**: JS creates mini-card elements in `#suggestions-container`

## 7. Metadata Extraction Stage

**Purpose:** Turn the user’s free-text query into structured filters (genres, people, year range, keywords).

**JSON Schema:**
```json
{
  "genres": ["Action", "Comedy", …],
  "persons": ["Bruce Willis", …],
  "yearRange": { "start": 1980, "end": 1989 } | null,
  "keywords": ["hitman", "dark humor", …],
  "rawQuery": "Surprise me with…"
}
```

**Prompt Template (Stage 1):**
```
You are an expert at parsing free-text movie requests into structured JSON metadata.
Supported genres: {comma-list of all genres}.
Return _only_ a JSON object matching the schema above.
Now parse: "{rawQuery}"
```

**Service Responsibilities:**
- Call OpenAI ChatCompletion with `temperature = 0`.
- Deserialize into `MetadataFilters`.
- On failure, log and return `{ RawQuery = query }`.

**DI Registration:**
- `services.AddHttpClient<MetadataExtractionService>()`
- `services.AddSingleton<MetadataExtractionService>()`

**Acceptance Criteria:**
- Given sample queries, the service produces valid `MetadataFilters`.
- Invalid/malformed JSON is handled gracefully.

## 8. Non-Functional & Constraints

- **Performance:** Prompt subset limited to avoid token bloat (max ~20 items).
- **Phase 1 Caching:** In-memory per user; Phase 2 migrate to distributed cache.
- **Error Handling:** Return clear JSON errors (400/500) for missing API key, library empty, or HTTP failures.

## 9. Milestones & Acceptance Criteria

1. **Scaffold**
   - Plugin loads in Jellyfin 10.10.7
   - `/Suggester/Generate` returns dummy data

2. **Library Integration**
   - Returns random subset of movies

3. **OpenAI Call**
   - Returns real recommendations

4. **UI Page**
   - Prompts user, displays mini-cards

5. **Configuration**
   - Settings page overrides prompt template & recommendation count