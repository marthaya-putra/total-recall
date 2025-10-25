using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TiktokenSharp;

namespace TotalRecall.Core
{
    public enum OpenAIModel
    {
        GPT3_5_Turbo,
        GPT4,
        GPT4_Turbo,
        GPT4o,
        GPT4o_Mini,
        TextEmbedding3Small,
        TextEmbedding3Large
    }

    public class TokenCounterService
    {
        private readonly Dictionary<OpenAIModel, TikToken> _tokenizers;
        private readonly OpenAIModel _defaultModel;

        public TokenCounterService(OpenAIModel defaultModel = OpenAIModel.GPT4)
        {
            _defaultModel = defaultModel;
            _tokenizers = new Dictionary<OpenAIModel, TikToken>();

            // Initialize tokenizers for different models
            InitializeTokenizers();
        }

        private void InitializeTokenizers()
        {
            // Common encoding names for different OpenAI models
            var modelEncodings = new Dictionary<OpenAIModel, string>
            {
                { OpenAIModel.GPT3_5_Turbo, "cl100k_base" },     // gpt-3.5-turbo, gpt-3.5-turbo-16k
                { OpenAIModel.GPT4, "cl100k_base" },             // gpt-4, gpt-4-32k
                { OpenAIModel.GPT4_Turbo, "cl100k_base" },       // gpt-4-turbo
                { OpenAIModel.GPT4o, "o200k_base" },             // gpt-4o, gpt-4o-2024-05-13
                { OpenAIModel.GPT4o_Mini, "o200k_base" },        // gpt-4o-mini
                { OpenAIModel.TextEmbedding3Small, "cl100k_base" }, // text-embedding-3-small
                { OpenAIModel.TextEmbedding3Large, "cl100k_base" }  // text-embedding-3-large
            };

            foreach (var model in modelEncodings)
            {
                try
                {
                    var tokenizer = TikToken.GetEncoding(model.Value);
                    _tokenizers[model.Key] = tokenizer;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to initialize tokenizer for {model.Key}: {ex.Message}");
                    // Fall back to cl100k_base if specific encoding fails
                    if (model.Value != "cl100k_base")
                    {
                        try
                        {
                            var fallbackTokenizer = TikToken.GetEncoding("cl100k_base");
                            _tokenizers[model.Key] = fallbackTokenizer;
                        }
                        catch (Exception fallbackEx)
                        {
                            Console.WriteLine($"Failed to initialize fallback tokenizer for {model.Key}: {fallbackEx.Message}");
                        }
                    }
                }
            }
        }

        public int CountTokens(string text, OpenAIModel? model = null)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            var targetModel = model ?? _defaultModel;

            if (!_tokenizers.TryGetValue(targetModel, out var tokenizer))
            {
                Console.WriteLine($"Tokenizer not found for model {targetModel}, using default");
                tokenizer = _tokenizers[_defaultModel];
            }

            try
            {
                var tokens = tokenizer.Encode(text);
                return tokens.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error counting tokens: {ex.Message}");
                // Fall back to rough estimation if tokenization fails
                return EstimateTokensRoughly(text);
            }
        }

        public async Task<int> CountTokensAsync(string text, OpenAIModel? model = null)
        {
            return await Task.FromResult(CountTokens(text, model));
        }

        public List<string> GetTokenSample(string text, OpenAIModel? model = null, int maxTokensToShow = 10)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>();

            var targetModel = model ?? _defaultModel;

            if (!_tokenizers.TryGetValue(targetModel, out var tokenizer))
            {
                tokenizer = _tokenizers[_defaultModel];
            }

            try
            {
                var tokens = tokenizer.Encode(text);
                var decodedTokens = tokens.Take(maxTokensToShow).Select(token => tokenizer.Decode(new List<int> { token })).ToList();
                return decodedTokens;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting token sample: {ex.Message}");
                return new List<string>();
            }
        }

        private int EstimateTokensRoughly(string text)
        {
            // Rough estimation: ~4 characters per token for English text
            // This is a fallback when tiktoken fails
            return (int)Math.Ceiling(text.Length / 4.0);
        }

        public OpenAIModel GetModelForEmbedding(string embeddingModel)
        {
            return embeddingModel.ToLower() switch
            {
                var s when s.Contains("small") => OpenAIModel.TextEmbedding3Small,
                var s when s.Contains("large") => OpenAIModel.TextEmbedding3Large,
                _ => OpenAIModel.TextEmbedding3Large
            };
        }

        public OpenAIModel GetModelForChat(string chatModel)
        {
            return chatModel.ToLower() switch
            {
                var s when s.Contains("gpt-3.5") => OpenAIModel.GPT3_5_Turbo,
                var s when s.Contains("gpt-4o-mini") => OpenAIModel.GPT4o_Mini,
                var s when s.Contains("gpt-4o") => OpenAIModel.GPT4o,
                var s when s.Contains("gpt-4-turbo") => OpenAIModel.GPT4_Turbo,
                var s when s.Contains("gpt-4") => OpenAIModel.GPT4,
                _ => OpenAIModel.GPT4
            };
        }
    }
}