
using System.Text;
using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using DotNetEnv;
using TotalRecall.Core;

namespace TotalRecall.Indexer
{
    public class Program
    {
        private static readonly string? SearchServiceEndpoint = Environment.GetEnvironmentVariable("AZURE_SEARCH_ENDPOINT");
        private static readonly string? SearchServiceKey = Environment.GetEnvironmentVariable("AZURE_SEARCH_KEY");
        private static readonly string? OpenAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        private static readonly string? OpenAIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
        private static readonly string? EmbeddingEndpoint = Environment.GetEnvironmentVariable("AZURE_EMBEDDING_ENDPOINT");
        private static readonly string? EmbeddingKey = Environment.GetEnvironmentVariable("AZURE_EMBEDDING_KEY");

        private const string IndexName = "my-code";
        private const string EmbeddingDeployment = "text-embedding-3-large";
        private const string CompletionDeployment = "gpt-4o";
        private const int VectorSize = 3072;
        static async Task Main(string[] args)
        {
            Env.Load();

            var envVars = new List<string>();

            if (string.IsNullOrEmpty(SearchServiceEndpoint))
            {
                envVars.Add("AZURE_SEARCH_ENDPOINT");
            }

            if (string.IsNullOrEmpty(SearchServiceKey))
            {
                envVars.Add("AZURE_SEARCH_KEY");
            }
            if (string.IsNullOrEmpty(SearchServiceKey))
            {
                envVars.Add("AZURE_SEARCH_KEY");
            }
            if (string.IsNullOrEmpty(OpenAIEndpoint))
            {
                envVars.Add("AZURE_OPENAI_ENDPOINT");
            }

            if (string.IsNullOrEmpty(OpenAIKey))
            {
                envVars.Add("AZURE_OPENAI_KEY");
            }

            if (string.IsNullOrEmpty(EmbeddingEndpoint))
            {
                envVars.Add("AZURE_EMBEDDING_ENDPOINT");
            }

            if (string.IsNullOrEmpty(EmbeddingKey))
            {
                envVars.Add("AZURE_EMBEDDING_KEY");
            }


            if (envVars.Count > 0)
            {
                envVars.ForEach(v => Console.WriteLine($"Please set the environment variable: {v}"));
                return;
            }

            try
            {
                // Console.WriteLine("Calling CreateSearchIndexAsync API...");
                // await CreateSearchIndexAsync();
                // Console.WriteLine("CreateSearchIndexAsync API call completed");

                // var myFiles = new MyFiles("/Users/marthayaputra/Documents/code/hirelytics-dashboard", new HashSet<string>(["node_modules", "dist", ".turbo", ".next", "assets", ".git", ".angular"]));
                // Console.WriteLine("Getting all files...");
                // var allFiles = myFiles.GetFileNames();
                // Console.WriteLine($"files count: {allFiles.Count}");
                // await IndexingFilesAsync(allFiles);

                // // Wait for indexing to complete
                // await Task.Delay(2000);

                // Search query
                var searchText = "What is the best way of building charts in Angular?";
                Console.WriteLine($"Searching for: {searchText}");

                // Generate embedding for query
                Console.WriteLine("Calling GetEmbeddingsAsync API for search query...");
                var queryEmbeddingClient = new OpenAIClient(new Uri(EmbeddingEndpoint!), new AzureKeyCredential(EmbeddingKey!));
                var queryOptions = new EmbeddingsOptions(EmbeddingDeployment, [searchText]);
                var queryEmbedding = await queryEmbeddingClient.GetEmbeddingsAsync(queryOptions);

                Console.WriteLine("GetEmbeddingsAsync API call completed for search query");

                var vector = queryEmbedding.Value.Data[0].Embedding.ToArray();
                var vectorQuery = new VectorizedQuery(vector)
                {
                    Fields = { "ContentVector" },
                    KNearestNeighborsCount = 3
                };

                // Vector search
                var searchOptions = new SearchOptions
                {
                    Size = 3,
                    Select = { "Content", "Path" },
                    VectorSearch = new()
                    {
                        Queries = { vectorQuery }
                    }
                };

                // Perform search with vector similarity
                Console.WriteLine("Calling SearchAsync API...");
                var querySearchClient = new SearchClient(new Uri(SearchServiceEndpoint!), IndexName, new AzureKeyCredential(SearchServiceKey!));
                var searchResults = await querySearchClient.SearchAsync<Document>(null, searchOptions);
                Console.WriteLine("SearchAsync API call completed");
                var contexts = new List<(string path, string content)>();

                await foreach (var result in searchResults.Value.GetResultsAsync())
                {
                    contexts.Add((result.Document.Path, result.Document.Content));
                }

                // Generate completion using dynamic prompt builder
                var systemPrompt = BuildDynamicSystemPrompt(contexts, searchText);
                var messages = new List<ChatRequestMessage>
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage($"Question: {searchText}")
                };

                var completionOptions = new ChatCompletionsOptions(CompletionDeployment, messages);
                Console.WriteLine("Calling GetChatCompletionsAsync API...");
                var openAIClient = new OpenAIClient(new Uri(OpenAIEndpoint!), new AzureKeyCredential(OpenAIKey!));
                var completionResponse = await openAIClient.GetChatCompletionsAsync(completionOptions);
                Console.WriteLine("GetChatCompletionsAsync API call completed");

                Console.WriteLine("\nAI Response:");
                Console.WriteLine(completionResponse.Value.Choices[0].Message.Content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner error: {ex.InnerException.Message}");
                }
            }
        }

