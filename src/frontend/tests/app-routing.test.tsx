import '@testing-library/jest-dom/vitest'
import { render, screen, within } from '@testing-library/react'

import { App } from '../src/app/App'
import { MockSalesApi } from '../src/shared/api/MockSalesApi'

const approvedRoutes = [
  '대시보드',
  '거래처 관리',
  '품목·단가 관리',
  '출고·매출 관리',
  '견적·고객문의',
  '환율·목표 관리',
  '영업망 관리',
  '잠재고객 리서치',
  'Excel 가져오기',
  '시스템 관리',
]

it('renders all approved navigation routes', async () => {
  render(<App api={new MockSalesApi()} />)

  expect(await screen.findByRole('heading', { name: '대시보드' })).toBeVisible()
  const links = within(screen.getByRole('navigation', { name: '주 메뉴' })).getAllByRole('link')
  expect(links.map((link) => link.textContent)).toEqual(approvedRoutes)
})

it('redirects anonymous users from protected routes to login', async () => {
  window.history.pushState({}, '', '/dashboard')

  render(<App api={new MockSalesApi({ currentUser: null })} />)

  expect(await screen.findByRole('heading', { name: '로그인' })).toBeVisible()
  expect(window.location.pathname).toBe('/login')
})
