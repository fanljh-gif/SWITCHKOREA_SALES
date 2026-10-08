import { afterEach, describe, expect, it, vi } from 'vitest'

import { HttpSalesApi } from '../src/shared/api/HttpSalesApi'
import type { QuoteCreateInput } from '../src/shared/api/contracts'

afterEach(() => vi.unstubAllGlobals())

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

const inquiry = { id: 'i1', inquiryNumber: 'INQ-202610-001', title: '문의', customerId: 'c1', customerName: '대한전자', contactName: '김현수', ownerId: 'u2', receivedAt: '2026-10-08', responseDueAt: '2026-10-10', status: 'Responding', summary: '요청', activities: [], attachments: [], linkedQuoteIds: [], version: 2 }
const revision = { id: 'r1', quoteId: 'q1', revisionNumber: 0, revisionLabel: 'Rev.0', status: 'ApprovalRequested', currency: 'KRW', tradeType: 'Domestic', taxType: 'Exclusive', validUntil: '2026-10-31', incotermCode: null, namedPlace: null, discountAmount: 0, shippingAmount: 0, otherAmount: 0, subtotalAmount: 1000, supplyAmount: 1000, taxAmount: 100, totalAmount: 1100, lines: [], approvals: [], dispatches: [], version: 3 }
const quote = { id: 'q1', quoteNumber: 'SWK-202610-001', customerId: 'c1', customerName: '대한전자', ownerId: 'u2', currentRevisionId: 'r1', currentRevision: revision, revisions: [revision], version: 4 }
const createInput: QuoteCreateInput = { customerId: 'c1', ownerId: 'u2', tradeType: 'domestic', currency: 'KRW', taxType: 'exclusive', validUntil: '2026-10-31', incotermCode: null, namedPlace: null, discountAmount: 0, shippingAmount: 0, otherAmount: 0, lines: [{ productId: 'p1', description: '스위치', quantity: 1, unitPrice: 1000 }] }

