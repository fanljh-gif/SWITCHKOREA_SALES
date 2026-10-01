import { createContext, useContext, type ReactNode } from 'react'
import { BrowserRouter, Link, Navigate, Route, Routes } from 'react-router-dom'

import type { SalesApi } from '../shared/api/SalesApi'

const SalesApiContext = createContext<SalesApi | null>(null)

export function useSalesApi(): SalesApi {
  const api = useContext(SalesApiContext)
  if (!api) throw new Error('SalesApi provider is missing')
  return api
}

const navigation = [
  { path: '/dashboard', label: '대시보드' },
  { path: '/customers', label: '거래처 관리' },
  { path: '/catalog', label: '품목·단가 관리' },
  { path: '/shipments', label: '출고·매출 관리' },
  { path: '/imports', label: 'Excel 가져오기' },
  { path: '/settings', label: '환율·목표 관리' },
  { path: '/admin', label: '시스템 관리' },
] as const

function Page({ children }: { children: ReactNode }) {
  return <h1>{children}</h1>
}

export function App({ api }: { api: SalesApi }) {
  return (
    <SalesApiContext.Provider value={api}>
      <BrowserRouter>
        <header>스위치코리아 (SWITCH KOREA)</header>
        <nav aria-label="주 메뉴">
          {navigation.map(({ path, label }) => <Link key={path} to={path}>{label}</Link>)}
        </nav>
        <main>
          <Routes>
            <Route path="/login" element={<Page>로그인</Page>} />
            {navigation.map(({ path, label }) => <Route key={path} path={path} element={<Page>{label}</Page>} />)}
            <Route path="*" element={<Navigate replace to="/dashboard" />} />
          </Routes>
        </main>
      </BrowserRouter>
    </SalesApiContext.Provider>
  )
}
