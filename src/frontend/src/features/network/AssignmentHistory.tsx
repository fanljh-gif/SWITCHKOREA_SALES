import { useEffect, useRef, useState, type FormEvent } from 'react'
import { useSalesApi } from '../../app/App'
import type { CompanyAssignment, NetworkCompany, User } from '../../shared/api/contracts'
import { todayInSeoul } from './networkDate'

export function AssignmentHistory({ company, users, manager, onChanged, onError }: { company: NetworkCompany; users: User[]; manager: boolean; onChanged: () => void; onError: (message: string | null, conflict?: boolean) => void }) {
  const api = useSalesApi(); const [rows, setRows] = useState<CompanyAssignment[]>([]); const loadGeneration = useRef(0)
  const load = () => { const generation = ++loadGeneration.current; void api.listCompanyAssignments(company.id).then((assignments) => { if (generation === loadGeneration.current) setRows(assignments) }).catch((cause: unknown) => { if (generation === loadGeneration.current) onError(cause instanceof Error ? cause.message : '담당 이력을 불러오지 못했습니다.') }) }
  useEffect(() => { load(); return () => { loadGeneration.current += 1 } }, [api, company.id])
  async function change(event: FormEvent<HTMLFormElement>) { event.preventDefault(); const data = new FormData(event.currentTarget); try { onError(null); await api.changeCompanyAssignment(company.id, { userId: String(data.get('userId')), effectiveDate: String(data.get('effectiveDate')), expectedVersion: company.version }); load(); onChanged() } catch (cause) { onError(cause instanceof Error ? cause.message : '담당자를 변경하지 못했습니다.', (cause as { status?: number }).status === 409) } }
  return <div className="detail-body">{manager && <form aria-label="당사 담당자 변경" onSubmit={(event) => void change(event)}><label>신규 담당자<select name="userId">{users.filter((user) => user.status === 'active').map((user) => <option key={user.id} value={user.id}>{user.name}</option>)}</select></label><label>변경 적용일<input name="effectiveDate" type="date" required defaultValue={todayInSeoul()} /></label><button type="submit">당사 담당자 변경</button></form>}
    <table><thead><tr><th>담당자</th><th>담당 기간</th><th>현재</th></tr></thead><tbody>{rows.map((row) => <tr key={row.id}><td>{users.find((user) => user.id === row.userId)?.name}</td><td>{row.validFrom} ~ {row.validTo ?? '현재'}</td><td>{row.isCurrent ? '현재 담당' : '이전 담당'}</td></tr>)}</tbody></table>
  </div>
}
