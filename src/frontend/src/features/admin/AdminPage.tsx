import { useEffect, useState } from 'react'

import { useSalesApi } from '../../app/App'
import type { AuditLog, User } from '../../shared/api/contracts'
import { PermissionGate } from '../../shared/ui/PermissionGate'

export function AdminPage() {
  const api = useSalesApi()
  const [users, setUsers] = useState<User[]>([])
  const [logs, setLogs] = useState<AuditLog[]>([])
  useEffect(() => { void Promise.all([api.listUsers(), api.listAuditLogs()]).then(([team, auditRows]) => { setUsers(team); setLogs(auditRows) }) }, [api])

  return (
    <section>
      <h1>시스템 관리</h1>
      <PermissionGate allow="manager">
        <button type="button">사용자 생성</button>
        <h2>사용자와 역할</h2><table><thead><tr><th>성명</th><th>아이디</th><th>이메일</th><th>역할</th><th>상태</th></tr></thead><tbody>{users.map((user) => <tr key={user.id}><td>{user.name}</td><td>{user.loginId}</td><td>{user.email}</td><td>{user.role === 'manager' ? '팀장' : '팀원'}</td><td>{user.status === 'active' ? '사용' : '중지'}</td></tr>)}</tbody></table>
        <h2>감사 이력</h2><label>이력 검색<input placeholder="대상 또는 작업" /></label><table><thead><tr><th>시각</th><th>수행자</th><th>대상</th><th>작업</th><th>변경내용</th></tr></thead><tbody>{logs.map((row) => <tr key={row.id}><td>{row.occurredAt}</td><td>{row.actorName}</td><td>{row.target}</td><td>{row.action}</td><td>{row.after}</td></tr>)}</tbody></table>
      </PermissionGate>
    </section>
  )
}