        private static async Task CreateSearchIndexAsync()
        {
            var searchIndexClient = new SearchIndexClient(new Uri(SearchServiceEndpoint!), new AzureKeyCredential(SearchServiceKey!));

            var fields = new List<SearchField>
            {
                new SimpleField("Id", SearchFieldDataType.String) { IsKey = true },
                new SimpleField("Path", SearchFieldDataType.String),
                new SearchableField("Content") { AnalyzerName = LexicalAnalyzerName.StandardLucene },
                new SearchField("ContentVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = VectorSize,
                    VectorSearchProfileName = "default"  // This links to the algorithm configuration name
                }
            };

            var vectorConfig = new VectorSearch
            {
                Algorithms = {
                    new HnswAlgorithmConfiguration("myHnsw")
                },
                Profiles = {
                    new VectorSearchProfile("default", "myHnsw")
                }
            };

            var index = new SearchIndex(IndexName, fields)
            {
                VectorSearch = vectorConfig
            };

            Console.WriteLine("Calling CreateOrUpdateIndexAsync API...");
            await searchIndexClient.CreateOrUpdateIndexAsync(index);
            Console.WriteLine("CreateOrUpdateIndexAsync API call completed");
        }

        private static async Task IndexingFilesAsync(List<string> files)
        {
            int batchSize = 500;
            int concurrency = 8;

            foreach (var batch in files.Chunk(batchSize))
            {
                Console.WriteLine($"Processing batch with {batch.Length} files...");

                // Use a semaphore to limit concurrent reads
                var semaphore = new SemaphoreSlim(concurrency);
                var readTasks = batch.Select(async file =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        string content = await File.ReadAllTextAsync(file);
                        var doc = new Document
                        {
                            Id = SanitizeFileName(file),
                            Path = file,
                            Content = content,
                        };
                        doc.ContentVector = await CreateEmbbedingAsync(doc);

                        return doc;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                var documents = await Task.WhenAll(readTasks);

                // Index documents
                Console.WriteLine("Calling IndexDocumentsAsync API...");
                var searchClient = new SearchClient(new Uri(SearchServiceEndpoint!), IndexName, new AzureKeyCredential(SearchServiceKey!));
                await searchClient.IndexDocumentsAsync(IndexDocumentsBatch.Upload(documents));
                Console.WriteLine("IndexDocumentsAsync API call completed");
                Console.WriteLine($"Uploaded batch of {documents.Length} files.\n");
            }

            Console.WriteLine("All batches completed!");
        }

        private static async Task<float[]> CreateEmbbedingAsync(Document doc)
        {
            var embeddingClient = new OpenAIClient(new Uri(EmbeddingEndpoint!), new AzureKeyCredential(EmbeddingKey!));
            Console.WriteLine($"Calling GetEmbeddingsAsync API for document {doc.Id}...");
            var embeddingOptions = new EmbeddingsOptions(EmbeddingDeployment, [doc.Content]);
            var embeddingResponse = await embeddingClient.GetEmbeddingsAsync(embeddingOptions);
            Console.WriteLine($"GetEmbeddingsAsync API call completed for document {doc.Id}");
            return embeddingResponse.Value.Data[0].Embedding.ToArray();
        }


        /// <summary>
        /// Sanitizes a file path by replacing special characters with underscores.
        /// Only allows letters, digits, underscores, and dashes.
        /// Ensures the result doesn't start with an underscore.
        /// </summary>
        /// <param name="filePath">The original file path</param>
        /// <returns>Sanitized file path with only allowed characters</returns>
        private static string SanitizeFileName(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return filePath;

            // Replace any character that's not a letter, digit, underscore, or dash with underscore
            // This includes directory separators which will be replaced with underscores
            string sanitized = System.Text.RegularExpressions.Regex.Replace(filePath, @"[^a-zA-Z0-9_-]", "_");

            // Remove leading underscores that were created from special characters
            sanitized = sanitized.TrimStart('_');

            // If the result is empty (all characters were invalid), use a default name
            if (string.IsNullOrEmpty(sanitized))
                return "file";

            return sanitized;
        }

        /// <summary>
        /// Builds a dynamic system prompt based on available context and question type
        /// </summary>
        /// <param name="contexts">List of context tuples (path, content) from search results</param>
        /// <param name="question">The user's question</param>
        /// <returns>Dynamically built system prompt</returns>
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
