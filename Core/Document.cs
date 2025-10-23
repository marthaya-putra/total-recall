namespace TotalRecall.Core
{
    public class Document
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public float[] ContentVector { get; set; } = Array.Empty<float>();
    }
}