import { createContext, useContext, type ReactNode } from 'react'
import { BrowserRouter, Link, Navigate, Route, Routes } from 'react-router-dom'

import type { SalesApi } from '../shared/api/SalesApi'
import { ChangePasswordPage } from '../features/auth/ChangePasswordPage'
import { LoginPage } from '../features/auth/LoginPage'
import { DashboardPage } from '../features/dashboard/DashboardPage'
import { AuthSessionProvider } from './AuthSession'

const SalesApiContext = createContext<SalesApi | null>(null)

export function SalesApiProvider({ api, children }: { api: SalesApi; children: ReactNode }) {
  return <SalesApiContext.Provider value={api}>{children}</SalesApiContext.Provider>
}

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
    <SalesApiProvider api={api}>
      <AuthSessionProvider api={api}>
        <BrowserRouter>
          <header>스위치코리아 (SWITCH KOREA)</header>
          <nav aria-label="주 메뉴">
            {navigation.map(({ path, label }) => <Link key={path} to={path}>{label}</Link>)}
          </nav>
          <main>
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              <Route path="/change-password" element={<ChangePasswordPage />} />
              <Route path="/dashboard" element={<DashboardPage />} />
              {navigation.filter(({ path }) => path !== '/dashboard').map(({ path, label }) => <Route key={path} path={path} element={<Page>{label}</Page>} />)}
              <Route path="*" element={<Navigate replace to="/dashboard" />} />
            </Routes>
          </main>
        </BrowserRouter>
      </AuthSessionProvider>
    </SalesApiProvider>
  )
}
