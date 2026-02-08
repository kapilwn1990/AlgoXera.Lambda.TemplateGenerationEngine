# AlgoXera.Lambda.TemplateGenerationEngine

AWS Lambda function for processing template generation requests from SQS queue. This function is triggered by SQS events and generates trading strategy templates using Google Gemini AI.

## Features

- **SQS-Triggered Processing**: Automatically processes template generation jobs from SQS queue
- **AI Template Generation**: Uses Google Gemini for intelligent template creation
- **DynamoDB Integration**: Stores templates and indicators in DynamoDB
- **Batch Processing**: Handles multiple template generation requests efficiently
- **Error Handling**: Failed jobs are sent to Dead Letter Queue for retry

## Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `GEMINI_API_KEY` | Google Gemini API key | `AIzaSy...` |
| `GEMINI_MODEL` | Gemini model name | `gemini-3-flash-preview` |

## Dependencies

- **AlgoXera.Lambda.Shared**: Shared libraries for common functionality
- **AWS SDK**: DynamoDB, SQS
- **Amazon.Lambda.SQSEvents**: SQS event handling
- **Newtonsoft.Json**: JSON serialization

## Services

- **GeminiService**: Google Gemini integration for AI template generation
- **IndicatorRepository**: Manages technical indicator definitions

## SQS Integration

**Queue Name**: `template-generation-queue`  
**Batch Size**: 1 message per invocation  
**Visibility Timeout**: 30 seconds  
**Dead Letter Queue**: Configured for failed processing

## Deployment

This Lambda function is automatically deployed via GitHub Actions when code is pushed to the `main` branch.

**Function Name**: `templategenerationengine-dev`  
**Region**: `us-east-1`  
**Runtime**: .NET 8  
**Memory**: 512 MB  
**Timeout**: 30 seconds  
**Trigger**: SQS Queue

## Local Development

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Publish
dotnet publish --configuration Release --runtime linux-x64 --no-self-contained
```

## Testing SQS Trigger

Send a message to the SQS queue:

```bash
aws sqs send-message \
  --queue-url https://sqs.us-east-1.amazonaws.com/428021717924/template-generation-queue \
  --message-body '{"userId":"test-user","templateType":"breakout","indicators":["RSI","MACD"]}' \
  --region us-east-1
```

## Related Resources

- SQS Queue: `template-generation-queue`
- DynamoDB Tables: `TEMPLATES-DEV`, `INDICATOR-DEFINITIONS-DEV`, `MESSAGES-DEV`, `CONVERSATIONS-DEV`, `NOTIFICATIONS-DEV`
- IAM Role: `algoxera-lambda-Dev-role`
