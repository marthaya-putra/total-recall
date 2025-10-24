import { useEffect, useRef, useState } from 'react'
import { useSearchStream } from '../hooks/useSearchStream'
import MarkdownRenderer from './MarkdownRenderer'

interface Message {
  id: string
  content: string
  role: 'user' | 'assistant'
  timestamp: Date
}

export function ChatInterface() {
  const [messages, setMessages] = useState<Array<Message>>([])
  const [input, setInput] = useState('')
  const messagesEndRef = useRef<HTMLDivElement>(null)

  const { mutate, streamData, isPending } = useSearchStream({
    onComplete: (content: string) => {
      const assistantMessage: Message = {
        id: (Date.now() + 1).toString(),
        content,
        role: 'assistant',
        timestamp: new Date(),
      }
      setMessages((prev) => [...prev, assistantMessage])
    },
    onError: (error: Error) => {
      const errorMessage: Message = {
        id: (Date.now() + 1).toString(),
        content: `Error: ${error.message}`,
        role: 'assistant',
        timestamp: new Date(),
      }
      setMessages((prev) => [...prev, errorMessage])
    },
  })

  console.log('Stream Data: ', streamData)

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }

  useEffect(() => {
    scrollToBottom()
  }, [messages, streamData])

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!input.trim() || isPending) return

    const userMessage: Message = {
      id: Date.now().toString(),
      content: input.trim(),
      role: 'user',
      timestamp: new Date(),
    }

    setMessages((prev) => [...prev, userMessage])
    mutate(input.trim())
    setInput('')
  }

  const handleReset = () => {
    setMessages([])
  }

  return (
    <div className="flex flex-col h-full bg-gray-50 mt-4">
      <div className="flex-1 overflow-y-auto px-6 py-4 space-y-4">
        {messages.length === 0 && !isPending && (
          <div className="text-center text-gray-500 min-h-48 grid content-center">
            <p className="text-lg">What would you like to recall today?</p>
          </div>
        )}

        {messages.map((message) => (
          <div key={message.id} className="flex justify-stretch">
            <div
              className={`px-4 py-3 rounded-lg ${
                message.role === 'user'
                  ? 'bg-blue-600 text-white max-w-5xl'
                  : 'bg-white border border-gray-200 text-gray-900'
              }`}
            >
              <div className="prose prose-sm max-w-none">
                {message.role === 'user' ? (
                  <p className="whitespace-pre-wrap">{message.content}</p>
                ) : (
                  <MarkdownRenderer content={message.content} />
                )}
              </div>
              <div className="text-xs opacity-70 mt-2">
                {message.timestamp.toLocaleTimeString()}
              </div>
            </div>
          </div>
        ))}

        {/* Streaming message */}
        {isPending && streamData && (
          <div className="flex">
            <div className="px-4 py-3 rounded-lg bg-white border border-gray-200 text-gray-900">
              <div className="prose prose-sm">
                <MarkdownRenderer content={streamData} />
              </div>
            </div>
          </div>
        )}

        {isPending && !streamData && (
          <div className="flex">
            <div className="px-4 py-3 rounded-lg bg-white border border-gray-200">
              <div className="flex items-center space-x-2">
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></div>
                <span className="text-sm text-gray-600">
                  Digging into your past...
                </span>
              </div>
            </div>
          </div>
        )}

        <div ref={messagesEndRef} />
      </div>

      {/* Input form */}
      <div className="bg-white border-t border-gray-200 px-6 py-4">
        <form onSubmit={handleSubmit} className="flex space-x-4">
          <input
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            className="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            disabled={isPending}
          />
          <button
            type="button"
            onClick={handleReset}
            disabled={messages.length === 0 || isPending}
            className="px-6 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors"
          >
            Reset
          </button>
          <button
            type="submit"
            disabled={!input.trim() || isPending}
            className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors"
          >
            {isPending ? 'Sending...' : 'Send'}
          </button>
        </form>
      </div>
    </div>
  )
}
