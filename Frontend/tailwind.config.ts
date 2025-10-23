import typography from '@tailwindcss/typography'
import type { Config } from 'tailwindcss'

const config: Config = {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  darkMode: 'media', // or 'class'
  theme: {
    extend: {
      colors: {
        brand: {
          DEFAULT: '#2563eb',
          light: '#3b82f6',
          dark: '#1d4ed8',
        },
        surface: {
          light: '#ffffff',
          dark: '#0d1117',
        },
      },
      fontFamily: {
        mono: ['Fira Code', 'ui-monospace', 'SFMono-Regular'],
      },
      typography: {
        DEFAULT: {
          css: {
            color: '#1f2937',
            code: {
              backgroundColor: '#f3f4f6',
              borderRadius: '0.25rem',
              padding: '0.2em 0.4em',
              fontWeight: '500',
              fontSize: '0.875em',
            },
            'code::before': { content: 'none' },
            'code::after': { content: 'none' },
          },
        },
        invert: {
          css: {
            color: '#e5e7eb',
            code: {
              backgroundColor: '#1f2937',
              color: '#e5e7eb',
            },
          },
        },
      },
    },
  },
  plugins: [typography],
}

export default config
