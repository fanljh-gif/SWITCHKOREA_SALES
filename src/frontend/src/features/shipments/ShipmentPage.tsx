import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'

import { useSalesApi } from '../../app/App'
import { useAuth } from '../../app/AuthSession'
import type { Customer, MonthlyRate, Price, Product, Shipment } from '../../shared/api/contracts'
import { Money } from '../../shared/ui/Money'

export function ShipmentPage() {
  const api = useSalesApi()
  const { currentUser } = useAuth()
  const [customers, setCustomers] = useState<Customer[]>([])
  const [products, setProducts] = useState<Product[]>([])
  const [prices, setPrices] = useState<Price[]>([])
  const [rates, setRates] = useState<MonthlyRate[]>([])
  const [rows, setRows] = useState<Shipment[]>([])
  const [customerId, setCustomerId] = useState('')
  const [productId, setProductId] = useState('')
  const [quantity, setQuantity] = useState(0)
  const [shipmentDate, setShipmentDate] = useState('2026-10-01')

  useEffect(() => { void Promise.all([api.listCustomers(), api.listProducts(), api.listPrices(), api.listMonthlyRates('2026-10'), api.listShipments()]).then(([customerRows, productRows, priceRows, rateRows, shipmentRows]) => { setCustomers(customerRows); setProducts(productRows); setPrices(priceRows); setRates(rateRows); setRows(shipmentRows); setCustomerId(customerRows[0]?.id ?? ''); setProductId(productRows[0]?.id ?? '') }) }, [api])

  const price = useMemo(() => prices.find((row) => row.customerId === customerId && row.productId === productId && row.validFrom <= shipmentDate && (!row.validTo || row.validTo >= shipmentDate)), [customerId, prices, productId, shipmentDate])
  const rate = price?.currency === 'KRW' ? 1 : rates.find((row) => row.currency === price?.currency)?.rate
  const supplyAmount = (price?.unitPrice ?? 0) * quantity
  const krwAmount = supplyAmount * (rate ?? 0)

  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!price || !rate || !currentUser) return
    const row = await api.createShipment({ shipmentNumber: `S-${Date.now()}`, shipmentDate, customerId, productId, quantity, unitPrice: price.unitPrice, currency: price.currency, supplyAmount, appliedRate: rate, krwAmount, ownerId: currentUser.id, status: 'confirmed' })
    setRows((current) => [row, ...current])
  }

  return (
    <section>
      <h1>출고·매출 관리</h1>
      <form onSubmit={(event) => void create(event)}>
        <label>출고일<input type="date" value={shipmentDate} onChange={(event) => setShipmentDate(event.target.value)} /></label>
        <label>거래처<select value={customerId} onChange={(event) => setCustomerId(event.target.value)}>{customers.map((row) => <option key={row.id} value={row.id}>{row.companyName}</option>)}</select></label>
        <label>품목<select value={productId} onChange={(event) => setProductId(event.target.value)}>{products.map((row) => <option key={row.id} value={row.id}>{row.name}</option>)}</select></label>
        <label>수량<input type="number" min="1" value={quantity || ''} onChange={(event) => setQuantity(Number(event.target.value))} /></label>
        <p>적용단가: {price ? <Money amount={price.unitPrice} currency={price.currency} /> : '유효 단가 없음'}</p>
        <p>공급가액: {price ? <Money amount={supplyAmount} currency={price.currency} /> : '-'}</p>
        <p>원화 환산금액: <Money amount={krwAmount} /></p>
        {price && !rate && <p role="alert">월 지정환율이 없습니다. <Link to="/settings">환율·목표 관리로 이동</Link></p>}
        <button type="submit" disabled={!price || !rate || quantity <= 0}>출고 저장</button>
      </form>
      <table><thead><tr><th>출고일</th><th>거래처</th><th>품목</th><th>수량</th><th>원화 매출</th><th>상태</th></tr></thead><tbody>{rows.map((row) => <tr key={row.id}><td>{row.shipmentDate}</td><td>{customers.find((item) => item.id === row.customerId)?.companyName}</td><td>{products.find((item) => item.id === row.productId)?.name}</td><td>{row.quantity}</td><td><Money amount={row.krwAmount} /></td><td>{row.status === 'confirmed' ? '확정' : '취소'}</td></tr>)}</tbody></table>
    </section>
  )
}
