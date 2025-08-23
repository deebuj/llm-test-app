# Chat History and Conversation Context

Your Ollama Chat API now supports conversation context and chat history! This means the AI can remember previous messages in a conversation and provide contextual responses.

## How It Works

The API now supports conversation sessions that maintain chat history. When you ask a follow-up question, the AI can reference previous messages in the same session.

### Example Scenario

1. **First message**: "What is 5 + 5?"
2. **AI Response**: "5 + 5 equals 10."
3. **Follow-up message**: "Add 2 to it"
4. **AI Response**: "10 + 2 equals 12." ✅ (The AI remembers the previous calculation)

## API Endpoints

### 1. Create a New Conversation Session
```http
POST /api/chat/session
```
Returns a session ID that you can use for subsequent messages.

**Response:**
```json
{
  "sessionId": "123e4567-e89b-12d3-a456-426614174000"
}
```

### 2. Send a Message with Context
```http
POST /api/chat
Content-Type: application/json

{
  "message": "What is 5 + 5?",
  "sessionId": "123e4567-e89b-12d3-a456-426614174000"
}
```

**Response:**
```json
{
  "response": "5 + 5 equals 10.",
  "model": "llama3.2:latest",
  "success": true,
  "createdAt": "2025-01-21T10:30:00Z",
  "sessionId": "123e4567-e89b-12d3-a456-426614174000",
  "messageCount": 2
}
```

### 3. Continue the Conversation
```http
POST /api/chat
Content-Type: application/json

{
  "message": "Add 2 to it",
  "sessionId": "123e4567-e89b-12d3-a456-426614174000"
}
```

The AI will now understand "it" refers to the result from the previous calculation (10).

### 4. Get Conversation History
```http
GET /api/chat/session/{sessionId}/history
```

**Response:**
```json
{
  "sessionId": "123e4567-e89b-12d3-a456-426614174000",
  "model": "llama3.2:latest",
  "createdAt": "2025-01-21T10:30:00Z",
  "lastUpdatedAt": "2025-01-21T10:35:00Z",
  "messageCount": 4,
  "messages": [
    {
      "role": "user",
      "content": "What is 5 + 5?",
      "createdAt": "2025-01-21T10:30:00Z"
    },
    {
      "role": "assistant",
      "content": "5 + 5 equals 10.",
      "createdAt": "2025-01-21T10:30:30Z"
    },
    {
      "role": "user",
      "content": "Add 2 to it",
      "createdAt": "2025-01-21T10:35:00Z"
    },
    {
      "role": "assistant",
      "content": "10 + 2 equals 12.",
      "createdAt": "2025-01-21T10:35:15Z"
    }
  ]
}
```

### 5. Clear a Conversation Session
```http
DELETE /api/chat/session/{sessionId}
```

## Usage Modes

### 1. With Session (Conversation Context)
- Create a session first using `POST /api/chat/session`
- Use the returned `sessionId` in subsequent requests
- The AI will remember all previous messages in that session

### 2. Without Session (Single Messages)
- Send messages without a `sessionId`
- Each message is independent with no context from previous messages
- Useful for one-off questions

## Configuration

You can configure conversation settings in `appsettings.json`:

```json
{
  "Conversation": {
    "CleanupIntervalMinutes": 30,
    "MaxSessionAgeHours": 24
  }
}
```

- `CleanupIntervalMinutes`: How often to clean up old sessions
- `MaxSessionAgeHours`: How long to keep inactive sessions

## Technical Details

- **Sessions are stored in memory**: They will be lost when the app restarts
- **Automatic cleanup**: Old sessions are automatically removed based on configuration
- **Thread-safe**: Multiple users can have concurrent conversations
- **Ollama Chat API**: Uses Ollama's `/api/chat` endpoint instead of `/api/generate` for better conversation support

## Benefits

1. **Contextual conversations**: AI can reference previous messages
2. **Better user experience**: More natural chat interactions
3. **Follow-up questions**: No need to repeat context
4. **Session management**: Clean separation between different conversations
5. **Backward compatibility**: Existing single-message API still works

Try it out with the provided `ConversationExamples.http` file!
