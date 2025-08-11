# Changelog

All notable changes to this project will be documented in this file.

## [1.0.0] - 2025-01-11

### Added

- **Dual Translation Modes**: Batch mode for bulk processing and Single mode with glossary support
- **Algebras AI Integration**: Direct integration with Algebras AI translation platform  
- **Unity Localization Extension**: Seamless integration with Unity's built-in Localization package
- **Service Provider**: Unity ScriptableObject-based configuration system
- **Batch Processing**: Configurable parallel processing with semaphore-based concurrency control
- **UI-Safe Translation**: Preserves Unity markup and formatting in translations
- **Custom Prompts**: Context-specific translation instructions per StringTable
- **String Normalization**: Automatic cleanup of translation artifacts
- **Editor Integration**: Custom property drawers and Unity menu integration
- **Progress Reporting**: Visual feedback during translation operations
- **Error Handling**: Comprehensive error handling and Unity Console logging

### Technical Details

- **API Endpoints**: Both batch (`/translate-batch`) and single (`/translate`) endpoints
- **Authentication**: API key-based authentication via `X-Api-Key` header
