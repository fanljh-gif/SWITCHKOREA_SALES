import '@testing-library/jest-dom/vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'

import { SalesApiProvider } from '../src/app/App'
import { DashboardPage } from '../src/features/dashboard/DashboardPage'
import { MockSalesApi } from '../src/shared/api/MockSalesApi'
import type { DashboardSummary } from '../src/shared/api/contracts'

const summary: DashboardSummary = {
  todaySalesKrw: 250_000,
  monthSalesKrw: 1_500_000,
  monthTargetKrw: 0,
  achievementPercent: 0,
  currencySales: [
    { currency: 'KRW', originalAmount: 600_000, krwAmount: 600_000, sharePercent: 40 },
    { currency: 'USD', originalAmount: 500, krwAmount: 675_000, sharePercent: 45 },
    { currency: 'CNY', originalAmount: 1_250, krwAmount: 225_000, sharePercent: 15 },
  ],
}

it('shows original currencies, KRW total, currency shares, and a safe zero-target state', async () => {
  const api = new MockSalesApi({ dashboardSummary: summary })
  render(
    <SalesApiProvider api={api}>
      <MemoryRouter initialEntries={['/dashboard?dateFrom=2026-10-01&dateTo=2026-10-31']}>
        <DashboardPage />
      </MemoryRouter>
    </SalesApiProvider>,
  )

  expect(await screen.findByText('₩1,500,000')).toBeVisible()
  expect(screen.getByText('목표 미설정')).toBeVisible()
  expect(screen.getByText('₩600,000')).toBeVisible()
  expect(screen.getByText('$500')).toBeVisible()
  expect(screen.getByText('CN¥1,250')).toBeVisible()
  expect(screen.getByText('40.0%')).toBeVisible()
  expect(screen.getByText('45.0%')).toBeVisible()
  expect(screen.getByText('15.0%')).toBeVisible()
})
