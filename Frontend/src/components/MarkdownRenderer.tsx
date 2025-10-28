import { forwardRef, useEffect, useLayoutEffect, useRef, useState } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import hljs from 'highlight.js'
import '@fontsource/fira-code'
import 'highlight.js/styles/github-dark.css'

interface MarkdownRendererProps {
  content: string
}

export default function MarkdownRenderer({ content }: MarkdownRendererProps) {
  useLayoutEffect(() => {
    hljs.highlightAll()
  })

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
              return <CodeBlock language={'javascript'} code={code} />
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

const CodeBlock = forwardRef<HTMLElement, { language: string; code: string }>(
  ({ language, code }, ref) => {
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
          <code ref={ref} className={`language-${language}`}>
            {code}
          </code>
        </pre>
      </div>
    )
  },
)
