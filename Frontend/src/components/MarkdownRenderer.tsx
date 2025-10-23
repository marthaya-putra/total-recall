import { useEffect } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import rehypeHighlight from 'rehype-highlight'
import hljs from 'highlight.js'
import 'highlight.js/styles/github-dark.css'
import '@fontsource/fira-code'

interface MarkdownRendererProps {
  content: string
}

export default function MarkdownRenderer({ content }: MarkdownRendererProps) {
  // ensure highlighting runs on re-render (for streaming updates)
  useEffect(() => {
    hljs.highlightAll()
  }, [content])

  return (
    <div className="prose prose-slate max-w-none dark:prose-invert">
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        rehypePlugins={[rehypeHighlight]}
      >
        {content}
      </ReactMarkdown>
    </div>
  )
}

function CodeBlock({ language, code }: { language: string; code: string }) {
  const handleCopy = async () => {
    await navigator.clipboard.writeText(code)
  }

  return (
    <div className="relative group my-4">
      <div className="absolute right-2 top-2 opacity-0 group-hover:opacity-100 transition">
        <button
          onClick={handleCopy}
          className="text-xs bg-gray-700 text-gray-100 px-2 py-1 rounded hover:bg-gray-600"
        >
          Copy
        </button>
      </div>
      <pre className="bg-[#0d1117] rounded-xl p-4 overflow-x-auto font-mono text-sm leading-6">
        <code className={`language-${language}`}>{code}</code>
      </pre>
    </div>
  )
}
