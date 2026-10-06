import type { SalesApi } from './SalesApi'
import { SalesApiError } from './HttpSalesApi'
import type {
  AuditLog,
  AssignmentChangeInput,
  CompanyAssignment,
  CompanyContact,
  CompanyRelationship,
  ContactInput,
  Customer,
  DailyRate,
  DashboardQuery,
  DashboardSummary,
  ImportRequest,
  ImportValidation,
  LoginRequest,
  ListQuery,
  MonthlyRate,
  NetworkCompany,
  NetworkCompanyInput,
  NetworkQuery,
  NetworkTreeNode,
  Price,
  Product,
  ProspectConversionInput,
  RelationshipInput,
  SalesTarget,
  Shipment,
  User,
} from './contracts'

export class MockSalesApi implements SalesApi {
  private currentUser: User | null
  private dashboardSummary: DashboardSummary
  private users: User[]
  private customers: Customer[]
  private networkCompanies: NetworkCompany[]
  private networkRelationships: CompanyRelationship[]
  private companyContacts: CompanyContact[]
  private companyAssignments: CompanyAssignment[]
  private products: Product[]
  private prices: Price[]
  private shipments: Shipment[]
  private monthlyRates: MonthlyRate[]
  private dailyRates: DailyRate[]
  private salesTargets: SalesTarget[]
  private auditLogs: AuditLog[]
  private importValidation: ImportValidation

