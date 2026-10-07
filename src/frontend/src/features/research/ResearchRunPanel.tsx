import type { ResearchRun } from '../../shared/api/contracts'

export function ResearchRunPanel({ runs }: { runs: ResearchRun[] }) {
  return <article className="research-runs"><h2>리서치 실행 이력</h2><p>예약·수동·Excel 실행과 오류를 확인합니다.</p><ul>{runs.map((run) => <li key={run.id}><span className="run-kind">{run.kind}</span><div><strong>{new Date(run.startedAt).toLocaleString('ko-KR')}</strong><small>발견 {run.discoveredCount} · 보강 {run.enrichedCount} · 실패 {run.failedCount}</small></div><span>{run.apiCallCount}회<br />₩{run.costKrw.toLocaleString()}</span></li>)}</ul></article>
}
