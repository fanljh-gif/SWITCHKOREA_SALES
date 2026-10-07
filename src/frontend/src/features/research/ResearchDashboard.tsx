import type { ResearchSummary } from '../../shared/api/contracts'

const labels = { collected: '신규 후보', enriching: '정보 보강', reviewPending: '검토 대기', duplicateSuspected: '중복 의심', onHold: '보류', excluded: '제외', approved: '승인', converted: '전환' } as const

export function ResearchDashboard({ summary }: { summary: ResearchSummary }) {
  return <section className="research-dashboard" aria-label="리서치 현황">
    {(['collected', 'reviewPending', 'duplicateSuspected', 'approved', 'converted'] as const).map((status) => <article key={status}><span>{labels[status]}</span><strong>{summary.statusCounts[status]}</strong></article>)}
    <article className="usage-card"><span>월 API 사용량</span><strong>{summary.monthlyUsage.percent}%</strong><small>₩{summary.monthlyUsage.costKrw.toLocaleString()} / ₩{summary.monthlyUsage.limitKrw.toLocaleString()}</small><progress max={100} value={summary.monthlyUsage.percent} /></article>
  </section>
}
