using System;
using System.Text.Json;
using Azure;
using Azure.Search.Documents;

namespace TotalRecall.Core
{
    public class RAGService
    {
        private readonly AzureAIService _aiService;
        private readonly SearchIndexService _searchService;
        private readonly ConfigurationService _configService;

        public RAGService(
            ConfigurationService configService,
            SearchIndexService searchService,
            AzureAIService aiService)
        {
            _configService = configService;
            _searchService = searchService;
            _aiService = aiService;
        }

        public async Task<List<(string path, string content)>> SearchContextsAsync(string query, int topK = 3)
        {
            try
            {
                // Step 1: Generate embedding for the query
                Console.WriteLine($"Step 1: Generating embedding for query: {query}");
                var queryVector = await _aiService.GenerateQueryEmbeddingAsync(query);

                // Step 2: Perform vector search and return contexts
                Console.WriteLine($"Step 2: Performing vector search with top {topK} results");
                var contexts = await _searchService.SearchAsync(query, queryVector, topK);

                return contexts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SearchContextsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<(List<Document> indexedDocuments, List<string> errors)> IndexDocumentsAsync(List<string> filePaths)
        {
            var indexedDocuments = new List<Document>();
            var errors = new List<string>();
            const int batchSize = 500;
            const int maxConcurrentTasks = 10;

            for (int i = 0; i < filePaths.Count; i += batchSize)
            {
                var batchPaths = filePaths.Skip(i).Take(batchSize).ToList();
                Console.WriteLine($"Processing batch {i / batchSize + 1} ({batchPaths.Count} files)...");

                var semaphore = new SemaphoreSlim(maxConcurrentTasks);
                var batchTasks = new List<Task<Document?>>();

                foreach (var filePath in batchPaths)
                {
                    await semaphore.WaitAsync();

                    batchTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var content = await File.ReadAllTextAsync(filePath);
                            var doc = new Document
                            {
                                Id = Guid.NewGuid().ToString(),
                                Path = filePath,
                                Content = content
                            };

                            doc.ContentVector = await _aiService.GenerateDocumentEmbeddingAsync(doc);
                            return doc;
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Failed to process {filePath}: {ex.Message}");
                            return null;
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }

                var completed = await Task.WhenAll(batchTasks);
                var batchDocuments = completed.Where(d => d != null).ToList()!;

                if (batchDocuments.Any())
                {
                    Console.WriteLine($"Indexing batch {i / batchSize + 1} with {batchDocuments.Count} documents...");
                    try
                    {
                        await _searchService.IndexDocumentsAsync(batchDocuments!);
                        indexedDocuments.AddRange(batchDocuments!);
                        Console.WriteLine($"Successfully indexed batch {i / batchSize + 1}");
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to index batch {i / batchSize + 1}: {ex.Message}");
                    }
                }
            }

            return (indexedDocuments, errors);
        }


        public async Task<(bool success, string message)> EnsureIndexExistsAsync()
        {
            try
            {
                // Check if index already exists by attempting to create it
                Console.WriteLine("Checking if search index exists...");
                try
                {
                    await _searchService.CreateSearchIndexAsync();
                    return (true, "Search index created successfully");
                }
                catch (Exception ex) when (ex.Message.Contains("already exists") || ex.Message.Contains("Index"))
                {
                    // Index likely already exists, try to use it
                    return (true, "Index already exists");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error creating search index: {ex.Message}");
            }
        }

  
        public string GetIndexInfo()
        {
            var config = _configService.GetAllConfigs();
            return JsonSerializer.Serialize(new
            {
                SearchEndpoint = config["AZURE_SEARCH_ENDPOINT"],
                IndexName = config["SEARCH_INDEX_NAME"],
                VectorSize = config["VECTOR_SIZE"],
                EmbeddingModel = config["EMBEDDING_DEPLOYMENT"],
                CompletionModel = config["COMPLETION_DEPLOYMENT"]
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}