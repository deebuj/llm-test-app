# Weather Tool Integration

Your Ollama Chat API now supports automatic tool calling! When users ask weather-related questions, the system will:

1. **Detect** the weather query and extract the location
2. **Call** the weather API to get real-time data
3. **Feed** the weather data to the LLM as context
4. **Return** a natural language response with current weather information
5. **Store** the interaction in the conversation session for future reference

## How It Works

### Example Flow

**User**: "What is the weather in New York?"

**System Process**:
1. ✅ **Tool Detection**: Detects "weather" keyword and extracts "New York"
2. ✅ **API Call**: Calls OpenWeatherMap API for New York weather
3. ✅ **Context Building**: Formats weather data for the LLM:
   ```
   Current weather information for New York, US:
   - Temperature: 22.5°C (feels like 24.1°C)
   - Conditions: partly cloudy
   - Humidity: 65%
   - Wind Speed: 3.2 m/s
   ```
4. ✅ **LLM Processing**: Sends combined context + user question to Ollama
5. ✅ **Response**: Returns natural language response with current data
6. ✅ **Session Storage**: Stores both user question and weather context in session

**Follow-up User**: "Should I wear a jacket?"

**System**: Uses the previously fetched weather data from the session to provide contextual advice!

## Features

### 🔍 Smart Detection
Automatically detects various weather query patterns:
- "What is the weather in [location]?"
- "How's the temperature in [location]?"
- "What's the weather like in [location]?"
- "Weather forecast for [location]"
- And many more variations...

### 🌐 Real-time Data
- Uses OpenWeatherMap API for current weather conditions
- Provides temperature, conditions, humidity, and wind speed
- Automatic unit conversion (metric system)
- Location validation and normalization

### 💬 Contextual Conversations
- Weather data is stored in conversation sessions
- Follow-up questions can reference previous weather information
- Natural language responses combining weather data with AI reasoning

### 🛠️ Extensible Architecture
- Easy to add new tools (news, stocks, calculations, etc.)
- Modular tool detection and execution
- Proper error handling and fallbacks

## Setup

### 1. Get Weather API Key
1. Sign up at [OpenWeatherMap](https://openweathermap.org/api)
2. Get your free API key
3. Add it to `appsettings.json`:

```json
{
  "Weather": {
    "ApiKey": "your-openweathermap-api-key-here"
  }
}
```

### 2. Test the Integration
```http
POST /api/chat
Content-Type: application/json

{
  "message": "What is the weather in New York?",
  "sessionId": "your-session-id"
}
```

## API Response Structure

The response now includes tool call information:

```json
{
  "response": "The current weather in New York is partly cloudy with a temperature of 22.5°C...",
  "model": "llama3.2:latest",
  "success": true,
  "createdAt": "2025-08-23T10:30:00Z",
  "sessionId": "123e4567-e89b-12d3-a456-426614174000",
  "messageCount": 4,
  "toolCalls": [
    {
      "toolName": "weather",
      "query": "New York",
      "result": {
        "location": "New York, US",
        "temperature": 22.5,
        "description": "partly cloudy",
        "feelsLike": 24.1,
        "humidity": 65,
        "windSpeed": 3.2,
        "timestamp": "2025-08-23T10:30:00Z"
      },
      "success": true,
      "error": null,
      "executedAt": "2025-08-23T10:30:00Z"
    }
  ]
}
```

## Supported Weather Queries

### Direct Questions
- "What is the weather in Paris?"
- "How's the weather in Tokyo?"
- "Weather in London"

### Temperature Specific
- "What's the temperature in Berlin?"
- "How hot is it in Miami?"
- "Temperature forecast for Sydney"

### Conversational Styles
- "Tell me about the weather in Rome"
- "I want to know the weather conditions in Dubai"
- "Check the weather for San Francisco"

### Follow-up Questions
After getting weather info, you can ask:
- "Should I wear a coat?"
- "Is it good weather for a walk?"
- "Will I need an umbrella?"
- "Compare that to [another city]"

## Technical Implementation

### Tool Detection Service
- Pattern matching for weather-related keywords
- Location extraction using regex patterns
- Extensible for adding new tool types

### Weather Service
- OpenWeatherMap API integration
- Error handling and retry logic
- Data normalization and formatting

### Context Integration
- Seamless integration with conversation flow
- Tool results stored in session history
- LLM receives both user question and real-time data

## Adding More Tools

The architecture supports easy addition of new tools:

1. **Create Tool Service** (e.g., `NewsService`, `StockService`)
2. **Add Detection Logic** in `ToolDetectionService`
3. **Register Service** in `Program.cs`
4. **Update Models** if needed

### Example: Adding a Stock Tool
```csharp
public async Task<List<ToolCall>> DetectAndExecuteToolsAsync(string message)
{
    var toolCalls = new List<ToolCall>();
    
    // Existing weather detection...
    
    // New stock detection
    if (IsStockQuery(message))
    {
        var symbol = ExtractStockSymbol(message);
        var stockData = await _stockService.GetStockDataAsync(symbol);
        // ... process and add to toolCalls
    }
    
    return toolCalls;
}
```

## Error Handling

- **API Key Missing**: Returns helpful error message
- **Invalid Location**: LLM explains location not found
- **API Timeout**: Graceful fallback with explanation
- **Tool Failure**: Conversation continues without tool data

## Benefits

1. **Enhanced User Experience**: Real-time data in natural conversations
2. **Context Awareness**: Follow-up questions work seamlessly
3. **Accurate Information**: Always current weather data
4. **Conversation Memory**: Tool results stored in session history
5. **Extensible**: Easy to add more external data sources

## Next Steps

Consider adding these tools:
- 📰 **News API**: Latest news and headlines
- 📈 **Stock API**: Real-time stock prices
- 🗺️ **Maps API**: Directions and location info
- 🔍 **Search API**: Web search results
- 📅 **Calendar API**: Schedule and events
- 🌐 **Translation API**: Multi-language support

The tool system is designed to be modular and easy to extend!
