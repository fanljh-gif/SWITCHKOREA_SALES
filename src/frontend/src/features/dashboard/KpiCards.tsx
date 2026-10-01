import type { DashboardSummary } from '../../shared/api/contracts'
import { Money } from '../../shared/ui/Money'

export function KpiCards({ summary }: { summary: DashboardSummary }) {
  return (
    <section aria-label="핵심 매출 지표">
      <article><h2>오늘 매출</h2><Money amount={summary.todaySalesKrw} /></article>
      <article><h2>당월 매출</h2><Money amount={summary.monthSalesKrw} /></article>
      <article><h2>월 목표</h2>{summary.monthTargetKrw > 0 ? <Money amount={summary.monthTargetKrw} /> : <p>-</p>}</article>
      <article><h2>달성률</h2><p>{summary.monthTargetKrw > 0 ? `${summary.achievementPercent.toFixed(1)}%` : '목표 미설정'}</p></article>
    </section>
  )
}
