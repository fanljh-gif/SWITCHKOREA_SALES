import { useEffect, useState, type FormEvent } from 'react'

import { useSalesApi } from '../../app/App'
import type { Customer, Currency, User } from '../../shared/api/contracts'
import { ConcurrencyNotice } from '../../shared/ui/ConcurrencyNotice'
import { PermissionGate } from '../../shared/ui/PermissionGate'

export function CustomerPage() {
  const api = useSalesApi()
  const [rows, setRows] = useState<Customer[]>([])
  const [users, setUsers] = useState<User[]>([])
  const [conflict, setConflict] = useState(false)

  useEffect(() => { void Promise.all([api.listCustomers(), api.listUsers()]).then(([customers, team]) => { setRows(customers); setUsers(team) }) }, [api])

  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = event.currentTarget
    const data = new FormData(form)
    try {
      const row = await api.createCustomer({
        code: String(data.get('code')),
        companyName: String(data.get('companyName')),
        country: String(data.get('country')),
        defaultCurrency: String(data.get('currency')) as Currency,
        ownerId: String(data.get('ownerId')),
        status: 'active',
      })
      setRows((current) => [...current, row])
      form.reset()
    } catch { setConflict(true) }
  }

  return (
    <section>
      <h1>거래처 관리</h1>
      {conflict && <ConcurrencyNotice />}
      <form onSubmit={(event) => void create(event)}>
        <label>거래처 코드<input name="code" required /></label>
        <label>회사명<input name="companyName" required /></label>
        <label>국가<input name="country" required /></label>
        <label>기본 통화<select name="currency"><option>KRW</option><option>USD</option><option>CNY</option></select></label>
        <label>담당자<select name="ownerId">{users.map((user) => <option key={user.id} value={user.id}>{user.name}</option>)}</select></label>
        <button type="submit">거래처 등록</button>
      </form>
      <table><thead><tr><th>코드</th><th>회사명</th><th>국가</th><th>통화</th><th>담당자</th><th>상태</th><th>작업</th></tr></thead>
        <tbody>{rows.map((row) => <tr key={row.id}><td>{row.code}</td><td>{row.companyName}</td><td>{row.country}</td><td>{row.defaultCurrency}</td><td>{users.find((user) => user.id === row.ownerId)?.name}</td><td>{row.status === 'active' ? '사용' : '중지'}</td><td><PermissionGate allow="owner" ownerUserId={row.ownerId}><button type="button">수정</button></PermissionGate></td></tr>)}</tbody>
      </table>
    </section>
  )
}
