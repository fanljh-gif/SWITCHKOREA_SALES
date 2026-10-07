import type { DuplicateMatch } from '../../shared/api/contracts'

export function DuplicateReview({ matches, onDecide }: { matches: DuplicateMatch[]; onDecide(decision: 'distinct' | 'duplicate'): void }) {
  const pending = matches.find((match) => match.decision === 'pending')
  if (!pending) return null
  return <section className="duplicate-review" role="region" aria-label="중복 후보 비교"><h3>중복 후보 비교</h3><p>자동 병합하지 않고 담당자가 판정합니다.</p><div><span>신규 후보</span><b>현재 검토 기업</b><span>기존 영업망</span><b>{pending.targetName}</b></div><p>{pending.reasons.join(' · ')} · 유사도 {Math.round(pending.similarity * 100)}%</p><div className="actions"><button type="button" className="secondary" onClick={() => onDecide('duplicate')}>기존 기업과 중복</button><button type="button" onClick={() => onDecide('distinct')}>별도 신규 후보</button></div></section>
}
