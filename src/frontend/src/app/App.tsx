import { createContext, useContext, type ReactNode } from 'react'
import { BrowserRouter, Link, Navigate, Outlet, Route, Routes } from 'react-router-dom'

import type { SalesApi } from '../shared/api/SalesApi'
import { ChangePasswordPage } from '../features/auth/ChangePasswordPage'
import { LoginPage } from '../features/auth/LoginPage'
import { DashboardPage } from '../features/dashboard/DashboardPage'
import { AdminPage } from '../features/admin/AdminPage'
import { CustomerPage } from '../features/customers/CustomerPage'
import { ImportWizard } from '../features/imports/ImportWizard'
import { ProductPage } from '../features/products/ProductPage'
import { SettingsPage } from '../features/settings/SettingsPage'
import { ShipmentPage } from '../features/shipments/ShipmentPage'
import { NetworkPage } from '../features/network/NetworkPage'
import { ResearchPage } from '../features/research/ResearchPage'
import { AuthSessionProvider, useAuth } from './AuthSession'

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
  { path: '/dashboard', label: '대시보드', managerOnly: false },
  { path: '/customers', label: '거래처 관리', managerOnly: false },
  { path: '/catalog', label: '품목·단가 관리', managerOnly: false },
  { path: '/shipments', label: '출고·매출 관리', managerOnly: false },
  { path: '/settings', label: '환율·목표 관리', managerOnly: true },
  { path: '/network', label: '영업망 관리', managerOnly: false },
  { path: '/research', label: '잠재고객 리서치', managerOnly: false },
  { path: '/imports', label: 'Excel 가져오기', managerOnly: false },
  { path: '/admin', label: '시스템 관리', managerOnly: true },
] as const

function AppShell() {
  const { currentUser } = useAuth()
  const visibleNavigation = navigation.filter((item) => !item.managerOnly || currentUser?.role === 'manager')

  return (
    <BrowserRouter>
      <div className="app-shell">
        <aside>
          <div className="brand"><span>SK</span><strong>SWITCH KOREA</strong><small>영업관리 시스템</small></div>
          <nav aria-label="주 메뉴">{visibleNavigation.map(({ path, label }) => <Link key={path} to={path}>{label}</Link>)}</nav>
        </aside>
        <div className="workspace">
          <header><div><strong>스위치코리아</strong><small>Sales Operations</small></div><div>{currentUser?.name ?? '로그인 필요'} · {currentUser?.role === 'manager' ? '팀장' : '팀원'}</div></header>
          <main>
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              <Route path="/change-password" element={<ChangePasswordPage />} />
              <Route element={<RequireAuthentication />}>
                <Route path="/dashboard" element={<DashboardPage />} />
                <Route path="/customers" element={<CustomerPage />} />
                <Route path="/network" element={<NetworkPage />} />
                <Route path="/research" element={<ResearchPage />} />
                <Route path="/catalog" element={<ProductPage />} />
                <Route path="/shipments" element={<ShipmentPage />} />
                <Route path="/imports" element={<ImportWizard />} />
                <Route path="/settings" element={<SettingsPage />} />
                <Route path="/admin" element={<AdminPage />} />
              </Route>
              <Route path="*" element={<Navigate replace to="/dashboard" />} />
            </Routes>
          </main>
        </div>
      </div>
    </BrowserRouter>
  )
}

function RequireAuthentication() {
  const { currentUser, status } = useAuth()

  if (status === 'loading') {
    return <p role="status">세션 확인 중입니다.</p>
  }

  if (status !== 'authenticated' || !currentUser) {
    return <Navigate replace to="/login" />
  }

  return <Outlet />
}

export function App({ api }: { api: SalesApi }) {
  return (
    <SalesApiProvider api={api}>
      <AuthSessionProvider api={api}>
        <AppShell />
      </AuthSessionProvider>
    </SalesApiProvider>
  )
}
