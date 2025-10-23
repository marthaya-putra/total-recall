using System.Text;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using TotalRecall.Core;

namespace TotalRecall.Core
{
    public class SearchIndexService
    {
        private readonly string _searchServiceEndpoint;
        private readonly string _searchServiceKey;
        private readonly string _indexName;
        private readonly int _vectorSize;

        public SearchIndexService(ConfigurationService configService)
        {
            var configs = configService.GetAllConfigs();
            _searchServiceEndpoint = configs["AZURE_SEARCH_ENDPOINT"];
            _searchServiceKey = configs["AZURE_SEARCH_KEY"];
            _indexName = configs["SEARCH_INDEX_NAME"];
            _vectorSize = int.Parse(configs["VECTOR_SIZE"]);
        }

        public async Task CreateSearchIndexAsync()
        {
            Console.WriteLine($"Creating search index: {_indexName}");
            var searchIndexClient = new SearchIndexClient(new Uri(_searchServiceEndpoint), new AzureKeyCredential(_searchServiceKey));

            var fields = new List<SearchField>
            {
                new SimpleField("Id", SearchFieldDataType.String) { IsKey = true },
                new SimpleField("Path", SearchFieldDataType.String),
                new SearchableField("Content") { AnalyzerName = LexicalAnalyzerName.StandardLucene },
                new SearchField("ContentVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = _vectorSize,
                    VectorSearchProfileName = "default"
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

            var index = new SearchIndex(_indexName, fields)
            {
                VectorSearch = vectorConfig
            };

            Console.WriteLine($"Calling CreateOrUpdateIndexAsync for index: {_indexName}");
            await searchIndexClient.CreateOrUpdateIndexAsync(index);
            Console.WriteLine($"Search index created successfully: {_indexName}");
        }

        public async Task IndexDocumentsAsync(List<Document> documents)
        {
            if (documents.Count == 0)
            {
                Console.WriteLine("No documents to index.");
                return;
            }

            int batchSize = 500;
            int concurrency = 8;

            foreach (var batch in documents.Chunk(batchSize))
            {
                Console.WriteLine($"Processing batch with {batch.Length} documents...");

                var semaphore = new SemaphoreSlim(concurrency);
                var readTasks = batch.Select(async document =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var doc = new Document
                        {
                            Id = SanitizeFileName(document.Path),
                            Path = document.Path,
                            Content = document.Content,
                        };
                        doc.ContentVector = document.ContentVector;
                        return doc;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                var processedDocuments = await Task.WhenAll(readTasks);

                Console.WriteLine($"Calling IndexDocumentsAsync API for batch of {processedDocuments.Length} documents...");
                var searchClient = new SearchClient(new Uri(_searchServiceEndpoint), _indexName, new AzureKeyCredential(_searchServiceKey));
                await searchClient.IndexDocumentsAsync(IndexDocumentsBatch.Upload(processedDocuments));
                Console.WriteLine($"Indexed batch of {processedDocuments.Length} documents successfully.");
            }

            Console.WriteLine("All document batches completed!");
        }

        public async Task<List<(string path, string content)>> SearchAsync(string query, float[] queryVector, int topK = 3)
        {
            Console.WriteLine($"Performing vector search for query: {query}");

            var vectorQuery = new VectorizedQuery(queryVector)
            {
                Fields = { "ContentVector" },
                KNearestNeighborsCount = topK
            };

            var searchOptions = new SearchOptions
            {
                Size = topK,
                Select = { "Content", "Path" },
                VectorSearch = new()
                {
                    Queries = { vectorQuery }
                }
            };

            var querySearchClient = new SearchClient(new Uri(_searchServiceEndpoint), _indexName, new AzureKeyCredential(_searchServiceKey));
            var searchResults = await querySearchClient.SearchAsync<Document>(null, searchOptions);

            Console.WriteLine("Vector search completed successfully");

            var contexts = new List<(string path, string content)>();
            await foreach (var result in searchResults.Value.GetResultsAsync())
            {
                contexts.Add((result.Document.Path, result.Document.Content));
            }

            Console.WriteLine($"Found {contexts.Count} relevant documents for query: {query}");
            return contexts;
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
    }
}