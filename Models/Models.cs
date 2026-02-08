namespace AlgoXera.Lambda.TemplateGenerationEngine.Models;

public class TemplateGenerationRequest
{
    public string ConversationId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<ChatMessage> Messages { get; set; } = new();
    
    // Multi-Timeframe Support
    public string TemplateType { get; set; } = "Execution"; // "Execution" or "Signal"
    public string? Direction { get; set; } // "Bullish" or "Bearish" (for Signal templates)
    public string? Timeframe { get; set; } // Template timeframe
}

public class ChatMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class IndicatorDefinition
{
    public string IndicatorType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IdFormat { get; set; } = string.Empty;
    public string ExampleId { get; set; } = string.Empty;
    public string ParametersJson { get; set; } = string.Empty;
    public string PromptSnippet { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

