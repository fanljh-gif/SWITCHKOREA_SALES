import { describe, expect, it } from 'vitest'

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
