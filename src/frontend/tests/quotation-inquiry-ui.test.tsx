import '@testing-library/jest-dom/vitest'
import { fireEvent, render, screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'

import { App } from '../src/app/App'
import { calculateQuote, validateQuoteDraft } from '../src/features/quotes/quoteCalculations'
import { SalesApiError } from '../src/shared/api/HttpSalesApi'
import { MockSalesApi } from '../src/shared/api/MockSalesApi'
import type { QuoteCreateInput, User } from '../src/shared/api/contracts'

const member: User = {
  id: 'u2', loginId: 'ptw', name: '박태원', email: 'ptw0517@switchkorea.com', role: 'member', status: 'active',
}

function quoteInput(overrides: Partial<QuoteCreateInput> = {}): QuoteCreateInput {
  return {
    inquiryId: 'inquiry-1',
    customerId: 'c1',
    ownerId: 'u2',
    tradeType: 'domestic',
    currency: 'KRW',
    taxType: 'exclusive',
    validUntil: '2026-10-31',
    incotermCode: null,
    namedPlace: null,
    discountAmount: 2_000,
    shippingAmount: 1_000,
    otherAmount: 0,
    lines: [{ productId: 'p1', description: '마이크로 스위치', quantity: 10, unitPrice: 3_200 }],
    ...overrides,
  }
}

describe('quotation and inquiry Mock API', () => {
  it('renders manager dashboard and member-owned records', async () => {
    const managerApi = new MockSalesApi()
    const memberApi = new MockSalesApi({ currentUser: member })

    const managerDashboard = await managerApi.getQuotationDashboard()
    const managerInquiries = await managerApi.listInquiries()
    const memberInquiries = await memberApi.listInquiries()
    const memberQuotes = await memberApi.listQuotes()

    expect(managerDashboard.unansweredCount).toBeGreaterThan(0)
    expect(managerDashboard.approvalPendingCount).toBeGreaterThan(0)
    expect(managerInquiries.length).toBeGreaterThan(memberInquiries.length)
    expect(memberInquiries.every((row) => row.ownerId === member.id)).toBe(true)
    expect(memberQuotes.every((row) => row.ownerId === member.id)).toBe(true)
  })

  it('calculates domestic and export quotes', async () => {
    const api = new MockSalesApi()
    const domestic = await api.createQuote(quoteInput())
    const exported = await api.createQuote(quoteInput({
      customerId: 'c2', ownerId: 'u3', tradeType: 'export', currency: 'USD', taxType: 'exempt',
      incotermCode: 'CIF', namedPlace: 'Port of Los Angeles', discountAmount: 0, shippingAmount: 25,
      lines: [{ productId: 'p1', description: 'Micro Switch', quantity: 100, unitPrice: 2.5 }],
    }))

    expect(domestic.currentRevision).toMatchObject({ subtotalAmount: 32_000, taxAmount: 3_100, totalAmount: 34_100 })
    expect(exported.currentRevision).toMatchObject({ subtotalAmount: 250, taxAmount: 0, totalAmount: 275, incotermCode: 'CIF', namedPlace: 'Port of Los Angeles' })
    await expect(api.createQuote(quoteInput({ tradeType: 'export', currency: 'USD', taxType: 'exempt' }))).rejects.toMatchObject({ status: 400 } satisfies Partial<SalesApiError>)
  })

  it('requires manager approval before PDF or dispatch', async () => {
    const memberApi = new MockSalesApi({ currentUser: member })
    const memberQuote = await memberApi.createQuote(quoteInput())
    const submitted = await memberApi.submitQuoteRevision(memberQuote.id, memberQuote.currentRevision.id, memberQuote.currentRevision.version)

    await expect(memberApi.approveQuoteRevision(memberQuote.id, submitted.id, submitted.version)).rejects.toMatchObject({ status: 403 } satisfies Partial<SalesApiError>)
    await expect(memberApi.downloadQuotePdf(memberQuote.id, submitted.id, 'ko')).rejects.toMatchObject({ status: 422 } satisfies Partial<SalesApiError>)
    await expect(memberApi.recordQuoteDispatch(memberQuote.id, submitted.id, { sentAt: '2026-10-08T14:30:00+09:00', memo: '이메일 발송', expectedVersion: submitted.version })).rejects.toMatchObject({ status: 422 } satisfies Partial<SalesApiError>)

    const managerApi = new MockSalesApi()
    const managerQuote = await managerApi.createQuote(quoteInput())
    const managerSubmitted = await managerApi.submitQuoteRevision(managerQuote.id, managerQuote.currentRevision.id, managerQuote.currentRevision.version)
    const approved = await managerApi.approveQuoteRevision(managerQuote.id, managerSubmitted.id, managerSubmitted.version)
    expect(await managerApi.downloadQuotePdf(managerQuote.id, approved.id, 'en')).toBeInstanceOf(Blob)
  })

  it('creates Rev.1 from approved Rev.0', async () => {
    const api = new MockSalesApi()
    const quote = await api.createQuote(quoteInput())
    const submitted = await api.submitQuoteRevision(quote.id, quote.currentRevision.id, quote.currentRevision.version)
    const approved = await api.approveQuoteRevision(quote.id, submitted.id, submitted.version)
    const revision = await api.createQuoteRevision(quote.id, approved.version)

    expect(revision).toMatchObject({ revisionNumber: 1, revisionLabel: 'Rev.1', status: 'draft' })
    expect(revision.lines.map(({ id: _id, ...line }) => line)).toEqual(approved.lines.map(({ id: _id, ...line }) => line))
    expect(revision.lines[0].id).not.toBe(approved.lines[0].id)
  })

  it('keeps won quotes separate from shipments', async () => {
    const api = new MockSalesApi()
    const shipmentCount = (await api.listShipments()).length
    const quote = await api.createQuote(quoteInput())
    const submitted = await api.submitQuoteRevision(quote.id, quote.currentRevision.id, quote.currentRevision.version)
    const approved = await api.approveQuoteRevision(quote.id, submitted.id, submitted.version)
    const sent = await api.recordQuoteDispatch(quote.id, approved.id, { sentAt: '2026-10-08T14:30:00+09:00', memo: '이메일 발송', expectedVersion: approved.version })
    const won = await api.recordQuoteResult(quote.id, sent.revisionId, { result: 'won', note: '양산 수주', expectedVersion: sent.version })

    expect(won.result).toBe('won')
    expect((await api.listShipments()).length).toBe(shipmentCount)
  })
})

describe('quotation and inquiry workspace', () => {
  it('renders the three tabs, dashboard alerts, manager approval, and approved revision lock', async () => {
    const api = new MockSalesApi()
    const pending = await api.getQuote('quote-seed-1')
    await api.approveQuoteRevision(pending.id, pending.currentRevision.id, pending.currentRevision.version)
    window.history.pushState({}, '', '/quotes')

    render(<App api={api} />)

    expect(await screen.findByRole('heading', { name: '견적·고객문의' })).toBeVisible()
    expect(screen.getAllByRole('tab').map((tab) => tab.textContent)).toEqual(['현황', '고객문의', '견적서'])
    expect(await screen.findByText('기한 초과')).toBeVisible()
    expect(await screen.findByRole('button', { name: '승인' })).toBeVisible()
    expect(await screen.findByText('승인된 개정본은 수정할 수 없습니다.')).toBeVisible()
    expect(await screen.findByRole('button', { name: '새 개정 만들기' })).toBeVisible()
    fireEvent.click(screen.getByRole('button', { name: /SWK-202610-002/ }))
    expect(screen.getByRole('combobox', { name: '거래구분' })).toBeEnabled()
    expect(screen.getByRole('textbox', { name: '지정 장소' })).toBeEnabled()
    expect(screen.getByRole('button', { name: '견적 저장' })).toBeVisible()
    fireEvent.click(screen.getByRole('button', { name: '+ 고객문의 등록' }))
    expect(screen.getByRole('form', { name: '고객문의 등록' })).toBeVisible()
  })

  it('shows only owned data and no approval action for a member', async () => {
    window.history.pushState({}, '', '/quotes')
    render(<App api={new MockSalesApi({ currentUser: member })} />)

    expect(await screen.findByRole('heading', { name: '견적·고객문의' })).toBeVisible()
    const workspace = screen.getByRole('main')
    expect((await within(workspace).findAllByText(/대한전자/)).length).toBeGreaterThan(0)
    expect(within(workspace).queryByText(/글로벌테크/)).not.toBeInTheDocument()
    expect(within(workspace).queryByRole('button', { name: '승인' })).not.toBeInTheDocument()
  })

  it('calculates all tax modes and validates export terms', () => {
    const base = { currency: 'KRW' as const, subtotalAmount: 32_000, discountAmount: 2_000, shippingAmount: 1_000, otherAmount: 0 }
    expect(calculateQuote({ ...base, taxType: 'exclusive' })).toEqual({ supplyAmount: 31_000, taxAmount: 3_100, totalAmount: 34_100 })
    expect(calculateQuote({ ...base, taxType: 'inclusive' })).toEqual({ supplyAmount: 28_182, taxAmount: 2_818, totalAmount: 31_000 })
    expect(calculateQuote({ ...base, taxType: 'exempt' })).toEqual({ supplyAmount: 31_000, taxAmount: 0, totalAmount: 31_000 })
    expect(validateQuoteDraft({ tradeType: 'export', incotermCode: '', namedPlace: '' })).toContain('Incoterms')
    expect(validateQuoteDraft({ tradeType: 'export', incotermCode: 'CIF', namedPlace: 'Port of Los Angeles' })).toBeNull()
  })

  it('exposes Incoterms management to the manager', async () => {
    window.history.pushState({}, '', '/admin')
    render(<App api={new MockSalesApi()} />)
    expect(await screen.findByRole('heading', { name: 'Incoterms 관리' })).toBeVisible()
    expect(await screen.findByText('CIF')).toBeVisible()
    fireEvent.click(screen.getByRole('button', { name: 'Incoterms 추가' }))
    expect(screen.getByRole('form', { name: 'Incoterms 추가' })).toBeVisible()
    expect(screen.getByRole('spinbutton', { name: 'CIF 표시순서' })).toBeVisible()
  })
})
