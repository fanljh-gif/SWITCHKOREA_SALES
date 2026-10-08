import { useCallback, useEffect, useState } from 'react'

import { useSalesApi } from '../../app/App'
import { useAuth } from '../../app/AuthSession'
import type { Customer, CustomerInquiryDto, IncotermDto, Product, QuoteDashboardDto, QuoteDto, User } from '../../shared/api/contracts'
import { InquiryCreateForm } from './InquiryCreateForm'
import { InquiryDetail } from './InquiryDetail'
import { InquiryList } from './InquiryList'
import { QuoteDashboard } from './QuoteDashboard'
import { QuoteEditor } from './QuoteEditor'
import { QuoteList } from './QuoteList'
import './quotes.css'

type Tab = 'dashboard' | 'inquiries' | 'quotes'

export function QuotationInquiryPage() {
  const api = useSalesApi(); const { currentUser } = useAuth()
  const [tab, setTab] = useState<Tab>('quotes'); const [summary, setSummary] = useState<QuoteDashboardDto | null>(null); const [inquiries, setInquiries] = useState<CustomerInquiryDto[]>([]); const [quotes, setQuotes] = useState<QuoteDto[]>([]); const [incoterms, setIncoterms] = useState<IncotermDto[]>([]); const [products, setProducts] = useState<Product[]>([]); const [customers, setCustomers] = useState<Customer[]>([]); const [users, setUsers] = useState<User[]>([]); const [showInquiryForm, setShowInquiryForm] = useState(false); const [selectedInquiryId, setSelectedInquiryId] = useState<string>(); const [selectedQuoteId, setSelectedQuoteId] = useState<string>(); const [loading, setLoading] = useState(true); const [busy, setBusy] = useState(false); const [error, setError] = useState<string>()

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const [nextSummary, nextInquiries, nextQuotes, nextIncoterms, nextProducts, nextCustomers, nextUsers] = await Promise.all([api.getQuotationDashboard(), api.listInquiries(), api.listQuotes(), api.listIncoterms(), api.listProducts(), api.listCustomers(), currentUser?.role === 'manager' ? api.listUsers() : Promise.resolve(currentUser ? [currentUser] : [])])
      setSummary(nextSummary); setInquiries(nextInquiries); setQuotes(nextQuotes); setIncoterms(nextIncoterms); setProducts(nextProducts); setCustomers(nextCustomers); setUsers(nextUsers)
      setSelectedInquiryId((value) => nextInquiries.some((row) => row.id === value) ? value : nextInquiries[0]?.id)
      setSelectedQuoteId((value) => nextQuotes.some((row) => row.id === value) ? value : nextQuotes[0]?.id)
      setError(undefined)
    } catch (cause) { setError(cause instanceof Error ? cause.message : '견적·고객문의 정보를 불러오지 못했습니다.') }
    finally { setLoading(false) }
  }, [api, currentUser])

  useEffect(() => { void load() }, [load])
  const selectedInquiry = inquiries.find((row) => row.id === selectedInquiryId)
  const selectedQuote = quotes.find((row) => row.id === selectedQuoteId)
  const mutate = async (action: () => Promise<unknown>) => { setBusy(true); try { await action(); await load() } catch (cause) { setError(cause instanceof Error ? cause.message : '작업을 처리하지 못했습니다.') } finally { setBusy(false) } }
  const createConnectedQuote = () => {
    if (!selectedInquiry || !currentUser || !products[0]) return
    void mutate(() => api.createQuote({ inquiryId: selectedInquiry.id, customerId: selectedInquiry.customerId, ownerId: currentUser.role === 'manager' ? selectedInquiry.ownerId : currentUser.id, tradeType: 'domestic', currency: 'KRW', taxType: 'exclusive', validUntil: '2026-10-31', incotermCode: null, namedPlace: null, discountAmount: 0, shippingAmount: 0, otherAmount: 0, lines: [{ productId: products[0].id, description: products[0].name, quantity: 1, unitPrice: 0 }] }))
    setTab('quotes')
  }
  const pdf = async (language: 'ko' | 'en') => { if (!selectedQuote) return; try { const blob = await api.downloadQuotePdf(selectedQuote.id, selectedQuote.currentRevision.id, language); const url = URL.createObjectURL(blob); const anchor = document.createElement('a'); anchor.href = url; anchor.download = `${selectedQuote.quoteNumber}-${selectedQuote.currentRevision.revisionLabel}-${language}.pdf`; anchor.click(); URL.revokeObjectURL(url) } catch (cause) { setError(cause instanceof Error ? cause.message : 'PDF를 생성하지 못했습니다.') } }

  if (!currentUser) return <p role="status">사용자 정보를 확인하는 중입니다.</p>
  return <section className="quotation-page"><div className="page-heading"><div><p className="eyebrow">B2B QUOTATION & INQUIRY</p><h1>견적·고객문의</h1><p>문의 접수부터 견적 승인, PDF 발행과 결과까지 관리합니다.</p></div><button type="button" onClick={() => { setTab('inquiries'); setShowInquiryForm(true) }}>+ 고객문의 등록</button></div>
    {error && <p role="alert">{error}</p>}
    {summary && <QuoteDashboard summary={summary} inquiries={inquiries} quotes={quotes} />}
    <div className="quote-tabs" role="tablist" aria-label="견적·고객문의 보기"><button type="button" role="tab" aria-selected={tab === 'dashboard'} onClick={() => setTab('dashboard')}>현황</button><button type="button" role="tab" aria-selected={tab === 'inquiries'} onClick={() => setTab('inquiries')}>고객문의</button><button type="button" role="tab" aria-selected={tab === 'quotes'} onClick={() => setTab('quotes')}>견적서</button></div>
    {showInquiryForm && <InquiryCreateForm customers={customers} users={users} currentUser={currentUser} busy={busy} onCancel={() => setShowInquiryForm(false)} onSave={(input) => void mutate(async () => { const saved = await api.saveInquiry(input); setSelectedInquiryId(saved.id); setShowInquiryForm(false) })} />}
    {loading ? <p role="status">견적·고객문의 정보를 불러오는 중입니다.</p> : tab === 'dashboard' ? summary && <QuoteDashboard summary={summary} inquiries={inquiries} quotes={quotes} /> : tab === 'inquiries' ? <div className="quotation-layout"><InquiryList rows={inquiries} selectedId={selectedInquiryId} onSelect={setSelectedInquiryId} />{selectedInquiry && <InquiryDetail row={selectedInquiry} onAddActivity={(body) => void mutate(() => api.addInquiryActivity(selectedInquiry.id, 'memo', body))} onChangeStatus={(status) => void mutate(() => api.changeInquiryStatus(selectedInquiry.id, status, undefined, selectedInquiry.version))} onUpload={(file) => void mutate(() => api.uploadInquiryAttachment(selectedInquiry.id, file))} onCreateQuote={createConnectedQuote} />}</div> : <div className="quotation-layout"><QuoteList rows={quotes} selectedId={selectedQuoteId} onSelect={setSelectedQuoteId} />{selectedQuote && <QuoteEditor quote={selectedQuote} user={currentUser} incoterms={incoterms} busy={busy} onSave={(input) => void mutate(() => api.saveQuoteRevision(selectedQuote.id, selectedQuote.currentRevision.id, input))} onSubmit={() => void mutate(() => api.submitQuoteRevision(selectedQuote.id, selectedQuote.currentRevision.id, selectedQuote.currentRevision.version))} onApprove={() => void mutate(() => api.approveQuoteRevision(selectedQuote.id, selectedQuote.currentRevision.id, selectedQuote.currentRevision.version))} onReject={() => void mutate(() => api.rejectQuoteRevision(selectedQuote.id, selectedQuote.currentRevision.id, '단가 및 조건 재검토', selectedQuote.currentRevision.version))} onNewRevision={() => void mutate(() => api.createQuoteRevision(selectedQuote.id, selectedQuote.version))} onPdf={(language) => void pdf(language)} onDispatch={() => void mutate(() => api.recordQuoteDispatch(selectedQuote.id, selectedQuote.currentRevision.id, { sentAt: new Date().toISOString(), memo: '고객 발송 완료', language: 'ko', expectedVersion: selectedQuote.currentRevision.version }))} onResult={(result) => void mutate(() => api.recordQuoteResult(selectedQuote.id, selectedQuote.currentRevision.id, { result, note: result === 'won' ? '수주' : result === 'lost' ? '실주' : '보류', expectedVersion: selectedQuote.currentRevision.version }))} />}</div>}
  </section>
}
