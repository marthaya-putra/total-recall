
using DotNetEnv;
using TotalRecall.Core;

namespace TotalRecall.Indexer
{
    public class Program
    {
        private static RAGService? _ragService;
        private static ConfigurationService? _configService;

        static async Task Main(string[] args)
        {
            // Load environment variables
            Env.Load();

            try
            {
                Console.WriteLine("Initializing Total Recall Indexer...");
                InitializeServices();

                // Display configuration
                _configService!.PrintConfiguration();


                if (args[0].Equals("index", StringComparison.OrdinalIgnoreCase) && args.Length > 1)
                {
                    await RunIndexing(args[1]);
                }
                else if (args[0].Equals("info", StringComparison.OrdinalIgnoreCase))
                {
                    ShowInfo();
                }
                else
                {
                    ShowUsage();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner error: {ex.InnerException.Message}");
                }
                Environment.Exit(1);
            }
        }

        private static void InitializeServices()
        {
            Console.WriteLine("Initializing services...");
            _configService = new ConfigurationService();
            _configService.ValidateConfiguration();

            var searchService = new SearchIndexService(_configService);
            var aiService = new AzureAIService(_configService);
            _ragService = new RAGService(_configService, searchService, aiService);
            Console.WriteLine("Services initialized successfully.");
        }

        private static async Task RunDefaultSearch()
        {
            Console.WriteLine("🔍 Running default search demo...");
            Console.WriteLine("=====================================");

            // Search query
            var searchText = "What is the best way of building charts in Angular?";
            Console.WriteLine($"Searching for: {searchText}");

            try
            {
                var contexts = await _ragService!.SearchContextsAsync(searchText);
                Console.WriteLine("\n📝 Retrieved Contexts:");
                foreach (var (path, content) in contexts)
                {
                    Console.WriteLine($"📄 Path: {path}");
                    Console.WriteLine($"Content: {content}\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Search failed: {ex.Message}");
            }

            Console.WriteLine("\n=====================================");
        }

        private static async Task RunSearch(string query)
        {
            Console.WriteLine("🔍 Running custom search...");
            Console.WriteLine($"Query: {query}");
            Console.WriteLine("=====================================");

            try
            {
                var contexts = await _ragService!.SearchContextsAsync(query);
                Console.WriteLine("\n📝 Retrieved Contexts:");
                foreach (var (path, content) in contexts)
                {
                    Console.WriteLine($"📄 Path: {path}");
                    Console.WriteLine($"Content: {content}\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Search failed: {ex.Message}");
            }

            Console.WriteLine("\n=====================================");
        }

        private static async Task RunIndexing(string directoryPath)
        {
            Console.WriteLine("📁 Running document indexing...");
            Console.WriteLine($"Directory: {directoryPath}");
            Console.WriteLine("=====================================");

            try
            {
                // Get files from directory
                var myFiles = new MyFiles(directoryPath, new HashSet<string>(["node_modules", "dist", ".turbo", ".next", "assets", ".git", ".angular"]));
                Console.WriteLine("Scanning for files...");
                var allFiles = myFiles.GetFileNames();
                Console.WriteLine($"Found {allFiles.Count} files to index");

                if (allFiles.Count == 0)
                {
                    Console.WriteLine("No files found to index.");
                    return;
                }

                // Ensure index exists
                var indexResult = await _ragService!.EnsureIndexExistsAsync();
                if (!indexResult.success)
                {
                    Console.WriteLine($"❌ Index creation failed: {indexResult.message}");
                    return;
                }

                // Index documents
                Console.WriteLine("Starting document indexing...");
                var (indexedDocuments, errors) = await _ragService.IndexDocumentsAsync(allFiles);

                Console.WriteLine("\nIndexing Summary:");
                Console.WriteLine($"✅ Successfully indexed: {indexedDocuments.Count} files");

                if (errors.Any())
                {
                    Console.WriteLine($"❌ Errors occurred: {errors.Count} files");
                    foreach (var error in errors)
                    {
                        Console.WriteLine($"   • {error}");
                    }
                }

                Console.WriteLine("=====================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Indexing failed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner error: {ex.InnerException.Message}");
                }
                Console.WriteLine("\n=====================================");
            }
        }


        private static void ShowInfo()
        {
            Console.WriteLine("ℹ️ Total Recall Information");
            Console.WriteLine("=========================");

            try
            {
                var config = _ragService!.GetIndexInfo();
                Console.WriteLine("Configuration:");
                Console.WriteLine(config);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to get info: {ex.Message}");
            }

            Console.WriteLine("=========================");
        }

        private static void ShowUsage()
        {
            Console.WriteLine("📖 Total Recall Indexer Usage");
            Console.WriteLine("============================");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  (default)          - Run default search demo");
            Console.WriteLine("  search <query>     - Search with custom query");
            Console.WriteLine("  index <directory>  - Index files from directory");
            Console.WriteLine("  create-index       - Create search index");
            Console.WriteLine("  info              - Show configuration information");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run -- --search \"How do I handle authentication?\"");
            Console.WriteLine("  dotnet run -- index \"/path/to/code\"");
            Console.WriteLine();
            Console.WriteLine("============================");
        }
    }
}
