import { useSearchParams } from 'react-router-dom'

const currentMonth = new Date().toISOString().slice(0, 7)

export function DashboardFilters() {
  const [searchParams, setSearchParams] = useSearchParams()

  function update(name: string, value: string) {
    const next = new URLSearchParams(searchParams)
    if (value) next.set(name, value)
    else next.delete(name)
    setSearchParams(next, { replace: true })
  }

  return (
    <form aria-label="대시보드 필터">
      <label>시작일<input type="date" value={searchParams.get('dateFrom') ?? `${currentMonth}-01`} onChange={(event) => update('dateFrom', event.target.value)} /></label>
      <label>종료일<input type="date" value={searchParams.get('dateTo') ?? `${currentMonth}-31`} onChange={(event) => update('dateTo', event.target.value)} /></label>
      <label>담당자<input value={searchParams.get('ownerId') ?? ''} onChange={(event) => update('ownerId', event.target.value)} /></label>
      <label>거래처<input value={searchParams.get('customerId') ?? ''} onChange={(event) => update('customerId', event.target.value)} /></label>
      <label>품목<input value={searchParams.get('productId') ?? ''} onChange={(event) => update('productId', event.target.value)} /></label>
    </form>
  )
}
