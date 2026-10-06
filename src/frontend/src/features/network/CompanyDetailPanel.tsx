import { useEffect, useRef, useState, type FormEvent } from 'react'
import { useSalesApi } from '../../app/App'
import { useAuth } from '../../app/AuthSession'
import type { AuditLog, Currency, NetworkCompany, User } from '../../shared/api/contracts'
import { ConcurrencyNotice } from '../../shared/ui/ConcurrencyNotice'
import { AssignmentHistory } from './AssignmentHistory'
import { ContactEditor } from './ContactEditor'
import { RelationshipEditor } from './RelationshipEditor'

const tabs = ['기본정보', '관계·납품', '거래처 담당자', '당사 담당자 이력', '변경 이력'] as const
type Tab = typeof tabs[number]

export function CompanyDetailPanel({ company, users, onReload }: { company: NetworkCompany; users: User[]; onReload: (selectedId?: string) => void }) {
  const api = useSalesApi(); const { currentUser } = useAuth(); const [tab, setTab] = useState<Tab>('기본정보'); const [editing, setEditing] = useState(false); const [converting, setConverting] = useState(false); const [error, setError] = useState<string | null>(null); const [conflict, setConflict] = useState(false); const [auditLogs, setAuditLogs] = useState<AuditLog[]>([]); const [auditRevision, setAuditRevision] = useState(0); const auditGeneration = useRef(0)
  const manager = currentUser?.role === 'manager'
  const editable = Boolean(manager || currentUser?.id === company.currentOwnerId)
  const reportError = (message: string | null, isConflict = false) => { setError(message); setConflict(isConflict) }
  useEffect(() => { const generation = ++auditGeneration.current; void api.listAuditLogs({ search: company.id }).then((rows) => { if (generation === auditGeneration.current) setAuditLogs(rows.filter((row) => row.target.includes(company.id) || row.target.includes(company.companyName))) }).catch((cause: unknown) => { if (generation === auditGeneration.current) reportError(cause instanceof Error ? cause.message : '변경 이력을 불러오지 못했습니다.') }); return () => { auditGeneration.current += 1 } }, [api, auditRevision, company.id, company.companyName, company.version])

  async function saveCompany(event: FormEvent<HTMLFormElement>) { event.preventDefault(); const data = new FormData(event.currentTarget); try { reportError(null); await api.updateNetworkCompany(company.id, { companyName: String(data.get('companyName')), country: String(data.get('country')), industry: String(data.get('industry')), lifecycleType: company.lifecycleType, isOem: company.isOem, status: String(data.get('status')) as NetworkCompany['status'], expectedVersion: company.version }); setEditing(false); onReload(company.id) } catch (cause) { reportError(cause instanceof Error ? cause.message : '회사를 수정하지 못했습니다.', (cause as { status?: number }).status === 409) } }
  async function convert(event: FormEvent<HTMLFormElement>) { event.preventDefault(); const data = new FormData(event.currentTarget); try { reportError(null); await api.convertNetworkCompany(company.id, { customerCode: String(data.get('customerCode')), defaultCurrency: String(data.get('defaultCurrency')) as Currency, expectedVersion: company.version }); setConverting(false); onReload(company.id) } catch (cause) { reportError(cause instanceof Error ? cause.message : '거래처로 전환하지 못했습니다.', (cause as { status?: number }).status === 409) } }

  return <article className="company-detail" aria-label="회사 상세">
    <div className="detail-heading"><div><p>{company.isOem ? '원청' : company.lifecycleType === 'customer' ? '정식 거래처' : '잠재 협력사'}</p><h2>{company.companyName}</h2><span>{company.country} · {company.industry}</span></div><span className={`status-badge ${company.status}`}>{company.status === 'active' ? '사용' : '중지'}</span></div>
    {conflict && <ConcurrencyNotice />}{error && !conflict && <p role="alert">{error}</p>}
    <div role="tablist" aria-label="회사 상세 메뉴" className="detail-tabs">{tabs.map((label) => <button key={label} role="tab" aria-selected={tab === label} className={tab === label ? 'is-active' : ''} onClick={() => setTab(label)}>{label}</button>)}</div>
    {tab === '기본정보' && <div className="detail-body">
      <dl className="company-summary"><div><dt>회사 구분</dt><dd>{company.isOem ? '원청' : company.lifecycleType === 'customer' ? '정식 거래처' : '잠재 협력사'}</dd></div><div><dt>당사 담당자</dt><dd>{users.find((user) => user.id === company.currentOwnerId)?.name ?? '-'}</dd></div><div><dt>거래처 연결</dt><dd>{company.customerId ?? '미연결'}</dd></div><div><dt>버전</dt><dd>{company.version}</dd></div></dl>
      {editable && <div className="detail-actions"><button type="button" onClick={() => setEditing((value) => !value)}>회사 수정</button><button type="button" onClick={() => setTab('관계·납품')}>관계 연결</button></div>}
      {manager && company.lifecycleType === 'prospect' && <button type="button" className="accent-action" onClick={() => setConverting((value) => !value)}>정식 거래처 전환</button>}
      {manager && <button type="button" className="secondary-action" onClick={() => setTab('당사 담당자 이력')}>당사 담당자 변경</button>}
      {editing && <form aria-label="회사 수정" onSubmit={(event) => void saveCompany(event)}><label>회사명<input name="companyName" defaultValue={company.companyName} required /></label><label>국가<input name="country" defaultValue={company.country} required /></label><label>업종<input name="industry" defaultValue={company.industry} required /></label><label>상태<select name="status" defaultValue={company.status}><option value="active">사용</option><option value="inactive">중지</option></select></label><button type="submit">저장</button></form>}
      {converting && <form aria-label="정식 거래처 전환" onSubmit={(event) => void convert(event)}><label>거래처 코드<input name="customerCode" required /></label><label>기본 통화<select name="defaultCurrency"><option>KRW</option><option>USD</option><option>CNY</option></select></label><button type="submit">전환 완료</button></form>}
    </div>}
    {tab === '관계·납품' && <RelationshipEditor company={company} editable={editable} onError={reportError} onChanged={() => setAuditRevision((value) => value + 1)} />}
    {tab === '거래처 담당자' && <ContactEditor company={company} editable={editable} onError={reportError} onChanged={() => setAuditRevision((value) => value + 1)} />}
    {tab === '당사 담당자 이력' && <AssignmentHistory company={company} users={users} manager={manager} onChanged={() => { setAuditRevision((value) => value + 1); onReload(company.id) }} onError={reportError} />}
    {tab === '변경 이력' && <div className="detail-body"><table><thead><tr><th>일시</th><th>작업자</th><th>대상</th><th>작업</th><th>변경 전</th><th>변경 후</th></tr></thead><tbody>{auditLogs.map((row) => <tr key={row.id}><td>{row.occurredAt}</td><td>{row.actorName}</td><td>{row.target}</td><td>{row.action}</td><td>{row.before ?? '-'}</td><td>{row.after ?? '-'}</td></tr>)}</tbody></table>{auditLogs.length === 0 && <p>변경 이력이 없습니다.</p>}</div>}
  </article>
}
