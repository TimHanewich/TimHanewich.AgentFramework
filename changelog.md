# Changelog
|Version|Commit|Description|
|-|-|-|
|0.1.0|||Initial release
|0.1.1|`9304003df4963eecb3d2a556ca33ea9a1819ffc3`|Fixed Ollama tool calling|
|0.2.0|`33a2dfbb5a0681ef9d19eb1ddb57acfba44e2821`|You can now control the number of messages that get prompted to the model (message array length cutoff), added JSON mode, and constructors for `AzureOpenAICredentials` and `OllamaModel`|
|0.2.1|`a8d97ce49c54362d541b24e6929d591aa5d964f6`|Set HTTP timeout to 24 hours (increased from default of 100 seconds)|
|0.3.0|`205b66e2bbba2f843ac16eceb78ae1385f13fdbe`|Fixed issue where tool call responses were being left in the message buffer but their tool call request were being trimmed out, causing errors at the Azure OpenAI API level. Also increased the default message buffer size from **9999** from **10** (default is to include all, decrease at your own risk, functionality may be lost).|