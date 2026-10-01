import { afterEach, describe, expect, it, vi } from 'vitest'

import { HttpSalesApi, SalesApiError } from '../src/shared/api/HttpSalesApi'

describe('HttpSalesApi', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('sends same-origin cookie credentials and serializes dashboard filters', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ krwTotal: 40500, targetKrw: 50000, achievementRate: 81, byCurrency: [], byOwner: [], byCustomer: [], byProduct: [] }), { status: 200, headers: { 'Content-Type': 'application/json' } }))
    vi.stubGlobal('fetch', fetchMock)
    const api = new HttpSalesApi()

    const summary = await api.getDashboardSummary({ dateFrom: '2026-10-01', dateTo: '2026-10-31', ownerId: 'owner-1' })

    expect(fetchMock).toHaveBeenCalledWith('/api/dashboard?from=2026-10-01&to=2026-10-31&ownerId=owner-1', expect.objectContaining({ credentials: 'include' }))
    expect(summary.monthSalesKrw).toBe(40500)
  })

  it('converts RFC 7807 responses into status-aware errors', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ title: '다른 사용자가 먼저 수정했습니다.', status: 409, detail: 'version conflict' }), { status: 409, headers: { 'Content-Type': 'application/problem+json' } })))
    const api = new HttpSalesApi()

    await expect(api.listCustomers()).rejects.toMatchObject({ status: 409, message: '다른 사용자가 먼저 수정했습니다.' } satisfies Partial<SalesApiError>)
  })
})