  constructor(options: {
    currentUser?: User | null
    dashboardSummary?: DashboardSummary
    importValidation?: ImportValidation
  } = {}) {
    const manager: User = { id: 'u1', loginId: 'jhlee', name: '이재학', email: 'jhlee@switchkorea.com', role: 'manager', status: 'active' }
    this.currentUser = options.currentUser === undefined ? manager : options.currentUser
    this.dashboardSummary = options.dashboardSummary ?? {
      todaySalesKrw: 4_800_000,
      monthSalesKrw: 87_500_000,
      monthTargetKrw: 100_000_000,
      achievementPercent: 87.5,
      currencySales: [
        { currency: 'KRW', originalAmount: 35_087_500, krwAmount: 35_087_500, sharePercent: 40.1 },
        { currency: 'USD', originalAmount: 28_907.41, krwAmount: 39_025_000, sharePercent: 44.6 },
        { currency: 'CNY', originalAmount: 71_210.11, krwAmount: 13_387_500, sharePercent: 15.3 },
      ],
      ownerSales: [
        { id: 'u2', name: '박태원', krwAmount: 24_500_000 },
        { id: 'u3', name: '최명철', krwAmount: 21_000_000 },
        { id: 'u4', name: '이소연', krwAmount: 18_000_000 },
        { id: 'u5', name: '이창선', krwAmount: 15_000_000 },
        { id: 'u1', name: '이재학', krwAmount: 9_000_000 },
      ],
      customerSales: [{ id: 'c1', name: '대한전자', krwAmount: 35_087_500 }, { id: 'c2', name: '글로벌테크', krwAmount: 39_025_000 }],
      productSales: [{ id: 'p1', name: '마이크로 스위치', krwAmount: 74_112_500 }, { id: 'p2', name: '스위치 보호캡', krwAmount: 13_387_500 }],
    }
    this.users = [
      manager,
      { id: 'u2', loginId: 'ptw', name: '박태원', email: 'ptw@switchkorea.com', role: 'member', status: 'active' },
      { id: 'u3', loginId: 'mcc', name: '최명철', email: 'mcc@switchkorea.com', role: 'member', status: 'active' },
      { id: 'u4', loginId: 'sylee', name: '이소연', email: 'sylee@switchkorea.com', role: 'member', status: 'active' },
      { id: 'u5', loginId: 'cslee', name: '이창선', email: 'cslee@switchkorea.com', role: 'member', status: 'active' },
    ]
    this.customers = [
      { id: 'c1', code: 'CUS-001', companyName: '대한전자', country: '대한민국', defaultCurrency: 'KRW', ownerId: 'u2', status: 'active', version: 1 },
      { id: 'c2', code: 'CUS-002', companyName: '글로벌테크', country: '미국', defaultCurrency: 'USD', ownerId: 'u3', status: 'active', version: 1 },
    ]
    this.products = [
      { id: 'p1', code: 'SW-100', name: '마이크로 스위치', itemType: 'product', specification: '10A 250VAC', status: 'active', version: 1 },
      { id: 'p2', code: 'ACC-20', name: '스위치 보호캡', itemType: 'goods', specification: '실리콘', status: 'active', version: 1 },
    ]
    this.networkCompanies = [
      { id: 'nc-samsung', companyName: '삼성전자', country: '대한민국', industry: '전자', lifecycleType: 'customer', isOem: true, customerId: null, currentOwnerId: 'u1', status: 'active', version: 1 },
      { id: 'nc-lg', companyName: 'LG전자', country: '대한민국', industry: '전자', lifecycleType: 'customer', isOem: true, customerId: null, currentOwnerId: 'u1', status: 'active', version: 1 },
      { id: 'nc-shared', companyName: '대한전자', country: '대한민국', industry: '전자부품', lifecycleType: 'customer', isOem: false, customerId: 'c1', currentOwnerId: 'u2', status: 'active', version: 1 },
      { id: 'nc-prospect', companyName: '새빛정밀', country: '대한민국', industry: '정밀가공', lifecycleType: 'prospect', isOem: false, customerId: null, currentOwnerId: 'u3', status: 'active', version: 1 },
    ]
    this.networkRelationships = [
      { id: 'nr-samsung-shared', upstreamCompanyId: 'nc-samsung', downstreamCompanyId: 'nc-shared', validFrom: '2025-01-01', status: 'active', version: 1, supplies: [{ id: 'ns-1', relationshipId: 'nr-samsung-shared', productId: 'p1', applicationName: '생활가전 조작부' }] },
      { id: 'nr-lg-shared', upstreamCompanyId: 'nc-lg', downstreamCompanyId: 'nc-shared', validFrom: '2025-03-01', status: 'active', version: 1, supplies: [{ id: 'ns-2', relationshipId: 'nr-lg-shared', productId: 'p1', applicationName: '공조기' }] },
      { id: 'nr-shared-prospect', upstreamCompanyId: 'nc-shared', downstreamCompanyId: 'nc-prospect', validFrom: '2026-02-01', status: 'active', version: 1, supplies: [{ id: 'ns-3', relationshipId: 'nr-shared-prospect', productId: 'p2', applicationName: '조작부 ASSY' }] },
    ]
    this.companyContacts = [
      { id: 'nct-prospect-1', companyId: 'nc-prospect', name: '김현수', department: '구매팀', position: '부장', businessRole: '구매 의사결정', mobilePhone: '010-0000-0000', email: 'buyer@example.com', validFrom: '2026-01-01', employmentStatus: 'active', version: 1 },
    ]
    this.companyAssignments = [
      { id: 'na-samsung', companyId: 'nc-samsung', userId: 'u1', validFrom: '2025-01-01', isCurrent: true, version: 1 },
      { id: 'na-lg', companyId: 'nc-lg', userId: 'u1', validFrom: '2025-01-01', isCurrent: true, version: 1 },
      { id: 'na-shared', companyId: 'nc-shared', userId: 'u2', validFrom: '2025-01-01', isCurrent: true, version: 1 },
      { id: 'na-prospect', companyId: 'nc-prospect', userId: 'u3', validFrom: '2026-01-01', isCurrent: true, version: 1 },
    ]
    this.prices = [
      { id: 'pr1', customerId: 'c1', productId: 'p1', currency: 'KRW', unitPrice: 3200, validFrom: '2026-10-01', version: 1 },
      { id: 'pr2', customerId: 'c2', productId: 'p1', currency: 'USD', unitPrice: 2.5, validFrom: '2026-10-01', version: 1 },
    ]
    this.shipments = []
    this.monthlyRates = [
      { yearMonth: '2026-10', currency: 'USD', rate: 1350, updatedBy: 'u1', updatedAt: '2026-10-01T09:00:00+09:00' },
      { yearMonth: '2026-10', currency: 'CNY', rate: 188, updatedBy: 'u1', updatedAt: '2026-10-01T09:00:00+09:00' },
    ]
    this.dailyRates = [
      { date: '2026-10-01', currency: 'USD', rate: 1348.5, fetchedAt: '2026-10-01T16:30:00+09:00', status: 'success' },
      { date: '2026-10-01', currency: 'CNY', rate: 187.8, fetchedAt: '2026-10-01T16:30:00+09:00', status: 'success' },
    ]
    this.salesTargets = this.users.map((user, index) => ({ yearMonth: '2026-10', userId: user.id, targetKrw: index === 0 ? 0 : 50_000_000, version: 1 }))
    this.auditLogs = [{ id: 'a1', actorName: '이재학', occurredAt: '2026-10-01 09:00', target: '적용환율', action: '수정', after: 'USD 1,350 / CNY 188' }]
    this.importValidation = options.importValidation ?? { token: 'demo-import', newCount: 0, duplicateCount: 0, changeCandidateCount: 0, errorCount: 0, errors: [], canCommit: true }
  }