describe('HttpSalesApi quotation and inquiry contract', () => {
  it('maps inquiry filters and write endpoints', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(json([inquiry]))
      .mockResolvedValueOnce(json(inquiry))
      .mockResolvedValueOnce(json(inquiry))
      .mockResolvedValueOnce(json(inquiry))
      .mockResolvedValueOnce(json({ id: 'a1', inquiryId: 'i1', kind: 'memo', body: '확인', actorId: 'u1', occurredAt: '2026-10-08' }))
    vi.stubGlobal('fetch', fetchMock)
    const api = new HttpSalesApi('/base')

    const rows = await api.listInquiries({ ownerId: 'u2', status: 'responding', overdueOnly: true })
    await api.saveInquiry({ id: 'i1', expectedVersion: 2, title: '문의', customerId: 'c1', contactName: '김현수', ownerId: 'u2', receivedAt: '2026-10-08', responseDueAt: '2026-10-10', summary: '수정' })
    await api.assignInquiry('i1', 'u3', 2)
    await api.changeInquiryStatus('i1', 'answered', '답변 완료', 3)
    await api.addInquiryActivity('i1', 'memo', '확인')

    expect(rows[0].status).toBe('responding')
    expect(fetchMock.mock.calls.map((call) => [call[0], call[1].method ?? 'GET'])).toEqual([
      ['/base/api/inquiries?ownerId=u2&status=responding&overdueOnly=true', 'GET'],
      ['/base/api/inquiries/i1', 'PUT'],
      ['/base/api/inquiries/i1/assign', 'POST'],
      ['/base/api/inquiries/i1/status', 'POST'],
      ['/base/api/inquiries/i1/activities', 'POST'],
    ])
  })

  it('uploads multipart attachments and downloads attachment blobs', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(json({ id: 'f1', inquiryId: 'i1', fileName: 'spec.pdf', contentType: 'application/pdf', size: 3, uploadedBy: 'u2', uploadedAt: '2026-10-08' }))
      .mockResolvedValueOnce(new Response(new Blob(['pdf'], { type: 'application/pdf' })))
    vi.stubGlobal('fetch', fetchMock)
    const api = new HttpSalesApi()
    await api.uploadInquiryAttachment('i1', new File(['pdf'], 'spec.pdf', { type: 'application/pdf' }))
    const downloaded = await api.downloadInquiryAttachment('i1', 'f1')

    expect(fetchMock.mock.calls[0][0]).toBe('/api/inquiries/i1/attachments')
    expect(fetchMock.mock.calls[0][1].body).toBeInstanceOf(FormData)
    expect(downloaded).toBeInstanceOf(Blob)
    expect(fetchMock.mock.calls[1][0]).toBe('/api/inquiries/i1/attachments/f1')
  })

  it('maps quote lifecycle endpoints and PDF language', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(json(quote))
      .mockResolvedValueOnce(json(revision))
      .mockResolvedValueOnce(json(revision))
      .mockResolvedValueOnce(json(revision))
      .mockResolvedValueOnce(json(revision))
      .mockResolvedValueOnce(json(revision))
      .mockResolvedValueOnce(new Response(new Blob(['%PDF-'], { type: 'application/pdf' })))
      .mockResolvedValueOnce(json({ id: 'd1', revisionId: 'r1', sentAt: '2026-10-08', memo: '발송', language: 'Ko', recordedBy: 'u2', version: 4 }))
      .mockResolvedValueOnce(json({ ...quote, result: 'Won' }))
    vi.stubGlobal('fetch', fetchMock)
    const api = new HttpSalesApi()

    await api.createQuote(createInput)
    await api.saveQuoteRevision('q1', 'r1', { ...createInput, expectedVersion: 3 })
    await api.submitQuoteRevision('q1', 'r1', 3)
    await api.approveQuoteRevision('q1', 'r1', 4)
    await api.rejectQuoteRevision('q1', 'r1', '단가 재검토', 4)
    await api.createQuoteRevision('q1', 4)
    expect(await api.downloadQuotePdf('q1', 'r1', 'en')).toBeInstanceOf(Blob)
    await api.recordQuoteDispatch('q1', 'r1', { sentAt: '2026-10-08', memo: '발송', language: 'ko', expectedVersion: 4 })
    await api.recordQuoteResult('q1', 'r1', { result: 'won', note: '수주', expectedVersion: 5 })

    expect(fetchMock.mock.calls.map((call) => [call[0], call[1].method ?? 'GET'])).toEqual([
      ['/api/quotes', 'POST'],
      ['/api/quotes/q1/revisions/r1', 'PUT'],
      ['/api/quotes/q1/revisions/r1/submit', 'POST'],
      ['/api/quotes/q1/revisions/r1/approve', 'POST'],
      ['/api/quotes/q1/revisions/r1/reject', 'POST'],
      ['/api/quotes/q1/revisions', 'POST'],
      ['/api/quotes/q1/revisions/r1/pdf?language=en', 'GET'],
      ['/api/quotes/q1/revisions/r1/dispatch', 'POST'],
      ['/api/quotes/q1/revisions/r1/result', 'POST'],
    ])
  })

  it('maps Incoterms management endpoints', async () => {
    const row = { id: 'ic1', code: 'CIF', displayName: 'Cost Insurance and Freight', status: 'Active', sortOrder: 10, version: 1 }
    const fetchMock = vi.fn().mockResolvedValueOnce(json([row])).mockResolvedValueOnce(json(row)).mockResolvedValueOnce(json(row))
    vi.stubGlobal('fetch', fetchMock)
    const api = new HttpSalesApi()
    expect((await api.listIncoterms())[0].status).toBe('active')
    await api.saveIncoterm({ code: 'DAP', displayName: 'Delivered at Place', status: 'active', sortOrder: 20 })
    await api.saveIncoterm({ id: 'ic1', code: 'CIF', displayName: 'CIF', status: 'inactive', sortOrder: 10, expectedVersion: 1 })
    expect(fetchMock.mock.calls.map((call) => [call[0], call[1].method ?? 'GET'])).toEqual([
      ['/api/admin/incoterms', 'GET'],
      ['/api/admin/incoterms', 'POST'],
      ['/api/admin/incoterms/ic1', 'PUT'],
    ])
  })
})
