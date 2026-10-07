import { useEffect, useState } from 'react'
import type { ProspectCandidate, ProspectReviewInput, User } from '../../shared/api/contracts'
import { PermissionGate } from '../../shared/ui/PermissionGate'
import { DuplicateReview } from './DuplicateReview'

export function CandidateDetail({ candidate, users, busy, onAssign, onReview, onConvert }: { candidate: ProspectCandidate; users: User[]; busy: boolean; onAssign(ownerId: string): void; onReview(decision: ProspectReviewInput['decision']): void; onConvert(): void }) {
  const [ownerId, setOwnerId] = useState(candidate.ownerId ?? '')
  useEffect(() => setOwnerId(candidate.ownerId ?? ''), [candidate.id, candidate.ownerId])
  const hasPendingDuplicate = candidate.duplicateMatches.some((match) => match.decision === 'pending')
  return <article className="candidate-detail"><header><div><p>{candidate.country} · {candidate.industry}</p><h2>{candidate.companyName}</h2><a href={candidate.website} target="_blank" rel="noreferrer">{candidate.domain}</a></div><strong className="score-ring">{candidate.score.total}<small>/ 100</small></strong></header>
    <div className="candidate-facts"><span>기업규모<strong>{candidate.companySize}</strong></span><span>예상 적용<strong>{candidate.applications.join(' · ')}</strong></span><span>우선도<strong>{candidate.score.priority}</strong></span></div>
    <section className="score-breakdown" aria-label="점수 내역"><h3>점수 내역 <span>{candidate.score.total}점</span></h3><p>제품 적용 {candidate.score.productFit} · 업종 {candidate.score.industryFit} · 공급망 {candidate.score.supplyChainFit} · 지역 {candidate.score.regionFit} · 규모 {candidate.score.companyPotential} · 근거 {candidate.score.evidenceQuality}</p></section>
    {candidate.aiAssessment && <section className="ai-summary"><h3>AI 적합성 요약</h3><p>{candidate.aiAssessment.summary}</p><p><strong>주의:</strong> {candidate.aiAssessment.risks.join(' · ')}</p></section>}
    <section aria-label="조사 근거"><h3>조사 근거 {candidate.evidence.length}건</h3>{candidate.evidence.length === 0 ? <p>등록된 공개 근거가 없습니다.</p> : <ul>{candidate.evidence.map((evidence) => <li key={evidence.id}><a href={evidence.url} target="_blank" rel="noreferrer">{evidence.title}</a><span>{evidence.observedAt} · 신뢰도 {evidence.confidence}</span><p>{evidence.summary}</p></li>)}</ul>}</section>
    <PermissionGate allow="manager"><div className="owner-editor"><label>후보 담당자<select value={ownerId} onChange={(event) => setOwnerId(event.target.value)}>{users.filter((user) => user.role === 'member').map((user) => <option key={user.id} value={user.id}>{user.name}</option>)}</select></label><button type="button" disabled={busy || ownerId === candidate.ownerId} onClick={() => onAssign(ownerId)}>담당자 저장</button></div></PermissionGate>
    <DuplicateReview matches={candidate.duplicateMatches} onDecide={onReview} />
    <div className="actions"><PermissionGate allow="owner" ownerUserId={candidate.ownerId}><button type="button" className="secondary" disabled={busy} onClick={() => onReview('onHold')}>보류</button><button type="button" className="danger" disabled={busy} onClick={() => onReview('exclude')}>제외</button></PermissionGate><PermissionGate allow="manager">{candidate.status === 'approved' ? <button type="button" disabled={busy} onClick={onConvert}>영업망 잠재기업 전환</button> : candidate.status !== 'converted' && !hasPendingDuplicate && <button type="button" disabled={busy || candidate.evidence.length === 0} onClick={() => onReview('approve')}>승인</button>}</PermissionGate></div>
  </article>
}