  async getCurrentUser(): Promise<User | null> { return this.currentUser }
  async login(input: LoginRequest): Promise<{ user: User; passwordChangeRequired: boolean }> {
    if (!input.loginId || !input.password) throw new Error('아이디와 비밀번호를 확인해 주세요.')
    if (!this.currentUser || this.currentUser.loginId !== input.loginId) throw new Error('아이디 또는 비밀번호가 올바르지 않습니다.')
    if (this.currentUser.lockedUntil) throw new Error(`계정이 ${this.currentUser.lockedUntil}까지 잠겼습니다.`)
    return { user: this.currentUser, passwordChangeRequired: Boolean(this.currentUser.passwordChangeRequired) }
  }
  async logout(): Promise<void> { this.currentUser = null }
  async changePassword(_currentPassword: string, newPassword: string): Promise<void> {
    if (newPassword.length < 8) throw new Error('새 비밀번호는 8자 이상이어야 합니다.')
    if (this.currentUser) this.currentUser = { ...this.currentUser, passwordChangeRequired: false }
  }

  async listUsers(_query?: ListQuery): Promise<User[]> { return [...this.users] }
  async getUser(id: string): Promise<User> { const row = this.users.find((item) => item.id === id); if (!row) throw new Error('User not found'); return row }
  async createUser(input: Omit<User, 'id' | 'version'>): Promise<User> { const row = { ...input, id: crypto.randomUUID() }; this.users.push(row); return row }
  async updateUser(id: string, input: Partial<Omit<User, 'id' | 'version'>> & { version: number }): Promise<User> { const row = await this.getUser(id); Object.assign(row, input); return row }
  async cancelUser(id: string, _version: number): Promise<void> { const row = await this.getUser(id); row.status = 'inactive' }
  async listAuditLogs(_query?: ListQuery): Promise<AuditLog[]> { return [...this.auditLogs] }

  async listCustomers(_query?: ListQuery): Promise<Customer[]> { return [...this.customers] }
  async getCustomer(id: string): Promise<Customer> { const row = this.customers.find((item) => item.id === id); if (!row) throw new Error('Customer not found'); return row }
  async createCustomer(input: Omit<Customer, 'id' | 'version'>): Promise<Customer> { const row = { ...input, id: crypto.randomUUID(), version: 1 }; this.customers.push(row); return row }
  async updateCustomer(id: string, input: Partial<Omit<Customer, 'id' | 'version'>> & { version: number }): Promise<Customer> { const row = await this.getCustomer(id); Object.assign(row, input, { version: row.version + 1 }); return row }

