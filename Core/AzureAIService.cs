using System.Text;
using Azure;
using Azure.AI.OpenAI;
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
            var embeddingClient = new OpenAIClient(new Uri(_embeddingEndpoint), new AzureKeyCredential(_embeddingKey));
            var embeddingOptions = new EmbeddingsOptions(_embeddingDeployment, [document.Content]);
            var embeddingResponse = await embeddingClient.GetEmbeddingsAsync(embeddingOptions);
            Console.WriteLine($"Embedding generated for document: {document.Id}");
            return embeddingResponse.Value.Data[0].Embedding.ToArray();
        }

        public async Task<float[]> GenerateQueryEmbeddingAsync(string query)
        {
            Console.WriteLine($"Generating embedding for query: {query}");
            var embeddingClient = new OpenAIClient(new Uri(_embeddingEndpoint), new AzureKeyCredential(_embeddingKey));
            var embeddingOptions = new EmbeddingsOptions(_embeddingDeployment, [query]);
            var embeddingResponse = await embeddingClient.GetEmbeddingsAsync(embeddingOptions);
            Console.WriteLine($"Query embedding generated for: {query}");
            return embeddingResponse.Value.Data[0].Embedding.ToArray();
        }

        public async Task<string> GenerateCompletionAsync(List<(string path, string content)> contexts, string question)
        {
            var systemPrompt = BuildDynamicSystemPrompt(contexts, question);
            var messages = new List<ChatRequestMessage>
            {
                new ChatRequestSystemMessage(systemPrompt),
                new ChatRequestUserMessage($"Question: {question}")
            };

            var completionOptions = new ChatCompletionsOptions(_completionDeployment, messages);
            Console.WriteLine($"Generating completion using deployment: {_completionDeployment}");
            var openAIClient = new OpenAIClient(new Uri(_openAIEndpoint), new AzureKeyCredential(_openAIKey));
            var completionResponse = await openAIClient.GetChatCompletionsAsync(completionOptions);
            Console.WriteLine("Completion generated successfully");
            return completionResponse.Value.Choices[0].Message.Content;
        }

        private static string BuildDynamicSystemPrompt(List<(string path, string content)> contexts, string question)
        {
            var promptBuilder = new StringBuilder();

            // Base role definition - focused on finding user's previous implementations
            promptBuilder.AppendLine("You are the user's personal code library assistant. Your primary role is to help the user find and reuse their own previous implementations and solutions from their codebase.");
            promptBuilder.AppendLine("The user knows they have solved similar problems before and wants to locate their existing solutions rather than reinventing the wheel.");
            promptBuilder.AppendLine();

            // Add context-specific instructions
            if (contexts.Any())
            {
                promptBuilder.AppendLine("Code Search Results:");
                promptBuilder.AppendLine("I found these potentially relevant implementations from your codebase. These are YOUR previous code solutions:");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("Instructions:");
                promptBuilder.AppendLine("- PRIORITIZE showing the user's exact previous implementations that solve their current problem");
                promptBuilder.AppendLine("- Say \"I found your previous implementation that solves this\" when relevant code exists");
                promptBuilder.AppendLine("- Show the user's own code examples first and explain how they can be reused or adapted");
                promptBuilder.AppendLine("- Reference which file/context contains the solution");
                promptBuilder.AppendLine("- IMPORTANT: When presenting code, include the file path in clickable format like `path/to/file.ext` so the user can navigate directly to the file");
                promptBuilder.AppendLine("- Suggest how to adapt or modify their previous solution for the current needs");
                promptBuilder.AppendLine();

                // Add context information with more emphasis on ownership
                promptBuilder.AppendLine("Your Previous Code Implementations Found:");
                for (int i = 0; i < contexts.Count; i++)
                {
                    var (path, content) = contexts[i];
                    // Truncate very long contexts to avoid token limits
                    if (content.Length > 2000)
                    {
                        content = content.Substring(0, 2000) + "...";
                    }
                    promptBuilder.AppendLine($"Your Implementation {i + 1} (from {path}):");
                    promptBuilder.AppendLine(content);
                    promptBuilder.AppendLine();
                }
            }
            else
            {
                promptBuilder.AppendLine("No existing implementations found in your codebase for this problem.");
                promptBuilder.AppendLine("Since I couldn't find your previous solutions, you may need to implement this from scratch or the code might not be indexed yet.");
                promptBuilder.AppendLine();
            }

            // Add question-type specific instructions
            promptBuilder.AppendLine("Response Guidelines:");
            promptBuilder.AppendLine("- Always remind the user that these are THEIR previous implementations");
            promptBuilder.AppendLine("- Focus on reusing and adapting their existing code rather than writing new solutions");
            promptBuilder.AppendLine("- If no exact match is found, suggest the closest similar implementations");
            promptBuilder.AppendLine("- Format their code examples properly with syntax highlighting");
            promptBuilder.AppendLine("- CRITICAL: Always include clickable file paths before code blocks using backticks: `path/to/file.ext`");
            promptBuilder.AppendLine("- If no relevant code exists, acknowledge that and suggest they might need to implement something new");
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("Modernization and Updates:");
            promptBuilder.AppendLine("- Analyze the found implementations for potential outdated patterns or libraries");
            promptBuilder.AppendLine("- Identify deprecated APIs, old versions, or legacy approaches in their code");
            promptBuilder.AppendLine("- Suggest specific modern alternatives with current best practices");
            promptBuilder.AppendLine("- Provide updated code examples that maintain the same functionality");
            promptBuilder.AppendLine("- Explain WHY the modern approach is better (performance, security, maintainability)");
            promptBuilder.AppendLine("- Mention current library versions and when major updates occurred");
            promptBuilder.AppendLine("- If the solution is still current and valid, explicitly state that");
            promptBuilder.AppendLine("- ALWAYS include clickable file paths when referencing any found code: `path/to/file.ext`");

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Remember: Your goal is to save the user time by helping them rediscover, reuse, and modernize their own excellent work!");

            return promptBuilder.ToString();
        }
    }
}