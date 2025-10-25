using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TotalRecall.Core
{
    public class TextChunkerService
    {
        private readonly int _maxTokensPerChunk;
        private readonly TokenCounterService _tokenCounter;
        private readonly OpenAIModel _model;
        private readonly double _charsPerToken;

        public TextChunkerService(
            int maxTokensPerChunk = 8000,
            OpenAIModel model = OpenAIModel.TextEmbedding3Large,
            TokenCounterService? tokenCounter = null)
        {
            _maxTokensPerChunk = maxTokensPerChunk;
            _model = model;
            _tokenCounter = tokenCounter ?? new TokenCounterService(model);
            _charsPerToken = EstimateCharsPerToken(model);
        }

        public List<string> ChunkText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>();

            // Check if chunking is needed
            if (!NeedsChunking(text))
                return new List<string> { text };

            return CreateSimpleChunks(text);
        }

        public async Task<List<string>> ChunkTextAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>();

            // Check if chunking is needed
            if (!await NeedsChunkingAsync(text))
                return new List<string> { text };

            return await CreateSimpleChunksAsync(text);
        }

        public bool NeedsChunking(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            var tokenCount = _tokenCounter.CountTokens(text, _model);
            return tokenCount > _maxTokensPerChunk;
        }

        public async Task<bool> NeedsChunkingAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            var tokenCount = await _tokenCounter.CountTokensAsync(text, _model);
            return tokenCount > _maxTokensPerChunk;
        }

        private List<string> CreateSimpleChunks(string text)
        {
            var chunks = new List<string>();

            // Estimate max characters per chunk based on token limit
            var maxCharsPerChunk = (int)(_maxTokensPerChunk * _charsPerToken);

            // Simple character-based chunking
            for (int i = 0; i < text.Length; i += maxCharsPerChunk)
            {
                var chunkLength = Math.Min(maxCharsPerChunk, text.Length - i);
                var chunk = text.Substring(i, chunkLength);
                chunks.Add(chunk);
            }

            // Verify and adjust chunks that might exceed token limits
            return AdjustChunksForTokenLimits(chunks);
        }

        private async Task<List<string>> CreateSimpleChunksAsync(string text)
        {
            var chunks = new List<string>();

            // Estimate max characters per chunk based on token limit
            var maxCharsPerChunk = (int)(_maxTokensPerChunk * _charsPerToken);

            // Simple character-based chunking
            for (int i = 0; i < text.Length; i += maxCharsPerChunk)
            {
                var chunkLength = Math.Min(maxCharsPerChunk, text.Length - i);
                var chunk = text.Substring(i, chunkLength);
                chunks.Add(chunk);
            }

            // Verify and adjust chunks that might exceed token limits
            return await AdjustChunksForTokenLimitsAsync(chunks);
        }

        private List<string> AdjustChunksForTokenLimits(List<string> chunks)
        {
            var adjustedChunks = new List<string>();

            foreach (var chunk in chunks)
            {
                var tokenCount = _tokenCounter.CountTokens(chunk, _model);

                if (tokenCount <= _maxTokensPerChunk)
                {
                    adjustedChunks.Add(chunk);
                }
                else
                {
                    // Split chunk further if it exceeds token limit
                    adjustedChunks.AddRange(SplitOversizedChunk(chunk));
                }
            }

            return adjustedChunks;
        }

        private async Task<List<string>> AdjustChunksForTokenLimitsAsync(List<string> chunks)
        {
            var adjustedChunks = new List<string>();

            foreach (var chunk in chunks)
            {
                var tokenCount = await _tokenCounter.CountTokensAsync(chunk, _model);

                if (tokenCount <= _maxTokensPerChunk)
                {
                    adjustedChunks.Add(chunk);
                }
                else
                {
                    // Split chunk further if it exceeds token limit
                    adjustedChunks.AddRange(await SplitOversizedChunkAsync(chunk));
                }
            }

            return adjustedChunks;
        }

        private List<string> SplitOversizedChunk(string chunk)
        {
            var subChunks = new List<string>();

            // Calculate better character limit based on actual token density
            var actualTokenCount = _tokenCounter.CountTokens(chunk, _model);
            var actualCharsPerToken = (double)chunk.Length / actualTokenCount;
            var adjustedMaxChars = (int)(_maxTokensPerChunk * actualCharsPerToken * 0.9); // 90% safety margin

            for (int i = 0; i < chunk.Length; i += adjustedMaxChars)
            {
                var subChunkLength = Math.Min(adjustedMaxChars, chunk.Length - i);
                var subChunk = chunk.Substring(i, subChunkLength);
                subChunks.Add(subChunk);
            }

            return subChunks;
        }

        private async Task<List<string>> SplitOversizedChunkAsync(string chunk)
        {
            var subChunks = new List<string>();

            // Calculate better character limit based on actual token density
            var actualTokenCount = await _tokenCounter.CountTokensAsync(chunk, _model);
            var actualCharsPerToken = (double)chunk.Length / actualTokenCount;
            var adjustedMaxChars = (int)(_maxTokensPerChunk * actualCharsPerToken * 0.9); // 90% safety margin

            for (int i = 0; i < chunk.Length; i += adjustedMaxChars)
            {
                var subChunkLength = Math.Min(adjustedMaxChars, chunk.Length - i);
                var subChunk = chunk.Substring(i, subChunkLength);
                subChunks.Add(subChunk);
            }

            return subChunks;
        }

        private static double EstimateCharsPerToken(OpenAIModel model)
        {
            return model switch
            {
                OpenAIModel.GPT3_5_Turbo => 3.5,  // ~3.5 chars per token for cl100k_base
                OpenAIModel.GPT4 => 3.5,           // ~3.5 chars per token for cl100k_base
                OpenAIModel.GPT4_Turbo => 3.5,     // ~3.5 chars per token for cl100k_base
                OpenAIModel.GPT4o => 4.0,         // ~4.0 chars per token for o200k_base
                OpenAIModel.GPT4o_Mini => 4.0,    // ~4.0 chars per token for o200k_base
                OpenAIModel.TextEmbedding3Small => 3.5,  // ~3.5 chars per token for cl100k_base
                OpenAIModel.TextEmbedding3Large => 3.5,  // ~3.5 chars per token for cl100k_base
                _ => 3.7  // Conservative default
            };
        }

        public int GetTokenCount(string text)
        {
            return _tokenCounter.CountTokens(text, _model);
        }

        public async Task<int> GetTokenCountAsync(string text)
        {
            return await _tokenCounter.CountTokensAsync(text, _model);
        }

        public List<string> GetTokenSample(string text, int maxTokens = 10)
        {
            return _tokenCounter.GetTokenSample(text, _model, maxTokens);
        }
    }
}