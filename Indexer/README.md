# Total Recall Indexer

A console application for indexing documents and performing vector search operations using Azure AI services.

## Prerequisites

- .NET 9.0 SDK
- Azure AI Search service
- Azure OpenAI service
- Environment variables configured in `.env` file

## Configuration

The indexer requires the following environment variables in your `.env` file:

```env
AZURE_SEARCH_ENDPOINT=your_search_service_endpoint
AZURE_SEARCH_KEY=your_search_service_key
AZURE_OPENAI_ENDPOINT=your_openai_service_endpoint
AZURE_OPENAI_KEY=your_openai_service_key
AZURE_EMBEDDING_ENDPOINT=your_embedding_endpoint
AZURE_EMBEDDING_KEY=your_embedding_key
```

## Usage

### Build the Project

```bash
dotnet build
```

### Run the Indexer

```bash
dotnet run -- [command] [arguments]
```

## Available Commands

### 1. Index Documents

Index all files from a specified directory:

```bash
dotnet run -- index <directory_path>
```

**Example:**
```bash
dotnet run -- index "/Users/username/projects/myapp"
```

**Features:**
- Recursively scans directories
- Excludes common build folders: `node_modules`, `dist`, `.turbo`, `.next`, `assets`, `.git`, `.angular`
- Generates embeddings for document content
- Stores documents in Azure AI Search vector index
- Provides indexing summary with success/error counts

### 2. Show Configuration Information

Display current configuration and index information:

```bash
dotnet run -- info
```

### 3. Default Search Demo (No arguments)

Run a demo search with a predefined query:

```bash
dotnet run
```

This will execute a search with the query: "What is the best way of building charts in Angular?"

## Search Results

When you run a search, the application will:

1. Generate embeddings for your query
2. Perform vector search in the indexed documents
3. Display the top matching contexts with:
   - File path
   - Content snippet
   - Relevance ranking

## Output Examples

### Indexing Output
```
📁 Running document indexing...
Directory: /Users/username/projects/myapp
=====================================
Scanning for files...
Found 42 files to index
Starting document indexing...

Indexing Summary:
✅ Successfully indexed: 38 files
❌ Errors occurred: 4 files
   • /path/to/large-file.pdf: File too large
   • /path/to/binary.exe: Unsupported file type
=====================================
```

### Search Output
```
🔍 Running default search demo...
=====================================
Searching for: What is the best way of building charts in Angular?

📝 Retrieved Contexts:
📄 Path: /docs/angular-charts.md
Content: Chart.js is a popular library for building responsive charts in Angular applications...

📄 Path: /src/app/chart.component.ts
Content: This component demonstrates how to integrate D3.js with Angular for complex visualizations...

=====================================
```

## Error Handling

The application provides detailed error information:

- Configuration validation errors
- File access permission issues
- Azure service connection problems
- Indexing failures with specific file details

## Troubleshooting

1. **Missing Environment Variables**: Ensure all required Azure service endpoints and keys are set in your `.env` file
2. **Index Creation Failed**: Check Azure AI Search service permissions and capacity
3. **File Access Denied**: Verify read permissions for the target directory
4. **Large Files**: Some files may be too large for processing and will be skipped with errors logged

## Integration Notes

The Indexer is designed as a standalone tool for:
- Initial document indexing
- Testing search functionality
- Maintenance operations on the vector index

For production use, consider integrating the RAG service directly into your application for real-time search and retrieval.