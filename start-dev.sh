#!/bin/bash

# TotalRecall Development Startup Script
# This script starts both the .NET API backend and React frontend

echo "🚀 Starting TotalRecall development environment..."
echo "=================================="

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET CLI not found. Please install .NET 9.0 SDK."
    exit 1
fi

# Check if Node.js is installed
if ! command -v node &> /dev/null; then
    echo "❌ Node.js not found. Please install Node.js."
    exit 1
fi

# Check if npm dependencies are installed for frontend
if [ ! -d "Frontend/node_modules" ]; then
    echo "📦 Installing frontend dependencies..."
    cd Frontend && npm install && cd ..
fi

# Start both backend and frontend concurrently
echo "🔧 Starting development servers..."
echo "   - API will be available at: http://localhost:5000 (check console for actual port)"
echo "   - Frontend will be available at: http://localhost:3000"
echo "   - Press Ctrl+C to stop both servers"
echo ""

# Use npm run dev which starts both services
npm run dev