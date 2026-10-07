import { useCallback, useEffect, useRef, useState } from 'react'
import { useSalesApi } from '../../app/App'
import { useAuth } from '../../app/AuthSession'
import { SalesApiError } from '../../shared/api/HttpSalesApi'
import type { ProspectCandidate, ProspectCandidateQuery, ProspectReviewInput, ResearchSummary, User } from '../../shared/api/contracts'
import { PermissionGate } from '../../shared/ui/PermissionGate'
import { CandidateDetail } from './CandidateDetail'
import { CandidateInbox } from './CandidateInbox'
import { ProspectImportPanel } from './ProspectImportPanel'
import { ResearchDashboard } from './ResearchDashboard'
import { ResearchRunPanel } from './ResearchRunPanel'
import { TargetProfileEditor } from './TargetProfileEditor'
import './research.css'

export function ResearchPage() {
  const api = useSalesApi(); const { currentUser } = useAuth()
  const [summary, setSummary] = useState<ResearchSummary | null>(null); const [users, setUsers] = useState<User[]>([]); const [candidates, setCandidates] = useState<ProspectCandidate[]>([]); const [selected, setSelected] = useState<ProspectCandidate | null>(null); const [query, setQuery] = useState<ProspectCandidateQuery>({}); const [loading, setLoading] = useState(true); const [detailLoading, setDetailLoading] = useState(false); const [busy, setBusy] = useState(false); const [error, setError] = useState<string | null>(null); const [conflict, setConflict] = useState(false)
  const listGeneration = useRef(0); const detailGeneration = useRef(0)

  const showError = (cause: unknown, fallback: string) => { setConflict(cause instanceof SalesApiError && cause.status === 409); setError(cause instanceof Error ? cause.message : fallback) }
  const loadDetail = useCallback(async (id: string) => { const generation = ++detailGeneration.current; setDetailLoading(true); try { const row = await api.getProspectCandidate(id); if (generation === detailGeneration.current) { setSelected(row); setError(null); setConflict(false) } } catch (cause) { if (generation === detailGeneration.current) showError(cause, '후보 상세를 불러오지 못했습니다.') } finally { if (generation === detailGeneration.current) setDetailLoading(false) } }, [api])
  const loadCandidates = useCallback(async (nextQuery: ProspectCandidateQuery, preferredId?: string) => { const generation = ++listGeneration.current; setLoading(true); try { const rows = await api.listProspectCandidates(nextQuery); if (generation !== listGeneration.current) return; setCandidates(rows); const id = rows.some((row) => row.id === preferredId) ? preferredId : rows[0]?.id; if (id) await loadDetail(id); else setSelected(null); setError(null) } catch (cause) { if (generation === listGeneration.current) showError(cause, '후보 목록을 불러오지 못했습니다.') } finally { if (generation === listGeneration.current) setLoading(false) } }, [api, loadDetail])

  useEffect(() => { let active = true; void Promise.all([api.getResearchSummary(), api.listUsers()]).then(([nextSummary, nextUsers]) => { if (!active) return; setSummary(nextSummary); setUsers(nextUsers) }).catch((cause) => { if (active) showError(cause, '리서치 현황을 불러오지 못했습니다.') }); return () => { active = false } }, [api])
  useEffect(() => { void loadCandidates(query, selected?.id) }, [loadCandidates, query])

  const mutate = async (action: () => Promise<ProspectCandidate>) => { setBusy(true); try { const row = await action(); setSelected(row); setCandidates((items) => items.map((item) => item.id === row.id ? row : item)); setError(null); setConflict(false) } catch (cause) { showError(cause, '후보를 변경하지 못했습니다.') } finally { setBusy(false) } }
  const review = (decision: ProspectReviewInput['decision']) => selected && void mutate(() => api.reviewProspectCandidate(selected.id, { decision, expectedVersion: selected.version }))

  return <section className="research-page"><div className="research-heading"><div><p className="eyebrow">GLOBAL B2B PROSPECTING</p><h1>잠재고객 리서치</h1><p>공개 근거를 바탕으로 글로벌 후보를 발굴하고 검토합니다.</p></div><PermissionGate allow="manager"><button type="button" className="secondary">Excel 후보 업로드</button><button type="button" onClick={() => void api.startResearchRun('manual')}>+ 즉시 조사</button></PermissionGate></div>
    {error && <p role="alert">{conflict ? '다른 사용자가 먼저 수정했습니다. 최신 내용을 다시 확인해 주세요.' : error}</p>}
    {summary?.partialError && <p role="alert" className="partial-warning">{summary.partialError} 정상 수집 후보는 계속 검토할 수 있습니다.</p>}
    {!summary ? <p role="status">리서치 현황을 불러오는 중입니다.</p> : <><ResearchDashboard summary={summary} /><TargetProfileEditor profile={summary.targetProfile} /><div className="research-main"><CandidateInbox candidates={candidates} query={query} users={users} selectedId={selected?.id} onQueryChange={setQuery} onSelect={(id) => void loadDetail(id)} />{detailLoading ? <article><p role="status">후보 상세를 불러오는 중입니다.</p></article> : selected && <CandidateDetail candidate={selected} users={users} busy={busy} onAssign={(ownerId) => void mutate(() => api.assignProspectCandidate(selected.id, { ownerId, expectedVersion: selected.version }))} onReview={review} onConvert={() => void mutate(() => api.convertProspectCandidate(selected.id, selected.version))} />}</div><div className="research-secondary"><ResearchRunPanel runs={summary.recentRuns} /><ProspectImportPanel /></div></>}
    {loading && <p role="status" className="loading-overlay">후보 목록을 불러오는 중입니다.</p>}
    {currentUser && <span className="sr-only">현재 리서치 사용자 {currentUser.name}</span>}
  </section>
}
