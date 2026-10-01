import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'

import { useSalesApi } from '../../app/App'
import type { DashboardQuery, DashboardSummary } from '../../shared/api/contracts'
import { DashboardFilters } from './DashboardFilters'
import { KpiCards } from './KpiCards'
import { SalesBreakdown } from './SalesBreakdown'

const currentMonth = new Date().toISOString().slice(0, 7)

export function DashboardPage() {
  const api = useSalesApi()
  const [searchParams] = useSearchParams()
  const [summary, setSummary] = useState<DashboardSummary | null>(null)
  const [error, setError] = useState<string | null>(null)
  const query = useMemo<DashboardQuery>(() => ({
    dateFrom: searchParams.get('dateFrom') ?? `${currentMonth}-01`,
    dateTo: searchParams.get('dateTo') ?? `${currentMonth}-31`,
    ownerId: searchParams.get('ownerId') || undefined,
    customerId: searchParams.get('customerId') || undefined,
    productId: searchParams.get('productId') || undefined,
  }), [searchParams])

  useEffect(() => {
    let active = true
    setError(null)
    void api.getDashboardSummary(query).then((result) => {
      if (active) setSummary(result)
    }).catch((cause: unknown) => {
      if (active) setError(cause instanceof Error ? cause.message : '대시보드를 불러오지 못했습니다.')
    })
    return () => { active = false }
  }, [api, query])

  return (
    <section>
      <h1>대시보드</h1>
      <DashboardFilters />
      {error && <p role="alert">{error}</p>}
      {!summary && !error && <p>매출 현황을 불러오는 중입니다.</p>}
      {summary && <><KpiCards summary={summary} /><SalesBreakdown summary={summary} /></>}
    </section>
  )
}
