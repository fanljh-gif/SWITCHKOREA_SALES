import type { CurrencySales, DashboardSummary, SalesPerformance } from '../../shared/api/contracts'
import { Money } from '../../shared/ui/Money'

function PerformanceList({ title, rows = [] }: { title: string; rows?: SalesPerformance[] }) {
  return (
    <section>
      <h2>{title}</h2>
      {rows.length === 0 ? <p>표시할 실적이 없습니다.</p> : (
        <ul>{rows.map((row) => <li key={row.id}>{row.name} <Money amount={row.krwAmount} /></li>)}</ul>
      )}
    </section>
  )
}

function CurrencyRow({ row }: { row: CurrencySales }) {
  return (
    <li>
      <strong>{row.currency}</strong>{' '}
      <Money amount={row.originalAmount} currency={row.currency} />{' '}
      {row.currency === 'KRW' ? <span>원화 동일</span> : <span>원화 환산 <Money amount={row.krwAmount} /></span>}{' '}
      <span>{row.sharePercent.toFixed(1)}%</span>
    </li>
  )
}

export function SalesBreakdown({ summary }: { summary: DashboardSummary }) {
  return (
    <div>
      <section aria-labelledby="currency-sales-title">
        <h2 id="currency-sales-title">통화별 판매 현황</h2>
        <ul>{summary.currencySales.map((row) => <CurrencyRow key={row.currency} row={row} />)}</ul>
      </section>
      <PerformanceList title="담당자별 실적" rows={summary.ownerSales} />
      <PerformanceList title="거래처별 실적" rows={summary.customerSales} />
      <PerformanceList title="품목별 실적" rows={summary.productSales} />
    </div>
  )
}
