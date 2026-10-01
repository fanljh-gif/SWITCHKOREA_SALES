import { useEffect, useState, type FormEvent } from 'react'

import { useSalesApi } from '../../app/App'
import type { ItemType, Product } from '../../shared/api/contracts'
import { PricePage } from '../prices/PricePage'

export function ProductPage() {
  const api = useSalesApi()
  const [rows, setRows] = useState<Product[]>([])
  useEffect(() => { void api.listProducts().then(setRows) }, [api])

  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    const row = await api.createProduct({ code: String(data.get('code')), name: String(data.get('name')), itemType: String(data.get('itemType')) as ItemType, specification: String(data.get('specification')), status: 'active' })
    setRows((current) => [...current, row])
  }

  return (
    <section>
      <h1>품목·단가 관리</h1>
      <form onSubmit={(event) => void create(event)}>
        <label>품목 코드<input name="code" required /></label><label>품명<input name="name" required /></label>
        <label>구분<select name="itemType" required><option value="product">제품</option><option value="goods">상품</option></select></label>
        <label>규격<input name="specification" /></label><button type="submit">품목 등록</button>
      </form>
      <table><thead><tr><th>코드</th><th>품명</th><th>구분</th><th>규격</th><th>상태</th></tr></thead><tbody>{rows.map((row) => <tr key={row.id}><td>{row.code}</td><td>{row.name}</td><td>{row.itemType === 'product' ? '제품' : '상품'}</td><td>{row.specification}</td><td>{row.status === 'active' ? '사용' : '중지'}</td></tr>)}</tbody></table>
      <PricePage products={rows} />
    </section>
  )
}
