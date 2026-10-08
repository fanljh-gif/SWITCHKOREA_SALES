import { FormEvent, useEffect, useState } from 'react'

import { useSalesApi } from '../../app/App'
import type { AuditLog, IncotermDto, User } from '../../shared/api/contracts'
import { PermissionGate } from '../../shared/ui/PermissionGate'

export function AdminPage() {
  const api = useSalesApi()
  const [users, setUsers] = useState<User[]>([])
  const [logs, setLogs] = useState<AuditLog[]>([])
  const [incoterms, setIncoterms] = useState<IncotermDto[]>([])
  const [showIncotermForm, setShowIncotermForm] = useState(false)
  useEffect(() => { void Promise.all([api.listUsers(), api.listAuditLogs(), api.listIncoterms()]).then(([team, auditRows, terms]) => { setUsers(team); setLogs(auditRows); setIncoterms(terms) }) }, [api])
  const replaceIncoterm = (saved: IncotermDto) => setIncoterms((items) => [...items.filter((item) => item.id !== saved.id), saved].sort((a, b) => a.sortOrder - b.sortOrder))
  const toggleIncoterm = (row: IncotermDto) => { void api.saveIncoterm({ id: row.id, code: row.code, displayName: row.displayName, status: row.status === 'active' ? 'inactive' : 'active', sortOrder: row.sortOrder, expectedVersion: row.version }).then(replaceIncoterm) }
  const createIncoterm = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); const data = new FormData(event.currentTarget)
    void api.saveIncoterm({ code: String(data.get('code')).trim().toUpperCase(), displayName: String(data.get('displayName')).trim(), sortOrder: Number(data.get('sortOrder')), status: 'active' }).then((saved) => { replaceIncoterm(saved); setShowIncotermForm(false) })
  }
  const changeSortOrder = (row: IncotermDto, sortOrder: number) => { void api.saveIncoterm({ id: row.id, code: row.code, displayName: row.displayName, status: row.status, sortOrder, expectedVersion: row.version }).then(replaceIncoterm) }

  return (
    <section>
      <h1>시스템 관리</h1>
      <PermissionGate allow="manager">
        <button type="button">사용자 생성</button>
        <h2>사용자와 역할</h2><table><thead><tr><th>성명</th><th>아이디</th><th>이메일</th><th>역할</th><th>상태</th></tr></thead><tbody>{users.map((user) => <tr key={user.id}><td>{user.name}</td><td>{user.loginId}</td><td>{user.email}</td><td>{user.role === 'manager' ? '팀장' : '팀원'}</td><td>{user.status === 'active' ? '사용' : '중지'}</td></tr>)}</tbody></table>
        <h2>Incoterms 관리</h2><button type="button" onClick={() => setShowIncotermForm((value) => !value)}>Incoterms 추가</button>
        {showIncotermForm && <form aria-label="Incoterms 추가" onSubmit={createIncoterm}><label>코드<input name="code" required maxLength={3} /></label><label>표시명<input name="displayName" required /></label><label>표시순서<input name="sortOrder" type="number" min="1" required /></label><button type="submit">저장</button></form>}
        <table><thead><tr><th>코드</th><th>표시명</th><th>순서</th><th>상태</th><th>작업</th></tr></thead><tbody>{incoterms.map((row) => <tr key={row.id}><td>{row.code}</td><td>{row.displayName}</td><td><input aria-label={`${row.code} 표시순서`} type="number" min="1" defaultValue={row.sortOrder} onBlur={(event) => { const next = Number(event.currentTarget.value); if (next !== row.sortOrder) changeSortOrder(row, next) }} /></td><td>{row.status === 'active' ? '사용' : '중지'}</td><td><button type="button" className="secondary-action" onClick={() => toggleIncoterm(row)}>{row.status === 'active' ? '비활성' : '활성'}</button></td></tr>)}</tbody></table>
        <h2>감사 이력</h2><label>이력 검색<input placeholder="대상 또는 작업" /></label><table><thead><tr><th>시각</th><th>수행자</th><th>대상</th><th>작업</th><th>변경내용</th></tr></thead><tbody>{logs.map((row) => <tr key={row.id}><td>{row.occurredAt}</td><td>{row.actorName}</td><td>{row.target}</td><td>{row.action}</td><td>{row.after}</td></tr>)}</tbody></table>
      </PermissionGate>
    </section>
  )
}
