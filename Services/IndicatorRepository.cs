using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using AlgoXera.Lambda.TemplateGenerationEngine.Models;

namespace AlgoXera.Lambda.TemplateGenerationEngine.Services;

public class IndicatorRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private const string TableName = "IndicatorDefinitions";

    public IndicatorRepository(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb;
    }

    public async Task<List<IndicatorDefinition>> GetByTypesAsync(List<string> types)
    {
        var definitions = new List<IndicatorDefinition>();

        foreach (var type in types)
        {
            var request = new GetItemRequest
            {
                TableName = TableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "IndicatorType", new AttributeValue { S = type.ToLowerInvariant() } }
                }
            };

            var response = await _dynamoDb.GetItemAsync(request);
            
            if (response.Item.Count > 0)
            {
                definitions.Add(MapToIndicatorDefinition(response.Item));
            }
        }

        return definitions;
    }

    private IndicatorDefinition MapToIndicatorDefinition(Dictionary<string, AttributeValue> item)
    {
        return new IndicatorDefinition
        {
            IndicatorType = item["IndicatorType"].S,
            DisplayName = item.ContainsKey("DisplayName") ? item["DisplayName"].S : "",
            Category = item.ContainsKey("Category") ? item["Category"].S : "",
            Description = item.ContainsKey("Description") ? item["Description"].S : "",
            IdFormat = item.ContainsKey("IdFormat") ? item["IdFormat"].S : "",
            ExampleId = item.ContainsKey("ExampleId") ? item["ExampleId"].S : "",
            ParametersJson = item.ContainsKey("ParametersJson") ? item["ParametersJson"].S : "",
            PromptSnippet = item.ContainsKey("PromptSnippet") ? item["PromptSnippet"].S : "",
            SortOrder = item.ContainsKey("SortOrder") && int.TryParse(item["SortOrder"].N, out var order) ? order : 0,
            IsActive = !item.ContainsKey("IsActive") || item["IsActive"].BOOL
        };
    }
}