  private requireNetworkCompany(id: string): NetworkCompany {
    const row = this.networkCompanies.find((item) => item.id === id)
    if (!row) throw new SalesApiError(404, '영업망 회사를 찾을 수 없습니다.')
    return row
  }

  private requireNetworkEdit(company: NetworkCompany) {
    if (!this.currentUser) throw new SalesApiError(401, '로그인이 필요합니다.')
    if (this.currentUser.role !== 'manager' && company.currentOwnerId !== this.currentUser.id) throw new SalesApiError(403, '다른 담당자의 회사는 수정할 수 없습니다.')
  }

  private requireNetworkVersion(actual: number, expected: number) {
    if (actual !== expected) throw new SalesApiError(409, '다른 사용자가 먼저 수정했습니다.')
  }

  private requireRelationshipEdit(upstreamCompanyId: string, downstreamCompanyId: string) {
    if (!this.currentUser) throw new SalesApiError(401, '로그인이 필요합니다.')
    const upstream = this.requireNetworkCompany(upstreamCompanyId)
    const downstream = this.requireNetworkCompany(downstreamCompanyId)
    if (this.currentUser.role === 'manager') return
    if (upstream.currentOwnerId !== this.currentUser.id && downstream.currentOwnerId !== this.currentUser.id) {
      throw new SalesApiError(403, '담당 회사가 포함된 관계만 수정할 수 있습니다.')
    }
  }

  async listNetworkCompanies(query?: NetworkQuery): Promise<NetworkCompany[]> {
    let allowedIds: Set<string> | undefined
    if (query?.rootCompanyId) {
      const collect = (nodes: NetworkTreeNode[]) => nodes.forEach((node) => { allowedIds!.add(node.company.id); collect(node.children) })
      allowedIds = new Set<string>()
      collect(await this.getNetworkTree({ rootCompanyId: query.rootCompanyId }))
    }
    return this.networkCompanies.filter((row) => {
      if (allowedIds && !allowedIds.has(row.id)) return false
      if (query?.lifecycleType === 'oem' && !row.isOem) return false
      if (query?.lifecycleType && query.lifecycleType !== 'oem' && (row.isOem || row.lifecycleType !== query.lifecycleType)) return false
      if (query?.ownerId && row.currentOwnerId !== query.ownerId) return false
      if (query?.status && row.status !== query.status) return false
      return !query?.search || `${row.companyName} ${row.country} ${row.industry}`.toLowerCase().includes(query.search.toLowerCase())
    })
  }

  async getNetworkCompany(id: string): Promise<NetworkCompany> { return this.requireNetworkCompany(id) }

  async createNetworkCompany(input: NetworkCompanyInput): Promise<NetworkCompany> {
    if (!this.currentUser) throw new SalesApiError(401, '로그인이 필요합니다.')
    if (this.currentUser.role === 'member' && (input.lifecycleType !== 'prospect' || input.isOem)) throw new SalesApiError(403, '팀원은 잠재 협력사만 등록할 수 있습니다.')
    if (input.lifecycleType === 'customer' && !input.isOem) throw new SalesApiError(422, '정식 거래처는 잠재 협력사 전환 또는 기존 거래처 연결로 등록해 주세요.')
    const ownerId = this.currentUser.role === 'member' ? this.currentUser.id : (input.ownerId ?? this.currentUser.id)
    const row: NetworkCompany = { id: crypto.randomUUID(), companyName: input.companyName, country: input.country, industry: input.industry, lifecycleType: input.lifecycleType, isOem: input.isOem, customerId: null, currentOwnerId: ownerId, status: input.status, version: 1 }
    this.networkCompanies.push(row)
    this.companyAssignments.push({ id: crypto.randomUUID(), companyId: row.id, userId: ownerId, validFrom: '2026-10-06', isCurrent: true, version: 1 })
    return row
  }

