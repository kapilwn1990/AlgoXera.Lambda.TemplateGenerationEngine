using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Newtonsoft.Json;
using AlgoXera.Lambda.TemplateGenerationEngine.Services;
using AlgoXera.Lambda.TemplateGenerationEngine.Models;
using AlgoXera.Lambda.Shared.Services;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AlgoXera.Lambda.TemplateGenerationEngine;

public class Function
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly GeminiService _geminiService;
    private readonly IndicatorRepository _indicatorRepository;
    private readonly NotificationService _notificationService;

    public Function()
    {
        _dynamoDbClient = new AmazonDynamoDBClient();
        
        var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? throw new Exception("GEMINI_API_KEY not set");
        var geminiModel = Environment.GetEnvironmentVariable("GEMINI_MODEL") ?? "gemini-1.5-flash";
        
        _indicatorRepository = new IndicatorRepository(_dynamoDbClient);
        _geminiService = new GeminiService(geminiApiKey, geminiModel, _indicatorRepository, null!);
        _notificationService = new NotificationService(_dynamoDbClient);
    }

    /// <summary>
    /// SQS event handler for template generation requests
    /// </summary>
    public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        context.Logger.LogInformation($"Processing {sqsEvent.Records.Count} SQS messages");

        foreach (var record in sqsEvent.Records)
        {
            try
            {
                context.Logger.LogInformation($"Processing message: {record.MessageId}");
                
                var request = JsonConvert.DeserializeObject<TemplateGenerationRequest>(record.Body);
                if (request == null)
                {
                    context.Logger.LogError("Failed to deserialize message body");
                    continue;
                }

                await ProcessTemplateGenerationAsync(request, context);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error processing message {record.MessageId}: {ex.Message}");
                // Let the message go back to queue for retry
                throw;
            }
        }
    }

    private async Task ProcessTemplateGenerationAsync(TemplateGenerationRequest request, ILambdaContext context)
    {
        context.Logger.LogInformation($"Generating {request.TemplateType} template from conversation {request.ConversationId}");

        try
        {
            // 1. Build conversation summary from provided messages
            var conversationSummary = BuildConversationSummary(request.Messages);
            context.Logger.LogInformation($"Built conversation summary: {conversationSummary.Length} characters");

            string templateJson;
            
            // 2. Generate template based on TemplateType
            if (request.TemplateType == "Signal")
            {
                // Signal template: simultaneous conditions, no stepwise logic
                context.Logger.LogInformation($"Generating Signal template (Direction: {request.Direction}, Timeframe: {request.Timeframe})");
                templateJson = await _geminiService.GenerateSignalTemplateAsync(
                    conversationSummary,
                    request.Name,
                    request.Description,
                    request.Category,
                    request.Direction ?? "Bullish",
                    request.Timeframe ?? "1d"
                );
            }
            else
            {
                // Execution template: stepwise T1→T2→T3 logic (existing behavior)
                context.Logger.LogInformation("Generating Execution template with stepwise logic");
                templateJson = await _geminiService.GenerateStepwiseTemplateAsync(
                    conversationSummary,
                    request.Name,
                    request.Description,
                    request.Category
                );
            }

            context.Logger.LogInformation("Template generation complete");

            // 3. Validate JSON
            try
            {
                JsonConvert.DeserializeObject<object>(templateJson);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Template validation error: {ex.Message}");
                throw new Exception($"Generated template is invalid: {ex.Message}");
            }

            // 4. Create template with generated rules
            var templateId = await CreateTemplateAsync(request, templateJson);
            context.Logger.LogInformation($"Template {templateId} created with generated rules");

            // 5. Update conversation status and link template
            await UpdateConversationAsync(request.ConversationId, templateId, "completed");
            context.Logger.LogInformation($"Conversation {request.ConversationId} updated with template ID");

            // 6. Send notification to user
            await _notificationService.SendTemplateCompleteNotificationAsync(
                request.UserId,
                templateId,
                request.Name,
                success: true
            );
            context.Logger.LogInformation($"Notification sent to user {request.UserId}");
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Template generation failed: {ex.Message}");
            
            // Send failure notification
            try
            {
                await _notificationService.SendTemplateCompleteNotificationAsync(
                    request.UserId,
                    string.Empty,
                    request.Name,
                    success: false
                );
            }
            catch { /* Ignore notification errors */ }
            
            throw;
        }
    }

    private string BuildConversationSummary(List<ChatMessage> messages)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== CONVERSATION HISTORY ===");
        sb.AppendLine();

        foreach (var message in messages)
        {
            sb.AppendLine($"{message.Role.ToUpper()}: {message.Content}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private async Task<string> CreateTemplateAsync(TemplateGenerationRequest request, string rulesJson)
    {
        var templateId = Guid.NewGuid().ToString();
        var timestamp = DateTime.UtcNow.ToString("O");

        var item = new Dictionary<string, AttributeValue>
        {
            { "TemplateId", new AttributeValue { S = templateId } },
            { "UserId", new AttributeValue { S = request.UserId } },
            { "Name", new AttributeValue { S = request.Name } },
            { "Description", new AttributeValue { S = request.Description } },
            { "Category", new AttributeValue { S = request.Category } },
            { "ConversationId", new AttributeValue { S = request.ConversationId } },
            { "Status", new AttributeValue { S = "ACTIVE" } },
            { "IsStepwise", new AttributeValue { BOOL = true } },
            { "CreatedAt", new AttributeValue { S = timestamp } },
            { "UpdatedAt", new AttributeValue { S = timestamp } },
            { "TemplateType", new AttributeValue { S = request.TemplateType } }
        };
        
        // Store rules in appropriate field based on template type
        if (request.TemplateType == "Signal")
        {
            item["RulesJsonSignal"] = new AttributeValue { S = rulesJson };
            if (!string.IsNullOrEmpty(request.Direction))
                item["Direction"] = new AttributeValue { S = request.Direction };
            if (!string.IsNullOrEmpty(request.Timeframe))
                item["Timeframe"] = new AttributeValue { S = request.Timeframe };
        }
        else
        {
            item["RulesJson"] = new AttributeValue { S = rulesJson };
            if (!string.IsNullOrEmpty(request.Timeframe))
                item["Timeframe"] = new AttributeValue { S = request.Timeframe };
        }

        var putRequest = new PutItemRequest
        {
            TableName = "Templates",
            Item = item
        };

        await _dynamoDbClient.PutItemAsync(putRequest);
        return templateId;
    }

    private async Task UpdateConversationAsync(string conversationId, string templateId, string status)
    {
        var request = new UpdateItemRequest
        {
            TableName = "Conversations",
            Key = new Dictionary<string, AttributeValue>
            {
                { "ConversationId", new AttributeValue { S = conversationId } }
            },
            UpdateExpression = "SET #status = :status, TemplateId = :templateId, UpdatedAt = :updatedAt",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                { "#status", "Status" }
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":status", new AttributeValue { S = status } },
                { ":templateId", new AttributeValue { S = templateId } },
                { ":updatedAt", new AttributeValue { S = DateTime.UtcNow.ToString("o") } }
            }
        };

        await _dynamoDbClient.UpdateItemAsync(request);
    }
}


