# TotalRecall - Full-Stack RAG Application

A modern full-stack application implementing Retrieval-Augmented Generation (RAG) using Azure AI services. This project combines a .NET 9.0 backend API with a React frontend to provide semantic search capabilities over documents using vector embeddings.

## 🏗️ Architecture

### Backend (.NET 9.0)
- **Core/**: Shared business logic and models
- **Api/**: REST API for frontend communication
- **Indexer/**: Document processing and vector indexing service

### Frontend (React + TypeScript)
- **TanStack Router**: File-based routing system
- **TanStack Query**: Data fetching and caching
- **Tailwind CSS**: Utility-first CSS framework
- **Vite**: Fast development and build tool

## 🚀 Quick Start

### Prerequisites
- .NET 9.0 SDK
- Node.js (v18 or higher)
- Azure AI services (Search, OpenAI)

### Setup

1. **Clone and setup dependencies**
   ```bash
   # Clone the repository
   git clone <repository-url>
   cd total-recall

   # Install frontend dependencies
   npm run install:frontend
   ```

2. **Configure Azure services**
   Create a `.env` file in the root directory with your Azure credentials:
   ```env
   AZURE_SEARCH_ENDPOINT=your-search-endpoint
   AZURE_SEARCH_KEY=your-search-key
   AZURE_OPENAI_ENDPOINT=your-openai-endpoint
   AZURE_OPENAI_KEY=your-openai-key
   AZURE_EMBEDDING_ENDPOINT=your-embedding-endpoint
   AZURE_EMBEDDING_KEY=your-embedding-key
   ```

3. **Run the application**
   ```bash
   # Start both backend and frontend
   npm run dev
   ```

   This will start:
   - Backend API on `http://localhost:5000` (or similar)
   - Frontend on `http://localhost:3000`

## 🛠️ Development

### Full-Stack Development
- **Install dependencies**: `npm run install:frontend`
- **Run full stack**: `npm run dev`
- **Build full stack**: `npm run build`
- **Run all tests**: `npm run test`

### Backend Only
```bash
# Build the backend
dotnet build

# Run the API
dotnet run --project Api/Api.csproj

# Run tests
dotnet test
```

### Frontend Only
```bash
cd Frontend

# Install dependencies
npm install

# Run development server
npm run dev

# Build for production
npm run build

# Run tests
npm test

# Lint and format
npm run lint
npm run format
```

## 📋 Features

- **Document Indexing**: Process and index documents for semantic search
- **Vector Search**: Perform similarity searches using AI embeddings
- **RAG Pipeline**: Generate responses using retrieved context
- **Modern UI**: React-based frontend with Tailwind CSS
- **API Integration**: RESTful API connecting frontend to backend services

## 🔧 Configuration

The application uses Azure AI services for core functionality:

- **Azure AI Search**: Vector storage and similarity search
- **Azure OpenAI**: Embeddings generation and chat completions
- **Environment Configuration**: `.env` file for service credentials

## 📁 Project Structure

```
total-recall/
├── Core/                   # Shared business logic
├── Api/                    # REST API project
├── Indexer/               # Document processing service
├── Frontend/              # React application
│   ├── src/
│   │   ├── components/    # React components
│   │   ├── routes/        # Application routes
│   │   ├── lib/           # Utilities and helpers
│   │   └── integrations/  # External service integrations
│   ├── public/            # Static assets
│   └── package.json       # Frontend dependencies
├── package.json           # Root package.json (dev scripts)
├── TotalRecall.sln        # .NET solution file
└── .env                   # Environment variables (not committed)
```

## 🧪 Testing

The project includes comprehensive testing for both backend and frontend:

```bash
# Run all tests
npm run test

# Backend tests only
dotnet test

# Frontend tests only
cd Frontend && npm test
```

## 📦 Deployment

### Backend Deployment
Build the .NET application for your target platform:
```bash
dotnet build --configuration Release
```

### Frontend Deployment
Build the React application:
```bash
cd Frontend
npm run build
```

The build output will be in the `dist/` directory.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests and linting
5. Submit a pull request

## 📄 License

[Add your license information here]

## 🔗 Related Resources

- [Azure AI Search Documentation](https://learn.microsoft.com/azure/search/)
- [Azure OpenAI Documentation](https://learn.microsoft.com/azure/cognitive-services/openai/)
- [TanStack Router Documentation](https://tanstack.com/router)
- [TanStack Query Documentation](https://tanstack.com/query)