  async updateNetworkCompany(id: string, input: NetworkCompanyInput & { expectedVersion: number }): Promise<NetworkCompany> {
    const row = this.requireNetworkCompany(id)
    this.requireNetworkEdit(row)
    this.requireNetworkVersion(row.version, input.expectedVersion)
    if (row.lifecycleType !== input.lifecycleType || row.isOem !== input.isOem) {
      if (this.currentUser?.role === 'member') throw new SalesApiError(403, '팀원은 회사 분류를 변경할 수 없습니다.')
      throw new SalesApiError(422, '회사 분류는 전용 등록·전환 기능을 사용해 주세요.')
    }
    Object.assign(row, { companyName: input.companyName, country: input.country, industry: input.industry, lifecycleType: input.lifecycleType, isOem: input.isOem, status: input.status, version: row.version + 1 })
    return row
  }

  async disableNetworkCompany(id: string, expectedVersion: number): Promise<void> {
    const row = this.requireNetworkCompany(id)
    this.requireNetworkEdit(row)
    this.requireNetworkVersion(row.version, expectedVersion)
    row.status = 'inactive'
    row.version += 1
  }

  async convertNetworkCompany(id: string, input: ProspectConversionInput): Promise<NetworkCompany> {
    const row = this.requireNetworkCompany(id)
    if (this.currentUser?.role !== 'manager') throw new SalesApiError(403, '팀장만 정식 거래처로 전환할 수 있습니다.')
    this.requireNetworkVersion(row.version, input.expectedVersion)
    if (row.lifecycleType !== 'prospect') throw new SalesApiError(422, '잠재 협력사만 전환할 수 있습니다.')
    const customer = await this.createCustomer({ code: input.customerCode, companyName: row.companyName, country: row.country, defaultCurrency: input.defaultCurrency, ownerId: row.currentOwnerId, status: row.status })
    row.customerId = customer.id
    row.lifecycleType = 'customer'
    row.version += 1
    return row
  }

  async getNetworkTree(query: NetworkQuery): Promise<NetworkTreeNode[]> {
    const roots = query.rootCompanyId ? [this.requireNetworkCompany(query.rootCompanyId)] : this.networkCompanies.filter((row) => row.isOem)
    const build = (company: NetworkCompany, depth: number, path: string[]): NetworkTreeNode => {
      const pathCompanyIds = [...path, company.id]
      const children = this.networkRelationships
        .filter((row) => row.upstreamCompanyId === company.id && row.status === 'active' && !pathCompanyIds.includes(row.downstreamCompanyId))
        .map((row) => build(this.requireNetworkCompany(row.downstreamCompanyId), depth + 1, pathCompanyIds))
      return { company, depth, pathCompanyIds, children }
    }
    const matches = (company: NetworkCompany) => {
      if (query.lifecycleType === 'oem' && !company.isOem) return false
      if (query.lifecycleType && query.lifecycleType !== 'oem' && (company.isOem || company.lifecycleType !== query.lifecycleType)) return false
      if (query.ownerId && company.currentOwnerId !== query.ownerId) return false
      if (query.status && company.status !== query.status) return false
      return !query.search || `${company.companyName} ${company.country} ${company.industry}`.toLowerCase().includes(query.search.toLowerCase())
    }
    const hasFilters = Boolean(query.lifecycleType || query.ownerId || query.status || query.search)
    if (!hasFilters) return roots.map((root) => build(root, 0, []))

    const prune = (node: NetworkTreeNode): NetworkTreeNode | null => {
      const children = node.children.map(prune).filter((child): child is NetworkTreeNode => child !== null)
      return matches(node.company) || children.length > 0 ? { ...node, children } : null
    }
    return roots.map((root) => prune(build(root, 0, []))).filter((node): node is NetworkTreeNode => node !== null)
  }

  async listCompanyRelationships(companyId?: string): Promise<CompanyRelationship[]> {
    return this.networkRelationships.filter((row) => !companyId || row.upstreamCompanyId === companyId || row.downstreamCompanyId === companyId)
  }

