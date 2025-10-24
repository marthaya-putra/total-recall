import { useEffect, useState } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
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
        components={{
          code(props: any) {
            const { inline, className, children, ...rest } = props
            const match = /language-(\w+)/.exec(className || '')
            const language = match ? match[1] : ''

            // Convert children to string properly
            let code = ''
            if (typeof children === 'string') {
              code = children
            } else if (Array.isArray(children)) {
              code = children
                .map((child) => (typeof child === 'string' ? child : ''))
                .join('')
            } else {
              code = String(children)
            }

            code = code.replace(/\n$/, '')

            if (!inline && language) {
              return <CodeBlock language={language} code={code} />
            }

            return (
              <code className={className} {...rest}>
                {children}
              </code>
            )
          },
        }}
      >
        {content}
      </ReactMarkdown>
    </div>
  )
}

function CodeBlock({ language, code }: { language: string; code: string }) {
  const [copied, setCopied] = useState(false)

  const handleCopy = async () => {
    await navigator.clipboard.writeText(code)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <div className="relative group my-4">
      <div className="absolute right-2 top-2 opacity-0 group-hover:opacity-100 transition">
        <button
          onClick={handleCopy}
          className={`text-xs px-2 py-1 rounded transition-colors cursor-pointer ${
            copied
              ? 'bg-green-600 text-white'
              : 'bg-gray-700 text-gray-100 hover:bg-gray-600'
          }`}
        >
          {copied ? 'Copied!' : 'Copy'}
        </button>
      </div>
      <pre className="bg-[#0d1117] rounded-xl p-4 overflow-x-auto font-mono text-sm leading-6">
        <code className={`language-${language}`}>{code}</code>
      </pre>
    </div>
  )
}
