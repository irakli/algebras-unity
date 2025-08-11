# Algebras Localization for Unity

AI-powered localization extension for Unity's Localization package.

## Features

- **Two Translation Modes**: Batch (bulk processing) and Single (with glossary support)
- **Unity Integration**: Extends Unity's built-in Localization system
- **AI Translation**: Powered by Algebras AI platform
- **UI-Safe Mode**: Preserves Unity markup and formatting
- **Custom Prompts**: Context-specific translation instructions
- **Batch Processing**: Configurable parallel processing for large projects

## Installation

### Via Git URL
1. Open **Window > Package Manager**  
2. Click **+** → **Add package from git URL**
3. Enter: `https://github.com/irakli/algebras-unity.git`

### Via Package Manager Manifest
```json
{
  "dependencies": {
    "ai.algebras.localization": "https://github.com/irakli/algebras-unity.git"
  }
}
```

## Requirements

- Unity 2021.3 or later
- Unity Localization Package 1.4.0+
- Algebras AI API key ([get one here](https://platform.algebras.ai))

## Quick Start

1. **Create Service Provider**
   - **Assets** → **Create** → **Algebras** → **Service Provider**
   - Set your API key from [platform.algebras.ai](https://platform.algebras.ai)

2. **Add to String Tables**
   - Select any StringTableCollection
   - In Inspector, click **Add Extension** → **Algebras Extension**
   - Choose translation mode and configure settings

3. **Configure Settings**
   - **Translation Mode**: Batch (fast) or Single (supports glossary)
   - **UI Safe**: Enable for interface text
   - **Custom Prompt**: Add context for better translations
   - **Glossary ID**: For terminology consistency (Single mode only)

4. **Translate**
   - Use the translate buttons in the StringTable Inspector

## Configuration

### Service Provider
| Setting | Description |
|---------|-------------|
| API Key | Your Algebras AI platform key |
| Application Name | Identifier for API requests |
| Batch Size | Strings per request (1-100) |
| Max Parallel Batches | Concurrent requests (1-10) |
| Request Delay | Delay between requests (seconds) |

### Per-Table Settings  
| Setting | Description |
|---------|-------------|
| Translation Mode | Batch (fast) or Single (glossary support) |
| UI Safe | Preserves Unity markup |
| Custom Prompt | Context-specific instructions |
| Glossary ID | Terminology consistency (Single mode) |

## Translation Modes

**Batch Mode**
- Fast bulk processing
- Parallel requests
- No glossary support

**Single Mode**  
- Individual translations
- Glossary support

## Support

- **Issues**: [GitHub Repository](https://github.com/irakli/algebras-unity/issues)
- **Documentation**: [Algebras AI Docs](https://docs.algebras.ai)
- **Email**: support@algebras.ai

## License

MIT License - see [LICENSE](LICENSE) for details.