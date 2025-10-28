import { useMutation } from '@tanstack/react-query'
import { useState } from 'react'

const fetchStream = async (
  query: string,
  onChunk: (chunk: string) => void,
  onDone?: () => void,
) => {
  const response = await fetch('http://localhost:5291/api/search', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ query }),
  })

  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`)
  }

  if (!response.body) throw new Error('No readable stream')

  const reader = response.body.getReader()
  const decoder = new TextDecoder('utf-8')

  // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
  while (true) {
    const { done, value } = await reader.read()
    if (done) {
      onDone?.()
      break
    }

    const chunk = decoder.decode(value, { stream: true })
    onChunk(chunk)
  }

  return true
}

interface UseSearchStreamOptions {
  onComplete?: () => void
  onError?: (error: Error) => void
}

interface Message {
  id: string
  content: string
  role: 'user' | 'assistant'
  timestamp: Date
}

export function useSearchStream(options: UseSearchStreamOptions = {}) {
  const [messages, setMessages] = useState<Array<Message>>([])

  const searchMutation = useMutation({
    mutationFn: async (query: string) => {
      try {
        await fetchStream(
          query,
          (chunk) => {
            const lastAssistantMessage = messages.at(-1)
            lastAssistantMessage!.content += chunk
            setMessages((prev) => {
              return [
                ...prev.filter((p) => p.id !== lastAssistantMessage!.id),
                lastAssistantMessage!,
              ]
            })
          },
          options.onComplete,
        )
      } catch (error) {
        if (options.onError) {
          options.onError(error as Error)
        }
        throw error
      }
    },
  })

  const sendMessage = (message: string) => {
    const newMessage: Message = {
      id: crypto.randomUUID(),
      content: message,
      role: 'user',
      timestamp: new Date(),
    }

    const newAssistantMessage = {
      id: crypto.randomUUID(),
      content: '',
      role: 'assistant',
      timestamp: new Date(),
    } satisfies Message

    setMessages((prev) => [...prev, newMessage, newAssistantMessage])

    searchMutation.mutate(message)
  }

  return {
    isPending: searchMutation.isPending,
    messages,
    sendMessage,
    reset: () => setMessages([]),
  }
}
