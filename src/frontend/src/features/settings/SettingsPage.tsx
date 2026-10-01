import { useEffect, useState } from 'react'

import { useSalesApi } from '../../app/App'
import type { DailyRate, MonthlyRate, SalesTarget, User } from '../../shared/api/contracts'
import { Money } from '../../shared/ui/Money'
import { PermissionGate } from '../../shared/ui/PermissionGate'

export function SettingsPage() {
  const api = useSalesApi()
  const [rates, setRates] = useState<MonthlyRate[]>([])
  const [daily, setDaily] = useState<DailyRate[]>([])
  const [targets, setTargets] = useState<SalesTarget[]>([])
  const [users, setUsers] = useState<User[]>([])
  useEffect(() => { void Promise.all([api.listMonthlyRates('2026-10'), api.listDailyRates('2026-10-01', '2026-10-31'), api.listSalesTargets('2026-10'), api.listUsers()]).then(([monthlyRows, dailyRows, targetRows, team]) => { setRates(monthlyRows); setDaily(dailyRows); setTargets(targetRows); setUsers(team) }) }, [api])

  return (
    <section>
      <h1>환율·목표 관리</h1>
      <PermissionGate allow="manager"><button type="button">2026년 10월 설정 저장</button></PermissionGate>
      <h2>월 지정환율</h2><table><thead><tr><th>통화</th><th>지정환율</th><th>수정시각</th></tr></thead><tbody>{rates.map((row) => <tr key={row.currency}><td>{row.currency}</td><td>{row.rate.toLocaleString('ko-KR')}</td><td>{row.updatedAt}</td></tr>)}</tbody></table>
      <h2>일별 참고환율</h2><table><thead><tr><th>기준일</th><th>통화</th><th>환율</th><th>상태</th></tr></thead><tbody>{daily.map((row) => <tr key={`${row.date}-${row.currency}`}><td>{row.date}</td><td>{row.currency}</td><td>{row.rate}</td><td>{row.status === 'success' ? '정상' : '수집 실패'}</td></tr>)}</tbody></table>
      <h2>담당자별 월 목표</h2><table><thead><tr><th>담당자</th><th>목표</th></tr></thead><tbody>{targets.map((row) => <tr key={row.userId}><td>{users.find((user) => user.id === row.userId)?.name}</td><td><Money amount={row.targetKrw} /></td></tr>)}</tbody></table>
    </section>
  )
}
