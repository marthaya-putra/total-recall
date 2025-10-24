using System.Text;
using Azure;
using Azure.AI.OpenAI;
using OpenAI;
using OpenAI.Chat;
using Sprache;
using TotalRecall.Core;

namespace TotalRecall.Core
{
    public class AzureAIService
    {
        private readonly string _openAIEndpoint;
        private readonly string _openAIKey;
        private readonly string _embeddingEndpoint;
        private readonly string _embeddingKey;
        private readonly string _embeddingDeployment;
        private readonly string _completionDeployment;

        public AzureAIService(ConfigurationService configService)
        {
            var configs = configService.GetAllConfigs();
            _openAIEndpoint = configs["AZURE_OPENAI_ENDPOINT"];
            _openAIKey = configs["AZURE_OPENAI_KEY"];
            _embeddingEndpoint = configs["AZURE_EMBEDDING_ENDPOINT"];
            _embeddingKey = configs["AZURE_EMBEDDING_KEY"];
            _embeddingDeployment = configs["EMBEDDING_DEPLOYMENT"];
            _completionDeployment = configs["COMPLETION_DEPLOYMENT"];
        }

        public async Task<float[]> GenerateDocumentEmbeddingAsync(Document document)
        {
            Console.WriteLine($"Generating embedding for document: {document.Id}");
            var client = new AzureOpenAIClient(new Uri(_embeddingEndpoint), new AzureKeyCredential(_embeddingKey));
            var embeddingClient = client.GetEmbeddingClient(_embeddingDeployment);
            var embeddingResponse = await embeddingClient.GenerateEmbeddingAsync(document.Content);
            Console.WriteLine($"Embedding generated for document: {document.Id}");
            return embeddingResponse.Value.ToFloats().ToArray();
        }

        public async Task<float[]> GenerateQueryEmbeddingAsync(string query)
        {
            Console.WriteLine($"Generating embedding for query: {query}");
            var client = new AzureOpenAIClient(new Uri(_embeddingEndpoint), new AzureKeyCredential(_embeddingKey));
            var embeddingClient = client.GetEmbeddingClient(_embeddingDeployment);
            var embeddingResponse = await embeddingClient.GenerateEmbeddingAsync(query);
            Console.WriteLine($"Query embedding generated for: {query}");
            return embeddingResponse.Value.ToFloats().ToArray();
        }

        public async IAsyncEnumerable<StreamingChatCompletionUpdate> GenerateCompletionStreamingAsync(List<(string path, string content)> contexts, string question)
        {
            var systemPrompt = BuildDynamicSystemPrompt(contexts, question);

            Console.WriteLine($"Generating streaming completion using deployment: {_completionDeployment}");
            var client = new AzureOpenAIClient(new Uri(_openAIEndpoint), new AzureKeyCredential(_openAIKey));
            var chatClient = client.GetChatClient(_completionDeployment);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage($"Question: {question}")
            };

            await foreach (var update in chatClient.CompleteChatStreamingAsync(messages))
            {
                yield return update;
            }
        }

        private static string BuildDynamicSystemPrompt(List<(string path, string content)> contexts, string question)
        {
            var promptBuilder = new StringBuilder();
            var currentDateTime = DateTime.Now;

            promptBuilder.AppendLine($"Current Date and Time: {currentDateTime:yyyy-MM-dd HH:mm:ss} (UTC)");
            promptBuilder.AppendLine($"Treat {currentDateTime:MMMM yyyy} as the current date for all time references.");
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("You are the user's personal code library assistant.");
            promptBuilder.AppendLine("Your role: find, explain, and modernize the user's previous code implementations. If none are found, provide a new, well-structured implementation.");
            promptBuilder.AppendLine();

            if (contexts.Any())
            {
                promptBuilder.AppendLine("User’s Previous Implementations Found:");
                for (int i = 0; i < contexts.Count; i++)
                {
                    var (path, content) = contexts[i];
                    if (content.Length > 2000) content = content[..2000] + "...";
                    promptBuilder.AppendLine($"Implementation {i + 1} (from {path}):");
                    promptBuilder.AppendLine(content);
                    promptBuilder.AppendLine();
                }

                promptBuilder.AppendLine("Instructions:");
                promptBuilder.AppendLine("- Begin with: \"I found your previous implementation that solves this.\"");
                promptBuilder.AppendLine("- Show how it works, referencing the actual file path.");
                promptBuilder.AppendLine("- Explain how to reuse or adapt it for the current problem.");
                promptBuilder.AppendLine("- If outdated, suggest specific modern alternatives and explain why they’re better.");
                promptBuilder.AppendLine("- If still valid, confirm that explicitly (e.g., 'Your implementation remains valid for October 2025.').");
            }
            else
            {
                promptBuilder.AppendLine("No previous implementations found in the codebase.");
                promptBuilder.AppendLine("Provide a complete new implementation that meets the user’s request, using current best practices and libraries.");
            }
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("Response Rules:");
            promptBuilder.AppendLine("- Reference only real file paths from the context; never invent or assume any.");
            promptBuilder.AppendLine("- Format code clearly with syntax highlighting and short explanations.");
            promptBuilder.AppendLine("- Do not offer further help, follow-up, or invitations to continue the conversation.");
            promptBuilder.AppendLine("- End the response immediately after providing the full answer.");

            return promptBuilder.ToString();
        }
    }
}