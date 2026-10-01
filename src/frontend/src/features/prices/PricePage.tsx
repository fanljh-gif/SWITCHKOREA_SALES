import { useEffect, useState, type FormEvent } from 'react'

import { useSalesApi } from '../../app/App'
import type { Customer, Currency, Price, Product } from '../../shared/api/contracts'
import { ConcurrencyNotice } from '../../shared/ui/ConcurrencyNotice'
import { Money } from '../../shared/ui/Money'

export function PricePage({ products }: { products: Product[] }) {
  const api = useSalesApi()
  const [customers, setCustomers] = useState<Customer[]>([])
  const [rows, setRows] = useState<Price[]>([])
  const [conflict, setConflict] = useState(false)
  useEffect(() => { void Promise.all([api.listCustomers(), api.listPrices()]).then(([customerRows, priceRows]) => { setCustomers(customerRows); setRows(priceRows) }) }, [api])

  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    try {
      const row = await api.createPrice({ customerId: String(data.get('customerId')), productId: String(data.get('productId')), currency: String(data.get('currency')) as Currency, unitPrice: Number(data.get('unitPrice')), validFrom: String(data.get('validFrom')), validTo: String(data.get('validTo')) || undefined })
      setRows((current) => [...current, row])
    } catch { setConflict(true) }
  }

  return (
    <section>
      <h2>거래처별 단가</h2>
      {conflict && <ConcurrencyNotice kind="price" />}
      <form onSubmit={(event) => void create(event)}>
        <label>거래처<select name="customerId">{customers.map((row) => <option key={row.id} value={row.id}>{row.companyName}</option>)}</select></label>
        <label>품목<select name="productId">{products.map((row) => <option key={row.id} value={row.id}>{row.name}</option>)}</select></label>
        <label>통화<select name="currency"><option>KRW</option><option>USD</option><option>CNY</option></select></label>
        <label>단가<input name="unitPrice" type="number" min="0" step="0.01" required /></label>
        <label>시작일<input name="validFrom" type="date" required /></label>
        <label>종료일<input name="validTo" type="date" /></label>
        <button type="submit">단가 등록</button>
      </form>
      <table><thead><tr><th>거래처</th><th>품목</th><th>단가</th><th>적용기간</th></tr></thead><tbody>{rows.map((row) => <tr key={row.id}><td>{customers.find((item) => item.id === row.customerId)?.companyName}</td><td>{products.find((item) => item.id === row.productId)?.name}</td><td><Money amount={row.unitPrice} currency={row.currency} /></td><td>{row.validFrom} ~ {row.validTo ?? '계속'}</td></tr>)}</tbody></table>
    </section>
  )
}
