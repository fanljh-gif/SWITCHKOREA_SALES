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

  it('maps exact sales-network routes, queries, JSON names, and expected versions', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response('[]', { status: 200, headers: { 'Content-Type': 'application/json' } }))
      .mockResolvedValueOnce(new Response('[]', { status: 200, headers: { 'Content-Type': 'application/json' } }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: 'nc-1', companyName: '대성전자', country: '대한민국', industry: '전자부품', lifecycleType: 'Prospect', isOem: false, customerId: null, status: 'Active', version: 4 }), { status: 200, headers: { 'Content-Type': 'application/json' } }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: 'nc-1', companyName: '대성전자', country: '대한민국', industry: '전자부품', lifecycleType: 'Customer', isOem: false, customerId: 'c9', status: 'Active', version: 5 }), { status: 200, headers: { 'Content-Type': 'application/json' } }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: 'rel-1', upstreamCompanyId: 'oem-1', downstreamCompanyId: 'nc-1', validFrom: '2026-10-01', status: 'Active', version: 1, supplies: [] }), { status: 200, headers: { 'Content-Type': 'application/json' } }))
      .mockResolvedValueOnce(new Response('[]', { status: 200, headers: { 'Content-Type': 'application/json' } }))
      .mockResolvedValueOnce(new Response('[]', { status: 200, headers: { 'Content-Type': 'application/json' } }))
    vi.stubGlobal('fetch', fetchMock)
    const api = new HttpSalesApi('/base')

    await api.listNetworkCompanies({ rootCompanyId: 'oem-1', lifecycleType: 'prospect', ownerId: 'u2', status: 'active', search: 'switch' })
    await api.getNetworkTree({ rootCompanyId: 'oem-1' })
    await api.updateNetworkCompany('nc-1', { companyName: '대성전자', country: '대한민국', industry: '전자부품', lifecycleType: 'prospect', isOem: false, status: 'active', expectedVersion: 3 })
    await api.convertNetworkCompany('nc-1', { customerCode: 'CUS-900', defaultCurrency: 'KRW', expectedVersion: 4 })
    await api.createNetworkRelationship({ upstreamCompanyId: 'oem-1', downstreamCompanyId: 'nc-1', validFrom: '2026-10-01', status: 'active', notes: '양산', supplies: [{ productId: 'p1', applicationName: '조작부', notes: '승인' }] })
    await api.listCompanyContacts('nc-1')
    await api.listCompanyAssignments('nc-1')

    expect(fetchMock.mock.calls[0][0]).toBe('/base/api/network/companies?rootCompanyId=oem-1&lifecycleType=prospect&ownerId=u2&status=active&search=switch')
    expect(fetchMock.mock.calls[1][0]).toBe('/base/api/network/tree?rootCompanyId=oem-1')
    expect(fetchMock.mock.calls[2]).toEqual(['/base/api/network/companies/nc-1', expect.objectContaining({ method: 'PUT', body: JSON.stringify({ companyName: '대성전자', country: '대한민국', industry: '전자부품', lifecycleType: 'prospect', isOem: false, status: 'active', expectedVersion: 3 }) })])
    expect(fetchMock.mock.calls[3]).toEqual(['/base/api/network/companies/nc-1/convert', expect.objectContaining({ method: 'POST', body: JSON.stringify({ customerCode: 'CUS-900', defaultCurrency: 'KRW', expectedVersion: 4 }) })])
    expect(fetchMock.mock.calls[4][0]).toBe('/base/api/network/relationships')
    expect(JSON.parse(fetchMock.mock.calls[4][1].body)).toEqual(expect.objectContaining({ upstreamCompanyId: 'oem-1', downstreamCompanyId: 'nc-1', supplies: [{ productId: 'p1', applicationName: '조작부', notes: '승인' }] }))
    expect(fetchMock.mock.calls[5][0]).toBe('/base/api/network/companies/nc-1/contacts')
    expect(fetchMock.mock.calls[6][0]).toBe('/base/api/network/companies/nc-1/assignments')
  })

  it('maps every network mutation route and preserves nullable effective dates', async () => {
    const json = (value: unknown) => new Response(JSON.stringify(value), { status: 200, headers: { 'Content-Type': 'application/json' } })
    const fetchMock = vi.fn().mockImplementation((url: string) => {
      if (url.includes('/contacts')) return Promise.resolve(json(url.endsWith('/contacts') ? [{ id: 'ct-1', companyId: 'nc-1', name: '김현수', department: '구매', position: '부장', businessRole: '구매', mobilePhone: '', email: '', validFrom: '2026-01-01', validTo: null, employmentStatus: 'Active', version: 1 }] : { id: 'ct-1', companyId: 'nc-1', name: '김현수', department: '구매', position: '부장', businessRole: '구매', mobilePhone: '', email: '', validFrom: '2026-01-01', validTo: null, employmentStatus: 'Active', version: 2 }))
      if (url.includes('/assignments')) return Promise.resolve(json({ id: 'as-2', companyId: 'nc-1', userId: 'u3', validFrom: '2026-10-10', validTo: null, isCurrent: true, version: 1 }))
      if (url.includes('/relationships')) { const row = { id: 'rel-1', upstreamCompanyId: 'oem-1', downstreamCompanyId: 'nc-1', validFrom: '2026-01-01', validTo: null, status: 'Active', version: 2, supplies: [] }; return Promise.resolve(json(url.includes('?companyId=') ? [row] : row)) }
      return Promise.resolve(json({ id: 'nc-1', companyName: '대성전자', country: '대한민국', industry: '전자', lifecycleType: 'Customer', isOem: false, customerId: 'c1', currentOwnerId: 'u2', status: 'Active', version: 2 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    const api = new HttpSalesApi('/base')
    const companyInput = { companyName: '대성전자', country: '대한민국', industry: '전자', lifecycleType: 'customer' as const, isOem: false, status: 'active' as const }
    const relationInput = { upstreamCompanyId: 'oem-1', downstreamCompanyId: 'nc-1', validFrom: '2026-01-01', validTo: undefined, status: 'active' as const, supplies: [] }
    const contactInput = { name: '김현수', department: '구매', position: '부장', businessRole: '구매', mobilePhone: '', email: '', validFrom: '2026-01-01', validTo: undefined, employmentStatus: 'active' as const }

    await api.getNetworkCompany('nc-1')
    await api.createNetworkCompany(companyInput)
    await api.disableNetworkCompany('nc-1', 2)
    await api.listCompanyRelationships('nc-1')
    await api.updateNetworkRelationship('rel-1', { ...relationInput, expectedVersion: 1 })
    await api.disableNetworkRelationship('rel-1', 2)
    const contacts = await api.listCompanyContacts('nc-1')
    await api.createCompanyContact('nc-1', contactInput)
    await api.updateCompanyContact('nc-1', 'ct-1', { ...contactInput, expectedVersion: 1 })
    await api.changeCompanyAssignment('nc-1', { userId: 'u3', effectiveDate: '2026-10-10', expectedVersion: 2 })

    expect(contacts[0].validTo).toBeNull()
    expect(fetchMock).toHaveBeenCalledWith('/base/api/network/companies/nc-1', expect.objectContaining({ credentials: 'include' }))
    expect(fetchMock).toHaveBeenCalledWith('/base/api/network/companies', expect.objectContaining({ method: 'POST' }))
    expect(fetchMock).toHaveBeenCalledWith('/base/api/network/companies/nc-1/disable', expect.objectContaining({ method: 'POST', body: JSON.stringify({ expectedVersion: 2 }) }))
    expect(fetchMock).toHaveBeenCalledWith('/base/api/network/relationships?companyId=nc-1', expect.objectContaining({ credentials: 'include' }))
    expect(fetchMock).toHaveBeenCalledWith('/base/api/network/relationships/rel-1', expect.objectContaining({ method: 'PUT', body: JSON.stringify({ ...relationInput, expectedVersion: 1 }) }))
    expect(fetchMock).toHaveBeenCalledWith('/base/api/network/relationships/rel-1/disable', expect.objectContaining({ method: 'POST', body: JSON.stringify({ expectedVersion: 2 }) }))
    expect(fetchMock).toHaveBeenCalledWith('/base/api/network/companies/nc-1/contacts', expect.objectContaining({ method: 'POST', body: JSON.stringify(contactInput) }))
    expect(fetchMock).toHaveBeenCalledWith('/base/api/network/companies/nc-1/contacts/ct-1', expect.objectContaining({ method: 'PUT', body: JSON.stringify({ ...contactInput, expectedVersion: 1 }) }))
    expect(fetchMock).toHaveBeenCalledWith('/base/api/network/companies/nc-1/assignments', expect.objectContaining({ method: 'POST', body: JSON.stringify({ userId: 'u3', effectiveDate: '2026-10-10', expectedVersion: 2 }) }))
  })
})
