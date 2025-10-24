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
        private readonly TextChunkerService _chunkerService;

        public RAGService(
            ConfigurationService configService,
            SearchIndexService searchService,
            AzureAIService aiService)
        {
            _configService = configService;
            _searchService = searchService;
            _aiService = aiService;
            _chunkerService = new TextChunkerService();
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
            const int batchSize = 100; // Reduced batch size to handle chunks

            for (int i = 0; i < filePaths.Count; i += batchSize)
            {
                var batchPaths = filePaths.Skip(i).Take(batchSize).ToList();
                Console.WriteLine($"Processing batch {i / batchSize + 1} ({batchPaths.Count} files)...");

                var allDocumentChunks = new List<Document>();

                foreach (var filePath in batchPaths)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(filePath);
                        var documentId = Guid.NewGuid().ToString();
                        var chunks = _chunkerService.ChunkText(content);
                        var documentChunks = new List<Document>();

                        if (chunks.Count == 1)
                        {
                            // No chunking needed, create single document
                            var doc = new Document
                            {
                                Id = documentId,
                                Path = filePath,
                                Content = chunks[0]
                            };

                            doc.ContentVector = await _aiService.GenerateDocumentEmbeddingAsync(doc);
                            allDocumentChunks.Add(doc);
                        }
                        else
                        {
                            // Process chunks and merge their embeddings
                            Console.WriteLine($"Document {filePath} requires {chunks.Count} chunks...");
                            var chunkEmbeddings = new List<float[]>();

                            for (int chunkIndex = 0; chunkIndex < chunks.Count; chunkIndex++)
                            {
                                var chunkDoc = new Document
                                {
                                    Id = $"{documentId}_chunk_{chunkIndex}",
                                    Path = filePath,
                                    Content = chunks[chunkIndex]
                                };

                                var chunkEmbedding = await _aiService.GenerateDocumentEmbeddingAsync(chunkDoc);
                                chunkEmbeddings.Add(chunkEmbedding);
                            }

                            // Merge all chunk embeddings into a single vector
                            var mergedEmbedding = MergeEmbeddings(chunkEmbeddings);

                            var doc = new Document
                            {
                                Id = documentId,
                                Path = filePath,
                                Content = content, // Store original full content
                                ContentVector = mergedEmbedding
                            };

                            allDocumentChunks.Add(doc);
                        }

                        Console.WriteLine($"Processed {allDocumentChunks.Count} documents for {filePath}");
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to process {filePath}: {ex.Message}");
                    }
                }

                if (allDocumentChunks.Any())
                {
                    Console.WriteLine($"Indexing batch {i / batchSize + 1} with {allDocumentChunks.Count} chunks...");
                    try
                    {
                        await _searchService.IndexDocumentsAsync(allDocumentChunks);
                        indexedDocuments.AddRange(allDocumentChunks);
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

        /// <summary>
        /// Merges multiple embeddings by averaging their values
        /// </summary>
        /// <param name="embeddings">List of embedding vectors to merge</param>
        /// <returns>Averaged embedding vector</returns>
        private static float[] MergeEmbeddings(List<float[]> embeddings)
        {
            if (embeddings == null || embeddings.Count == 0)
                return [];

            var vectorSize = embeddings[0].Length;
            var merged = new float[vectorSize];

            foreach (var embedding in embeddings)
            {
                if (embedding.Length != vectorSize)
                    throw new ArgumentException("Embedding dimensions must match.");

                for (int i = 0; i < vectorSize; i++)
                    merged[i] += embedding[i];
            }

            var count = embeddings.Count;
            for (int i = 0; i < vectorSize; i++)
                merged[i] /= count;

            return merged;
        }

    }
}