import { useEffect, useRef, useState, type FormEvent } from 'react'
import { useSalesApi } from '../../app/App'
import type { CompanyRelationship, NetworkCompany, Product } from '../../shared/api/contracts'
import { todayInSeoul } from './networkDate'

type SupplyDraft = { productId: string; applicationName: string; notes?: string }

export function RelationshipEditor({ company, editable, onError, onChanged }: { company: NetworkCompany; editable: boolean; onError: (message: string | null, conflict?: boolean) => void; onChanged: () => void }) {
  const api = useSalesApi(); const [rows, setRows] = useState<CompanyRelationship[]>([]); const [companies, setCompanies] = useState<NetworkCompany[]>([]); const [products, setProducts] = useState<Product[]>([]); const [editing, setEditing] = useState<CompanyRelationship | null>(null); const [supplies, setSupplies] = useState<SupplyDraft[]>([{ productId: '', applicationName: '' }]); const [validation, setValidation] = useState<string | null>(null); const loadGeneration = useRef(0)
  const load = () => { const generation = ++loadGeneration.current; void Promise.all([api.listCompanyRelationships(company.id), api.listNetworkCompanies(), api.listProducts()]).then(([relations, network, catalog]) => { if (generation === loadGeneration.current) { setRows(relations); setCompanies(network); setProducts(catalog) } }).catch((cause: unknown) => { if (generation === loadGeneration.current) onError(cause instanceof Error ? cause.message : '관계 정보를 불러오지 못했습니다.') }) }
  useEffect(() => { load(); return () => { loadGeneration.current += 1 } }, [api, company.id])

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const form = event.currentTarget; const data = new FormData(form); const validFrom = String(data.get('validFrom')); const validTo = String(data.get('validTo')) || null
    if (validTo && validTo < validFrom) { setValidation('종료일은 시작일보다 빠를 수 없습니다.'); return }
    setValidation(null); onError(null)
    const direction = String(data.get('direction')); const otherId = String(data.get('otherCompanyId'))
    const input = editing ? { upstreamCompanyId: String(data.get('upstreamCompanyId')), downstreamCompanyId: String(data.get('downstreamCompanyId')) } : direction === 'upstream' ? { upstreamCompanyId: company.id, downstreamCompanyId: otherId } : { upstreamCompanyId: otherId, downstreamCompanyId: company.id }
    if (supplies.some((supply) => !supply.productId || !supply.applicationName.trim())) { setValidation('모든 납품 품목과 적용 제품·프로젝트를 입력해 주세요.'); return }
    const payload = { ...input, validFrom, validTo, status: editing?.status ?? 'active' as const, notes: String(data.get('notes')) || undefined, supplies }
    try { if (editing) await api.updateNetworkRelationship(editing.id, { ...payload, expectedVersion: editing.version }); else await api.createNetworkRelationship(payload); form.reset(); setEditing(null); setSupplies([{ productId: '', applicationName: '' }]); load(); onChanged() }
    catch (cause) { onError(cause instanceof Error ? cause.message : '관계를 저장하지 못했습니다.', (cause as { status?: number }).status === 409) }
  }
  async function disable(row: CompanyRelationship) { try { onError(null); await api.disableNetworkRelationship(row.id, row.version); load(); onChanged() } catch (cause) { onError(cause instanceof Error ? cause.message : '관계를 중지하지 못했습니다.', (cause as { status?: number }).status === 409) } }
  function edit(row: CompanyRelationship) { setEditing(row); setSupplies(row.supplies.map(({ productId, applicationName, notes }) => ({ productId, applicationName, notes }))) }
  function updateSupply(index: number, changes: Partial<SupplyDraft>) { setSupplies((current) => current.map((supply, itemIndex) => itemIndex === index ? { ...supply, ...changes } : supply)) }
  const otherCompanies = companies.filter((row) => row.id !== company.id)
  return <div className="detail-body">
    {editable && <form key={editing?.id ?? 'new'} aria-label={editing ? '관계 수정' : '관계 연결'} onSubmit={(event) => void save(event)}>
      {editing ? <><label>상위 회사<select name="upstreamCompanyId" defaultValue={editing.upstreamCompanyId}>{companies.map((row) => <option key={row.id} value={row.id}>{row.companyName}</option>)}</select></label><label>하위 회사<select name="downstreamCompanyId" defaultValue={editing.downstreamCompanyId}>{companies.map((row) => <option key={row.id} value={row.id}>{row.companyName}</option>)}</select></label></> : <><label>선택 회사 역할<select name="direction"><option value="upstream">상위 회사</option><option value="downstream">하위 회사</option></select></label><label>연결 회사<select name="otherCompanyId">{otherCompanies.map((row) => <option key={row.id} value={row.id}>{row.companyName}</option>)}</select></label></>}
      <label>시작일<input name="validFrom" type="date" required defaultValue={editing?.validFrom ?? todayInSeoul()} /></label><label>종료일<input name="validTo" type="date" defaultValue={editing?.validTo ?? ''} /></label>
      <fieldset className="supply-editor"><legend>납품 품목·적용 제품</legend>{supplies.map((supply, index) => <div key={index}><label>납품 품목<select aria-label={`납품 품목 ${index + 1}`} value={supply.productId} onChange={(event) => updateSupply(index, { productId: event.target.value })}><option value="">선택</option>{products.map((row) => <option key={row.id} value={row.id}>{row.code} · {row.name}</option>)}</select></label><label>적용 제품·프로젝트<input aria-label={`적용 제품·프로젝트 ${index + 1}`} value={supply.applicationName} onChange={(event) => updateSupply(index, { applicationName: event.target.value })} /></label><label>품목 비고<input aria-label={`품목 비고 ${index + 1}`} value={supply.notes ?? ''} onChange={(event) => updateSupply(index, { notes: event.target.value })} /></label>{supplies.length > 1 && <button className="secondary-action" type="button" onClick={() => setSupplies((current) => current.filter((_, itemIndex) => itemIndex !== index))}>품목 삭제</button>}</div>)}</fieldset><button className="secondary-action" type="button" onClick={() => setSupplies((current) => [...current, { productId: '', applicationName: '' }])}>납품 품목 추가</button><label>비고<input name="notes" defaultValue={editing?.notes ?? ''} /></label><button type="submit">{editing ? '관계 수정 저장' : '관계 연결'}</button>{editing && <button className="secondary-action" type="button" onClick={() => { setEditing(null); setSupplies([{ productId: '', applicationName: '' }]) }}>취소</button>}
    </form>}
    {validation && <p role="alert">{validation}</p>}
    <table><thead><tr><th>상위 회사</th><th>하위 회사</th><th>기간</th><th>납품 내용</th><th>상태</th>{editable && <th>작업</th>}</tr></thead><tbody>{rows.map((row) => <tr key={row.id}><td>{companies.find((item) => item.id === row.upstreamCompanyId)?.companyName}</td><td>{companies.find((item) => item.id === row.downstreamCompanyId)?.companyName}</td><td>{row.validFrom} ~ {row.validTo ?? '현재'}</td><td>{row.supplies.map((supply) => `${products.find((item) => item.id === supply.productId)?.name ?? supply.productId} / ${supply.applicationName}`).join(', ')}</td><td>{row.status === 'active' ? '사용' : '중지'}</td>{editable && <td>{row.status === 'active' && <><button type="button" onClick={() => edit(row)}>수정</button><button className="secondary-action" type="button" onClick={() => void disable(row)}>사용 중지</button></>}</td>}</tr>)}</tbody></table>
  </div>
}
