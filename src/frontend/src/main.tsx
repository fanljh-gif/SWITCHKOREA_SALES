import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'

import { App } from './app/App'
import { MockSalesApi } from './shared/api/MockSalesApi'
import { HttpSalesApi } from './shared/api/HttpSalesApi'
import './app/styles.css'

const root = document.getElementById('root')
if (!root) throw new Error('Root element is missing')

const useMockApi = (import.meta as ImportMeta & { env?: Record<string, string> }).env?.VITE_USE_MOCK_API === 'true'
const api = useMockApi ? new MockSalesApi() : new HttpSalesApi()

createRoot(root).render(
  <StrictMode>
    <App api={api} />
  </StrictMode>,
)
