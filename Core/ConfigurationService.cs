using System.Collections;

namespace TotalRecall.Core
{
    public class ConfigurationService
    {
        private readonly Dictionary<string, string> _configs = new();

        public ConfigurationService()
        {
            LoadEnvironmentVariables();
        }

        public void LoadEnvironmentVariables()
        {
            _configs.Clear();

            var envVars = new Dictionary<string, string>
            {
                ["AZURE_SEARCH_ENDPOINT"] = Environment.GetEnvironmentVariable("AZURE_SEARCH_ENDPOINT") ?? "",
                ["AZURE_SEARCH_KEY"] = Environment.GetEnvironmentVariable("AZURE_SEARCH_KEY") ?? "",
                ["AZURE_OPENAI_ENDPOINT"] = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "",
                ["AZURE_OPENAI_KEY"] = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY") ?? "",
                ["AZURE_EMBEDDING_ENDPOINT"] = Environment.GetEnvironmentVariable("AZURE_EMBEDDING_ENDPOINT") ?? "",
                ["AZURE_EMBEDDING_KEY"] = Environment.GetEnvironmentVariable("AZURE_EMBEDDING_KEY") ?? "",
                ["SEARCH_INDEX_NAME"] = Environment.GetEnvironmentVariable("SEARCH_INDEX_NAME") ?? "my-code",
                ["EMBEDDING_DEPLOYMENT"] = Environment.GetEnvironmentVariable("EMBEDDING_DEPLOYMENT") ?? "text-embedding-3-large",
                ["COMPLETION_DEPLOYMENT"] = Environment.GetEnvironmentVariable("COMPLETION_DEPLOYMENT") ?? "gpt-4o",
                ["VECTOR_SIZE"] = Environment.GetEnvironmentVariable("VECTOR_SIZE") ?? "3072"
            };

            foreach (var kvp in envVars)
            {
                _configs[kvp.Key] = kvp.Value;
            }

            // Validate required variables
            var missingVars = GetMissingRequiredVariables();
            if (missingVars.Count > 0)
            {
                throw new InvalidOperationException($"Missing required environment variables: {string.Join(", ", missingVars)}");
            }
        }

        public string GetConfigValue(string key)
        {
            return _configs.TryGetValue(key, out var value) ? value : throw new KeyNotFoundException($"Configuration key '{key}' not found");
        }

        public Dictionary<string, string> GetAllConfigs()
        {
            return new Dictionary<string, string>(_configs);
        }

        public List<string> GetMissingRequiredVariables()
        {
            var requiredVars = new[]
            {
                "AZURE_SEARCH_ENDPOINT",
                "AZURE_SEARCH_KEY",
                "AZURE_OPENAI_ENDPOINT",
                "AZURE_OPENAI_KEY",
                "AZURE_EMBEDDING_ENDPOINT",
                "AZURE_EMBEDDING_KEY"
            };

            return requiredVars.Where(var => string.IsNullOrEmpty(_configs[var])).ToList();
        }

        public void ValidateConfiguration()
        {
            var missingVars = GetMissingRequiredVariables();
            if (missingVars.Count > 0)
            {
                Console.WriteLine("The following required environment variables are missing:");
                foreach (var missingVar in missingVars)
                {
                    Console.WriteLine($"  - {missingVar}");
                }
                Console.WriteLine("\nPlease set these environment variables in your .env file or system environment.");
                throw new InvalidOperationException($"Missing required environment variables: {string.Join(", ", missingVars)}");
            }

            // Validate configuration values
            if (string.IsNullOrWhiteSpace(_configs["SEARCH_INDEX_NAME"]))
            {
                _configs["SEARCH_INDEX_NAME"] = "my-code";
            }

            if (!int.TryParse(_configs["VECTOR_SIZE"], out var vectorSize) || vectorSize <= 0)
            {
                throw new InvalidOperationException("VECTOR_SIZE must be a positive integer");
            }

            if (string.IsNullOrWhiteSpace(_configs["EMBEDDING_DEPLOYMENT"]))
            {
                _configs["EMBEDDING_DEPLOYMENT"] = "text-embedding-3-large";
            }

            if (string.IsNullOrWhiteSpace(_configs["COMPLETION_DEPLOYMENT"]))
            {
                _configs["COMPLETION_DEPLOYMENT"] = "gpt-4o";
            }

            Console.WriteLine("Configuration validation successful");
        }

        public void PrintConfiguration()
        {
            Console.WriteLine("Current Configuration:");
            Console.WriteLine("======================");
            foreach (var kvp in _configs)
            {
                // Don't print sensitive information in full
                if (kvp.Key.EndsWith("KEY"))
                {
                    Console.WriteLine($"{kvp.Key}: {new string('*', kvp.Value.Length > 0 ? kvp.Value.Length : 10)}");
                }
                else
                {
                    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                }
            }
            Console.WriteLine("======================");
        }
    }
}