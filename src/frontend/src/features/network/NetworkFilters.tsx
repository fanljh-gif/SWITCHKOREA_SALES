import type { NetworkCompany, NetworkQuery, User } from '../../shared/api/contracts'

export function NetworkFilters({ query, oems, users, onChange }: { query: NetworkQuery; oems: NetworkCompany[]; users: User[]; onChange: (next: NetworkQuery) => void }) {
  const update = (changes: Partial<NetworkQuery>) => onChange({ ...query, ...changes })
  return (
    <form className="network-filters" aria-label="영업망 필터" onSubmit={(event) => event.preventDefault()}>
      <label>원청 선택<select value={query.rootCompanyId ?? ''} onChange={(event) => update({ rootCompanyId: event.target.value || undefined })}>{oems.map((company) => <option key={company.id} value={company.id}>{company.companyName}</option>)}</select></label>
      <label>회사 구분<select value={query.lifecycleType ?? ''} onChange={(event) => update({ lifecycleType: (event.target.value || undefined) as NetworkQuery['lifecycleType'] })}><option value="">전체</option><option value="oem">원청</option><option value="customer">정식 거래처</option><option value="prospect">잠재 협력사</option></select></label>
      <label>당사 담당자<select value={query.ownerId ?? ''} onChange={(event) => update({ ownerId: event.target.value || undefined })}><option value="">전체</option>{users.filter((user) => user.status === 'active').map((user) => <option key={user.id} value={user.id}>{user.name}</option>)}</select></label>
      <label>상태<select value={query.status ?? ''} onChange={(event) => update({ status: (event.target.value || undefined) as NetworkQuery['status'] })}><option value="">전체</option><option value="active">사용</option><option value="inactive">중지</option></select></label>
      <label>회사 검색<input value={query.search ?? ''} onChange={(event) => update({ search: event.target.value || undefined })} placeholder="회사명·국가·업종" /></label>
    </form>
  )
}
