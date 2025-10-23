# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Common Development Commands

### Building and Running
- **Build the project**: `dotnet build`
- **Run the application**: `dotnet run`
- **Run in debug mode**: `dotnet run --configuration Debug`
- **Build for release**: `dotnet build --configuration Release`

### Testing
- **Run tests**: `dotnet test` (if test projects are added)
- **Run specific test**: `dotnet test --filter "TestMethodName"`

## Architecture Overview

This is a .NET 9.0 console application that implements a RAG (Retrieval-Augmented Generation) system using Azure AI services. The application demonstrates semantic search over documents using vector embeddings.

### Core Components

- **Program.cs**: Main entry point containing the RAG pipeline logic
  - Environment variable validation and configuration loading
  - Azure OpenAI integration for embeddings and chat completions
  - Azure AI Search for vector indexing and retrieval
  - Complete workflow: indexing → search → generation

- **MyFiles.cs**: File system utility for scanning directories
  - Recursively finds files in specified paths
  - Supports excluding certain directories (node_modules, dist, etc.)
  - Handles file system access permissions

### Key Services and Dependencies

- **Azure.AI.OpenAI**: For generating embeddings and chat completions
- **Azure.Search.Documents**: For vector search operations
- **DotNetEnv**: For loading environment variables from .env file

### Configuration Requirements

The application requires these environment variables (set in .env file):
- `AZURE_SEARCH_ENDPOINT`: Azure AI Search service endpoint
- `AZURE_SEARCH_KEY`: Azure AI Search service key
- `AZURE_OPENAI_ENDPOINT`: Azure OpenAI service endpoint for chat
- `AZURE_OPENAI_KEY`: Azure OpenAI service key for chat
- `AZURE_EMBEDDING_ENDPOINT`: Azure OpenAI service endpoint for embeddings
- `AZURE_EMBEDDING_KEY`: Azure OpenAI service key for embeddings

### Data Flow

1. **Index Creation**: Creates Azure AI Search index with vector fields
2. **Document Processing**: Generates embeddings for sample documents
3. **Vector Search**: Performs similarity search using query embeddings
4. **Response Generation**: Uses retrieved context with GPT-4 for answers

### Vector Configuration

- Uses `text-embedding-3-large` model with 3072 dimensions
- Employs HNSW algorithm for efficient vector search
- Search index name: `documentindex`

## Important Notes

- The environment file (.env) contains sensitive API keys and should not be committed
- The current implementation uses sample documents but has infrastructure for processing real files
- Error handling includes detailed exception logging with stack traces