namespace TotalRecall.Core
{
    public class TextChunkerService
    {
        private readonly int _maxTokensPerChunk;
        private readonly int _averageTokensPerWord;

        public TextChunkerService(int maxTokensPerChunk = 8000, int averageTokensPerWord = 4)
        {
            _maxTokensPerChunk = maxTokensPerChunk;
            _averageTokensPerWord = averageTokensPerWord;
        }

        public List<string> ChunkText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>();

            // Check if chunking is needed
            if (!NeedsChunking(text))
                return new List<string> { text };

            var chunks = new List<string>();
            var maxWordsPerChunk = _maxTokensPerChunk / _averageTokensPerWord;
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < words.Length; i += maxWordsPerChunk)
            {
                var chunkWords = words.Skip(i).Take(maxWordsPerChunk);
                var chunk = string.Join(" ", chunkWords);
                chunks.Add(chunk);
            }

            return chunks;
        }

        public bool NeedsChunking(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            var estimatedTokens = wordCount * _averageTokensPerWord;
            return estimatedTokens > _maxTokensPerChunk;
        }
    }
}