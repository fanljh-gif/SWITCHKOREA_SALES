import { useCallback, useEffect, useMemo, useRef, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useSalesApi } from '../../app/App'
import { useAuth } from '../../app/AuthSession'
import type { NetworkCompany, NetworkQuery, NetworkTreeNode, User } from '../../shared/api/contracts'
import { CompanyDetailPanel } from './CompanyDetailPanel'
import { NetworkFilters } from './NetworkFilters'
import { NetworkTree } from './NetworkTree'
import './network.css'

export function NetworkPage() {
  const api = useSalesApi(); const { currentUser } = useAuth(); const [params, setParams] = useSearchParams(); const [companies, setCompanies] = useState<NetworkCompany[]>([]); const [users, setUsers] = useState<User[]>([]); const [tree, setTree] = useState<NetworkTreeNode[]>([]); const [selected, setSelected] = useState<NetworkCompany | null>(null); const [loading, setLoading] = useState(true); const [creating, setCreating] = useState(false); const [error, setError] = useState<string | null>(null)
  const requestGeneration = useRef(0)
  const oems = useMemo(() => companies.filter((company) => company.isOem), [companies])
  const query = useMemo<NetworkQuery>(() => ({ rootCompanyId: params.get('root') ?? undefined, lifecycleType: (params.get('type') || undefined) as NetworkQuery['lifecycleType'], ownerId: params.get('owner') ?? undefined, status: (params.get('status') || undefined) as NetworkQuery['status'], search: params.get('search') ?? undefined }), [params])

  const load = useCallback(async (selectedId?: string) => {
    const generation = ++requestGeneration.current
    setLoading(true)
    try {
      const [network, team] = await Promise.all([api.listNetworkCompanies(), api.listUsers()])
      const rootCompanyId = query.rootCompanyId ?? network.find((company) => company.isOem)?.id
      const nodes = await api.getNetworkTree({ ...query, rootCompanyId })
      const visibleIds = new Set<string>(); const collect = (items: NetworkTreeNode[]) => items.forEach((node) => { visibleIds.add(node.company.id); collect(node.children) }); collect(nodes)
      const requestedId = selectedId ?? params.get('company') ?? undefined
      const id = requestedId && (selectedId || visibleIds.has(requestedId)) ? requestedId : nodes[0]?.company.id
      const selectedCompany = id ? await api.getNetworkCompany(id) : null
      if (generation !== requestGeneration.current) return
      setCompanies(network); setUsers(team); setTree(nodes); setSelected(selectedCompany); setError(null)
      const normalized = new URLSearchParams(params)
      if (rootCompanyId) normalized.set('root', rootCompanyId)
      if (id) normalized.set('company', id); else normalized.delete('company')
      if (normalized.toString() !== params.toString()) setParams(normalized, { replace: true })
    } catch (cause) {
      if (generation === requestGeneration.current) setError(cause instanceof Error ? cause.message : '영업망을 불러오지 못했습니다.')
    } finally {
      if (generation === requestGeneration.current) setLoading(false)
    }
  }, [api, params, query, setParams])

  useEffect(() => { void load(); return () => { requestGeneration.current++ } }, [load])

  function changeQuery(next: NetworkQuery) {
    requestGeneration.current++
    const value = new URLSearchParams()
    if (next.rootCompanyId) value.set('root', next.rootCompanyId); if (next.lifecycleType) value.set('type', next.lifecycleType); if (next.ownerId) value.set('owner', next.ownerId); if (next.status) value.set('status', next.status); if (next.search) value.set('search', next.search); if (selected) value.set('company', selected.id)
    setParams(value)
  }
  function selectCompany(id: string) { requestGeneration.current++; const value = new URLSearchParams(params); value.set('company', id); setParams(value) }
  async function createCompany(event: FormEvent<HTMLFormElement>) { event.preventDefault(); const form = event.currentTarget; const data = new FormData(form); const kind = currentUser?.role === 'manager' ? String(data.get('kind')) : 'prospect'; try { const row = await api.createNetworkCompany({ companyName: String(data.get('companyName')), country: String(data.get('country')), industry: String(data.get('industry')), lifecycleType: kind === 'oem' ? 'customer' : 'prospect', isOem: kind === 'oem', status: 'active', ownerId: currentUser?.role === 'manager' ? String(data.get('ownerId')) : currentUser?.id }); setCreating(false); setError(null); await load(row.id) } catch (cause) { setError(cause instanceof Error ? cause.message : '회사를 등록하지 못했습니다.') } }

  const effectiveQuery = { ...query, rootCompanyId: query.rootCompanyId ?? oems[0]?.id }
  return <section className="network-page"><div className="page-heading"><div><p className="eyebrow">SALES NETWORK</p><h1>영업망 관리</h1><p>원청부터 협력사까지 공급 관계와 담당자 이력을 한 곳에서 관리합니다.</p></div><button type="button" onClick={() => setCreating((value) => !value)}>회사 등록</button></div>
    {error && <p role="alert">{error}</p>}
    {creating && <form aria-label="영업망 회사 등록" onSubmit={(event) => void createCompany(event)}><label>회사명<input name="companyName" required /></label><label>국가<input name="country" required /></label><label>업종<input name="industry" required /></label>{currentUser?.role === 'manager' ? <><label>등록 구분<select name="kind"><option value="prospect">잠재 협력사</option><option value="oem">원청</option></select></label><label>당사 담당자<select name="ownerId">{users.filter((user) => user.status === 'active').map((user) => <option key={user.id} value={user.id}>{user.name}</option>)}</select></label></> : <p>잠재 협력사로 등록되며 현재 사용자가 담당자가 됩니다.</p>}<button type="submit">등록 완료</button></form>}
    <NetworkFilters query={effectiveQuery} oems={oems} users={users} onChange={changeQuery} />
    {loading && !selected ? <p role="status">영업망을 불러오는 중입니다.</p> : <div className="network-layout"><article className="network-browser"><div className="panel-heading"><div><h2>협력사 계보도</h2><span>{new Set(tree.flatMap(function flatten(node): string[] { return [node.company.id, ...node.children.flatMap(flatten)] })).size}개 회사</span></div></div><NetworkTree nodes={tree} selectedId={selected?.id} onSelect={selectCompany} /></article>{selected && <CompanyDetailPanel key={selected.id} company={selected} users={users} onReload={(id) => void load(id)} />}</div>}
  </section>
}
