# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Common Development Commands

### Full-Stack Development (Recommended)
- **Install dependencies**: `npm run install:frontend` (install npm dependencies for frontend)
- **Run full stack**: `npm run dev` (starts both API and frontend concurrently)
- **Build full stack**: `npm run build` (builds both API and frontend)
- **Run all tests**: `npm run test` (runs both backend and frontend tests)

### Backend (.NET) Development
- **Build the backend**: `dotnet build`
- **Run the API**: `dotnet run --project Api/Api.csproj`
- **Run in debug mode**: `dotnet run --configuration Debug`
- **Build for release**: `dotnet build --configuration Release`
- **Run backend tests**: `dotnet test`

### Frontend (React) Development
- **Navigate to frontend**: `cd Frontend`
- **Install dependencies**: `npm install`
- **Run frontend dev server**: `npm run dev`
- **Build frontend**: `npm run build`
- **Run frontend tests**: `npm test`
- **Lint code**: `npm run lint`
- **Format code**: `npm run format`

## Architecture Overview

This is a full-stack application implementing a RAG (Retrieval-Augmented Generation) system using Azure AI services. The application consists of a .NET 9.0 backend API and a React frontend, demonstrating semantic search over documents using vector embeddings.

### Backend Components (.NET)

- **Core/**: Shared business logic and models
- **Api/**: REST API project for frontend communication
- **Indexer/**: Document processing and vector indexing service
- **Program.cs** (in projects): Main entry points containing RAG pipeline logic
  - Environment variable validation and configuration loading
  - Azure OpenAI integration for embeddings and chat completions
  - Azure AI Search for vector indexing and retrieval
  - Complete workflow: indexing → search → generation

- **MyFiles.cs**: File system utility for scanning directories
  - Recursively finds files in specified paths
  - Supports excluding certain directories (node_modules, dist, etc.)
  - Handles file system access permissions

### Frontend Components (React)

- **Frontend/**: React application built with modern tooling
  - **TanStack Router**: File-based routing system
  - **TanStack Query**: Data fetching and caching
  - **Tailwind CSS**: Utility-first CSS framework
  - **TypeScript**: Type-safe JavaScript development
  - **Vite**: Fast development and build tool

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