namespace TotalRecall.Core
{
    public class MyFiles
    {
        private readonly HashSet<string> excludePaths = [];
        private readonly List<string> fileNames = [];
        public string RootDir
        {
            get; private set;
        }
        public MyFiles(string path, HashSet<string>? excludePaths)
        {
            RootDir = path;
            this.excludePaths = excludePaths ?? [];
        }

        public List<string> GetFileNames()
        {
            try
            {
                var allFiles = Directory.EnumerateFiles(RootDir, "*.*", SearchOption.AllDirectories).ToList();
                if (this.excludePaths.Count == 0)
                {
                    fileNames.AddRange(allFiles);
                    return fileNames;
                }

                fileNames.AddRange(allFiles.Where(ShouldNotIncludeSelectedFiles));

                return this.fileNames;
            }
            catch (UnauthorizedAccessException ex)
            {
                fileNames.Clear();
                throw new UnauthorizedAccessException($"Access denied to directory: {RootDir}", ex);
            }
            catch (DirectoryNotFoundException)
            {
                fileNames.Clear();
                throw;
            }
            catch (Exception ex)
            {
                fileNames.Clear();
                throw new InvalidOperationException($"Failed to get files from {RootDir}", ex);
            }

        }

        private bool ShouldNotIncludeSelectedFiles(string file)
        {
            return !excludePaths.Any(ex => file.Contains(ex, StringComparison.OrdinalIgnoreCase)) && (Path.GetExtension(file) == ".js" || Path.GetExtension(file) == ".ts");
        }
    }
}