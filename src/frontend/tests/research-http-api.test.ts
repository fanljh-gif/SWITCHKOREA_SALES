import { afterEach, describe, expect, it, vi } from 'vitest'
import { HttpSalesApi } from '../src/shared/api/HttpSalesApi'

afterEach(() => vi.unstubAllGlobals())

function ok(body: unknown) {
  return new Response(JSON.stringify(body), { status: 200, headers: { 'Content-Type': 'application/json' } })
}

describe('HttpSalesApi research contract', () => {
  it('maps candidate filters and normalizes nested DTOs', async () => {
    const fetchMock = vi.fn().mockResolvedValue(ok([{ id: 'c1', companyName: 'Acme', country: 'US', region: '미국', industry: '전자', domain: 'acme.com', website: 'https://acme.com', products: [], applications: [], status: 'ReviewPending', researchRunId: 'r1', version: 1, evidence: [], duplicateMatches: [], score: { total: 70, priority: 'High' } }]))
    vi.stubGlobal('fetch', fetchMock)
    const rows = await new HttpSalesApi('/base').listProspectCandidates({ region: '미국', status: 'reviewPending' })
    expect(fetchMock).toHaveBeenCalledWith('/base/api/research/candidates?region=%EB%AF%B8%EA%B5%AD&status=reviewPending', expect.objectContaining({ credentials: 'include' }))
    expect(rows[0]).toMatchObject({ status: 'reviewPending', runId: 'r1', score: { priority: 'high' } })
  })

  it('sends expectedVersion for assign review approve and profile mutations', async () => {
    const fetchMock = vi.fn().mockImplementation(() => Promise.resolve(ok({ id: 'x', status: 'ReviewPending', evidence: [], duplicateMatches: [], score: {} })))
    vi.stubGlobal('fetch', fetchMock)
    const api = new HttpSalesApi()
    await api.assignProspectCandidate('c1', { ownerId: 'u2', expectedVersion: 3 })
    await api.reviewProspectCandidate('c1', { decision: 'onHold', expectedVersion: 4 })
    await api.convertProspectCandidate('c1', 5)
    await api.saveResearchTargetProfile({ id: 'p1', name: '기본', regions: [], industries: [], includeKeywords: [], excludeKeywords: [], maxSearchResults: 10, maxAiAnalyses: 5, isActive: true, version: 2, expectedVersion: 2 })
    expect(fetchMock.mock.calls.map((call) => [call[0], JSON.parse(call[1].body)])).toEqual([
      ['/api/research/candidates/c1/assign', { ownerId: 'u2', expectedVersion: 3 }],
      ['/api/research/candidates/c1/review', { decision: 'onHold', expectedVersion: 4 }],
      ['/api/research/candidates/c1/approve', { expectedVersion: 5 }],
      ['/api/research/profiles', expect.objectContaining({ id: 'p1', expectedVersion: 2 })],
    ])
  })
})
