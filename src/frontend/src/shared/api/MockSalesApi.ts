import type { SalesApi } from './SalesApi'
import type {
  AuditLog,
  Customer,
  DailyRate,
  DashboardQuery,
  DashboardSummary,
  ImportRequest,
  ImportValidation,
  LoginRequest,
  ListQuery,
  MonthlyRate,
  Price,
  Product,
  SalesTarget,
  Shipment,
  User,
} from './contracts'

export class MockSalesApi implements SalesApi {
  private currentUser: User | null
  private dashboardSummary: DashboardSummary
  private users: User[]
  private customers: Customer[]
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
