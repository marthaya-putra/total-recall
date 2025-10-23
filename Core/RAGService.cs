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
            var processedCount = 0;

            foreach (var filePath in filePaths)
            {
                try
                {
                    Console.WriteLine($"Processing file {processedCount + 1}/{filePaths.Count}: {filePath}");

                    string content;
                    try
                    {
                        content = await File.ReadAllTextAsync(filePath);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to read {filePath}: {ex.Message}");
                        continue;
                    }

                    var document = new Document
                    {
                        Id = SanitizeFileName(filePath),
                        Path = filePath,
                        Content = content
                    };

                    // Generate embedding for the document
                    document.ContentVector = await _aiService.GenerateDocumentEmbeddingAsync(document);
                    indexedDocuments.Add(document);

                    processedCount++;

                    if (processedCount % 10 == 0)
                    {
                        Console.WriteLine($"Processed {processedCount} files...");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to process {filePath}: {ex.Message}");
                    Console.WriteLine($"Error processing {filePath}: {ex.Message}");
                }
            }

            // Batch index all documents
            if (indexedDocuments.Any())
            {
                Console.WriteLine($"Indexing {indexedDocuments.Count} documents...");
                try
                {
                    await _searchService.IndexDocumentsAsync(indexedDocuments);
                    Console.WriteLine($"Successfully indexed {indexedDocuments.Count} documents");
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to index documents: {ex.Message}");
                    Console.WriteLine($"Error indexing documents: {ex.Message}");
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