import '@testing-library/jest-dom/vitest'
import { render, screen } from '@testing-library/react'

import { App } from '../src/app/App'
import { MockSalesApi } from '../src/shared/api/MockSalesApi'

const approvedRoutes = [
  '대시보드',
  '거래처 관리',
  '영업망 관리',
  '품목·단가 관리',
  '출고·매출 관리',
  'Excel 가져오기',
  '환율·목표 관리',
  '시스템 관리',
]

it('renders all approved navigation routes', async () => {
  render(<App api={new MockSalesApi()} />)

  expect(await screen.findByRole('heading', { name: '대시보드' })).toBeVisible()
  for (const routeName of approvedRoutes) {
    expect(screen.getByRole('link', { name: routeName })).toBeVisible()
  }
})

it('redirects anonymous users from protected routes to login', async () => {
  window.history.pushState({}, '', '/dashboard')

  render(<App api={new MockSalesApi({ currentUser: null })} />)

  expect(await screen.findByRole('heading', { name: '로그인' })).toBeVisible()
  expect(window.location.pathname).toBe('/login')
})