  async createNetworkRelationship(input: RelationshipInput): Promise<CompanyRelationship> {
    const upstream = this.requireNetworkCompany(input.upstreamCompanyId)
    const downstream = this.requireNetworkCompany(input.downstreamCompanyId)
    this.requireRelationshipEdit(upstream.id, downstream.id)
    if (upstream.id === downstream.id) throw new SalesApiError(422, '자기 자신과 연결할 수 없습니다.')
    const rowId = crypto.randomUUID()
    const row: CompanyRelationship = { id: rowId, upstreamCompanyId: upstream.id, downstreamCompanyId: downstream.id, validFrom: input.validFrom, validTo: input.validTo, notes: input.notes, status: input.status, version: 1, supplies: input.supplies.map((supply) => ({ ...supply, id: crypto.randomUUID(), relationshipId: rowId })) }
    this.networkRelationships.push(row)
    return row
  }

  async updateNetworkRelationship(id: string, input: RelationshipInput & { expectedVersion: number }): Promise<CompanyRelationship> {
    const row = this.networkRelationships.find((item) => item.id === id)
    if (!row) throw new SalesApiError(404, '관계를 찾을 수 없습니다.')
    this.requireRelationshipEdit(row.upstreamCompanyId, row.downstreamCompanyId)
    this.requireRelationshipEdit(input.upstreamCompanyId, input.downstreamCompanyId)
    this.requireNetworkVersion(row.version, input.expectedVersion)
    const { expectedVersion: _expectedVersion, ...changes } = input
    Object.assign(row, changes, { version: row.version + 1, supplies: input.supplies.map((supply) => ({ ...supply, id: crypto.randomUUID(), relationshipId: id })) })
    return row
  }

  async disableNetworkRelationship(id: string, expectedVersion: number): Promise<void> {
    const row = this.networkRelationships.find((item) => item.id === id)
    if (!row) throw new SalesApiError(404, '관계를 찾을 수 없습니다.')
    this.requireRelationshipEdit(row.upstreamCompanyId, row.downstreamCompanyId)
    this.requireNetworkVersion(row.version, expectedVersion)
    row.status = 'inactive'; row.version += 1
  }

  async listCompanyContacts(companyId: string): Promise<CompanyContact[]> { return this.companyContacts.filter((row) => row.companyId === companyId) }
  async createCompanyContact(companyId: string, input: ContactInput): Promise<CompanyContact> { const company = this.requireNetworkCompany(companyId); this.requireNetworkEdit(company); const row = { ...input, id: crypto.randomUUID(), companyId, version: 1 }; this.companyContacts.push(row); return row }
  async updateCompanyContact(companyId: string, contactId: string, input: ContactInput & { expectedVersion: number }): Promise<CompanyContact> { const company = this.requireNetworkCompany(companyId); this.requireNetworkEdit(company); const row = this.companyContacts.find((item) => item.id === contactId && item.companyId === companyId); if (!row) throw new SalesApiError(404, '담당자를 찾을 수 없습니다.'); this.requireNetworkVersion(row.version, input.expectedVersion); const { expectedVersion: _expectedVersion, ...changes } = input; Object.assign(row, changes, { version: row.version + 1 }); return row }

  async listCompanyAssignments(companyId: string): Promise<CompanyAssignment[]> { return this.companyAssignments.filter((row) => row.companyId === companyId) }
  async changeCompanyAssignment(companyId: string, input: AssignmentChangeInput): Promise<CompanyAssignment> {
    if (this.currentUser?.role !== 'manager') throw new SalesApiError(403, '팀장만 담당자를 변경할 수 있습니다.')
    const company = this.requireNetworkCompany(companyId)
    this.requireNetworkVersion(company.version, input.expectedVersion)
    const current = this.companyAssignments.find((row) => row.companyId === companyId && row.isCurrent)
    if (current) {
      const previousDay = new Date(`${input.effectiveDate}T00:00:00Z`)
      previousDay.setUTCDate(previousDay.getUTCDate() - 1)
      current.isCurrent = false
      current.validTo = previousDay.toISOString().slice(0, 10)
      current.version += 1
    }
    const next = { id: crypto.randomUUID(), companyId, userId: input.userId, validFrom: input.effectiveDate, isCurrent: true, version: 1 }
    this.companyAssignments.push(next)
    company.currentOwnerId = input.userId; company.version += 1
    return next
  }

