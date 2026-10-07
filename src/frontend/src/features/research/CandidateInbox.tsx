import type { ProspectCandidate, ProspectCandidateQuery, User } from '../../shared/api/contracts'

const statusLabels = { collected: '수집됨', enriching: '정보 보강 중', reviewPending: '검토 대기', duplicateSuspected: '중복 의심', onHold: '보류', excluded: '제외', approved: '승인', converted: '전환 완료' } as const

export function CandidateInbox({ candidates, query, users, selectedId, onQueryChange, onSelect }: { candidates: ProspectCandidate[]; query: ProspectCandidateQuery; users: User[]; selectedId?: string; onQueryChange(next: ProspectCandidateQuery): void; onSelect(id: string): void }) {
  const set = (change: Partial<ProspectCandidateQuery>) => onQueryChange({ ...query, ...change })
  return <section className="candidate-inbox" aria-label="후보 검토함">
    <form aria-label="잠재고객 필터" onSubmit={(event) => event.preventDefault()}>
      <label>검색<input value={query.search ?? ''} onChange={(event) => set({ search: event.target.value || undefined })} placeholder="기업명, 도메인 또는 적용 분야" /></label>
      <label>지역<select value={query.region ?? ''} onChange={(event) => set({ region: event.target.value || undefined })}><option value="">전체 지역</option>{['한국', '유럽', '미국', '중국', '인도', '베트남', '동남아'].map((value) => <option key={value}>{value}</option>)}</select></label>
      <label>업종<select value={query.industry ?? ''} onChange={(event) => set({ industry: event.target.value || undefined })}><option value="">전체 업종</option>{['전자', '가전', '자동차 전장', '산업제어', 'IoT', '의료기기'].map((value) => <option key={value}>{value}</option>)}</select></label>
      <label>상태<select value={query.status ?? ''} onChange={(event) => set({ status: (event.target.value || undefined) as ProspectCandidateQuery['status'] })}><option value="">전체 상태</option>{Object.entries(statusLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
      <label>담당자<select value={query.ownerId ?? ''} onChange={(event) => set({ ownerId: event.target.value || undefined })}><option value="">전체 담당자</option>{users.filter((user) => user.role === 'member').map((user) => <option key={user.id} value={user.id}>{user.name}</option>)}</select></label>
    </form>
    <article><div className="panel-title"><div><h2>후보 검토함</h2><p>{candidates.length}개 기업</p></div></div>{candidates.length === 0 ? <p className="empty-state">조건에 맞는 후보가 없습니다.</p> : <div className="candidate-list">{candidates.map((candidate) => <button className={candidate.id === selectedId ? 'selected' : ''} type="button" key={candidate.id} onClick={() => onSelect(candidate.id)} aria-label={`${candidate.companyName} ${statusLabels[candidate.status]}`}><span><strong>{candidate.companyName}</strong><small>{candidate.domain}</small></span><span>{candidate.region}<small>{candidate.industry}</small></span><b>{candidate.score.total}</b><span className={`status ${candidate.status}`}>{statusLabels[candidate.status]}</span></button>)}</div>}</article>
  </section>
}
