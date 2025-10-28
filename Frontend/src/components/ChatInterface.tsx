import { useEffect, useRef, useState } from 'react'
import { useSearchStream } from '../hooks/useSearchStream'
import MarkdownRenderer from './MarkdownRenderer'

export function ChatInterface() {
  const [input, setInput] = useState('')
  const [error, setError] = useState('')

  const messagesEndRef = useRef<HTMLDivElement>(null)

  const {
    sendMessage,
    messages: messages,
    reset,
    isPending,
  } = useSearchStream({
    onError: (streamError: Error) => {
      setError(streamError.message)
    },
  })

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }

  useEffect(() => {
    scrollToBottom()
  }, [messages, messages])

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!input.trim() || isPending) return

    sendMessage(input)
    setInput('')
  }

  const handleReset = () => {
    reset()
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
            {message.content && (
              <div
                className={`px-4 py-3 rounded-lg ${
                  message.role === 'user'
                    ? 'bg-blue-600 text-white max-w-5xl'
                    : 'bg-white border border-gray-200 text-gray-900'
                }`}
              >
                <div className="max-w-none">
                  {message.role === 'user' ? (
                    <p className="whitespace-pre-wrap">{message.content}</p>
                  ) : (
                    <div className="prose prose-sm">
                      <MarkdownRenderer content={message.content} />
                    </div>
                  )}
                </div>

                <div className="text-xs opacity-70 mt-2">
                  {message.timestamp.toLocaleTimeString()}
                </div>
              </div>
            )}
          </div>
        ))}

        {isPending && (
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
      <span className="text-red-500">{error}</span>
    </div>
  )
}