  async listProducts(_query?: ListQuery): Promise<Product[]> { return [...this.products] }
  async getProduct(id: string): Promise<Product> { const row = this.products.find((item) => item.id === id); if (!row) throw new Error('Product not found'); return row }
  async createProduct(input: Omit<Product, 'id' | 'version'>): Promise<Product> { const row = { ...input, id: crypto.randomUUID(), version: 1 }; this.products.push(row); return row }
  async updateProduct(id: string, input: Partial<Omit<Product, 'id' | 'version'>> & { version: number }): Promise<Product> { const row = await this.getProduct(id); Object.assign(row, input, { version: row.version + 1 }); return row }

  async listPrices(customerId?: string): Promise<Price[]> { return this.prices.filter((row) => !customerId || row.customerId === customerId) }
  async createPrice(input: Omit<Price, 'id' | 'version'>): Promise<Price> { const row = { ...input, id: crypto.randomUUID(), version: 1 }; this.prices.push(row); return row }
  async updatePrice(id: string, input: Partial<Omit<Price, 'id' | 'version'>> & { version: number }): Promise<Price> { const row = this.prices.find((item) => item.id === id); if (!row) throw new Error('Price not found'); Object.assign(row, input, { version: row.version + 1 }); return row }

  async listShipments(_query?: ListQuery): Promise<Shipment[]> { return [...this.shipments] }
  async getShipment(id: string): Promise<Shipment> { const row = this.shipments.find((item) => item.id === id); if (!row) throw new Error('Shipment not found'); return row }
  async createShipment(input: Omit<Shipment, 'id' | 'version'>): Promise<Shipment> { const row = { ...input, id: crypto.randomUUID(), version: 1 }; this.shipments.push(row); return row }
  async updateShipment(id: string, input: Partial<Omit<Shipment, 'id' | 'version'>> & { version: number }): Promise<Shipment> { const row = await this.getShipment(id); Object.assign(row, input, { version: row.version + 1 }); return row }
  async cancelShipment(id: string, _version: number): Promise<void> { const row = await this.getShipment(id); row.status = 'cancelled' }

  async listMonthlyRates(yearMonth: string): Promise<MonthlyRate[]> { return this.monthlyRates.filter((row) => row.yearMonth === yearMonth) }
  async updateMonthlyRate(input: MonthlyRate): Promise<MonthlyRate> { const index = this.monthlyRates.findIndex((row) => row.yearMonth === input.yearMonth && row.currency === input.currency); if (index >= 0) this.monthlyRates[index] = input; else this.monthlyRates.push(input); return input }
  async listDailyRates(_dateFrom: string, _dateTo: string): Promise<DailyRate[]> { return [...this.dailyRates] }
  async listSalesTargets(yearMonth: string): Promise<SalesTarget[]> { return this.salesTargets.filter((row) => row.yearMonth === yearMonth) }
  async updateSalesTarget(input: SalesTarget): Promise<SalesTarget> { const index = this.salesTargets.findIndex((row) => row.yearMonth === input.yearMonth && row.userId === input.userId); if (index >= 0) this.salesTargets[index] = input; else this.salesTargets.push(input); return input }
  async getDashboardSummary(_query: DashboardQuery): Promise<DashboardSummary> {
    return this.dashboardSummary
  }

  async validateImport(_input: ImportRequest): Promise<ImportValidation> { return this.importValidation }
  async commitImport(_token: string): Promise<void> {}
}
