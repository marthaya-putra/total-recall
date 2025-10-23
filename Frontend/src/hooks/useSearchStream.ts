import { useMutation } from '@tanstack/react-query'
import { useState } from 'react'

const fetchStream = async (query: string, onChunk: (chunk: string) => void) => {
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
    if (done) break

    const chunk = decoder.decode(value, { stream: true })
    onChunk(chunk)
  }

  return true
}

interface UseSearchStreamOptions {
  onComplete?: (content: string) => void
  onError?: (error: Error) => void
}

export function useSearchStream(options: UseSearchStreamOptions = {}) {
  const [streamData, setStreamData] = useState('')

  const searchMutation = useMutation({
    mutationFn: async (query: string) => {
      setStreamData('') // reset for new search
      let accumulatedContent = ''

      try {
        await fetchStream(query, (chunk) => {
          accumulatedContent += chunk
          setStreamData(accumulatedContent)
        })

        // Call completion callback when stream ends
        if (options.onComplete) {
          options.onComplete(accumulatedContent)
        }

        return 'done'
      } catch (error) {
        // Call error callback if provided
        if (options.onError) {
          options.onError(error as Error)
        }
        throw error
      }
    },
  })

  return { ...searchMutation, streamData }
}
