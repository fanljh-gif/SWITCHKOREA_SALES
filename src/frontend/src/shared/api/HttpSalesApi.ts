import type { SalesApi } from './SalesApi'
import type { AuditLog, Customer, DailyRate, DashboardQuery, DashboardSummary, ImportRequest, ImportValidation, ListQuery, LoginRequest, MonthlyRate, Price, Product, SalesTarget, Shipment, User } from './contracts'

type Create<T> = Omit<T, 'id' | 'version'>
type Update<T> = Partial<Create<T>> & { version: number }
type Problem = { title?: string; detail?: string; status?: number }

export class SalesApiError extends Error {
  constructor(public readonly status: number, message: string, public readonly detail?: string) { super(message); this.name = 'SalesApiError' }
}

export class HttpSalesApi implements SalesApi {
  constructor(private readonly baseUrl = '') {}

  private async request<T>(path: string, init: RequestInit = {}): Promise<T> {
    const headers = new Headers(init.headers)
    if (init.body && !(init.body instanceof FormData)) headers.set('Content-Type', 'application/json')
    const response = await fetch(`${this.baseUrl}${path}`, { ...init, headers, credentials: 'include' })
    if (!response.ok) {
      const problem = await response.json().catch(() => ({})) as Problem
      throw new SalesApiError(response.status, problem.title ?? `요청 처리 실패 (${response.status})`, problem.detail)
    }
    if (response.status === 204) return undefined as T
    return response.json() as Promise<T>
  }

  private query(values: Record<string, string | undefined>) {
    const query = new URLSearchParams(Object.entries(values).filter((entry): entry is [string, string] => Boolean(entry[1])))
    return query.size ? `?${query}` : ''
  }

  private mapUser(raw: any): User { return { id: raw.id, loginId: raw.loginId, name: raw.name, email: raw.email, role: String(raw.role).toLowerCase() === 'manager' || raw.role === 0 ? 'manager' : 'member', status: String(raw.status).toLowerCase() === 'inactive' || raw.status === 1 ? 'inactive' : 'active', passwordChangeRequired: raw.passwordChangeRequired, lockedUntil: raw.lockedUntil } }
  private mapCustomer(raw: any): Customer { return { ...raw, ownerId: raw.ownerId ?? raw.ownerUserId, defaultCurrency: String(raw.defaultCurrency).toUpperCase(), status: String(raw.status).toLowerCase() === 'inactive' || raw.status === 1 ? 'inactive' : 'active' } }
  private mapProduct(raw: any): Product { return { ...raw, itemType: String(raw.itemType).toLowerCase(), status: String(raw.status).toLowerCase() === 'inactive' || raw.status === 1 ? 'inactive' : 'active' } }
  private mapShipment(raw: any): Shipment { return { ...raw, ownerId: raw.ownerId ?? raw.ownerUserId, currency: String(raw.currency).toUpperCase(), status: String(raw.status).toLowerCase() === 'cancelled' || raw.status === 1 ? 'cancelled' : 'confirmed' } }

  async getCurrentUser() { try { return this.mapUser(await this.request<any>('/api/auth/me')) } catch (error) { if (error instanceof SalesApiError && error.status === 401) return null; throw error } }
  async login(input: LoginRequest) { const result = await this.request<any>('/api/auth/login', { method: 'POST', body: JSON.stringify(input) }); return { user: this.mapUser(result.user), passwordChangeRequired: result.passwordChangeRequired } }
  logout() { return this.request<void>('/api/auth/logout', { method: 'POST' }) }
  changePassword(currentPassword: string, newPassword: string) { return this.request<void>('/api/auth/change-password', { method: 'POST', body: JSON.stringify({ currentPassword, newPassword }) }) }

  async listUsers(query?: ListQuery) { const users = await this.request<any[]>('/api/admin/users'); return users.map(x => this.mapUser(x)).filter(x => (!query?.search || `${x.loginId} ${x.name} ${x.email}`.toLowerCase().includes(query.search.toLowerCase())) && (!query?.status || x.status === query.status)) }
  async getUser(id: string) { const user = (await this.listUsers()).find(x => x.id === id); if (!user) throw new SalesApiError(404, '사용자를 찾을 수 없습니다.'); return user }
  async createUser(input: Create<User>) { const temporary = (input as Create<User> & { initialPassword?: string }).initialPassword ?? crypto.randomUUID(); return this.mapUser(await this.request<any>('/api/admin/users', { method: 'POST', body: JSON.stringify({ loginId: input.loginId, initialPassword: temporary, name: input.name, email: input.email, role: input.role }) })) }
  async updateUser(id: string, input: Update<User>) { if (input.role) await this.request(`/api/admin/users/${id}/role`, { method: 'PUT', body: JSON.stringify({ role: input.role, expectedVersion: input.version }) }); if (input.status) await this.request(`/api/admin/users/${id}/status`, { method: 'PUT', body: JSON.stringify({ status: input.status, expectedVersion: input.version }) }); return this.getUser(id) }
  cancelUser(id: string, version: number) { return this.request<void>(`/api/admin/users/${id}/status`, { method: 'PUT', body: JSON.stringify({ status: 'inactive', expectedVersion: version }) }) }
  async listAuditLogs(query?: ListQuery) { const rows = await this.request<any[]>(`/api/admin/audit${this.query({ target: query?.search ?? '' })}`); return rows.map((x): AuditLog => ({ id: x.id, actorName: x.actorName ?? x.actorUserId, occurredAt: x.createdAt, target: x.target, action: x.action, before: x.beforeJson, after: x.afterJson })) }

  async listCustomers(query?: ListQuery) { const rows = (await this.request<any[]>('/api/customers')).map(x => this.mapCustomer(x)); return rows.filter(x => (!query?.search || `${x.code} ${x.companyName}`.toLowerCase().includes(query.search.toLowerCase())) && (!query?.status || x.status === query.status)) }
  async getCustomer(id: string) { const value = (await this.listCustomers()).find(x => x.id === id); if (!value) throw new SalesApiError(404, '거래처를 찾을 수 없습니다.'); return value }
  createCustomer(input: Create<Customer>) { return this.saveCustomer(undefined, input, 0) }
  updateCustomer(id: string, input: Update<Customer>) { return this.saveCustomer(id, input as Create<Customer>, input.version) }
  private async saveCustomer(id: string | undefined, input: Create<Customer>, version: number) { return this.mapCustomer(await this.request<any>('/api/customers', { method: 'POST', body: JSON.stringify({ command: { id, code: input.code, companyName: input.companyName, country: input.country, defaultCurrency: input.defaultCurrency, ownerUserId: input.ownerId, status: input.status }, expectedVersion: version }) })) }

  async listProducts(query?: ListQuery) { const rows = (await this.request<any[]>('/api/products')).map(x => this.mapProduct(x)); return rows.filter(x => (!query?.search || `${x.code} ${x.name}`.toLowerCase().includes(query.search.toLowerCase())) && (!query?.status || x.status === query.status)) }
  async getProduct(id: string) { const value = (await this.listProducts()).find(x => x.id === id); if (!value) throw new SalesApiError(404, '품목을 찾을 수 없습니다.'); return value }
  createProduct(input: Create<Product>) { return this.saveProduct(undefined, input, 0) }
  updateProduct(id: string, input: Update<Product>) { return this.saveProduct(id, input as Create<Product>, input.version) }
  private async saveProduct(id: string | undefined, input: Create<Product>, version: number) { return this.mapProduct(await this.request<any>('/api/products', { method: 'POST', body: JSON.stringify({ command: { id, ...input }, expectedVersion: version }) })) }

  async listPrices(customerId?: string) { const rows = await this.request<Price[]>('/api/prices'); return customerId ? rows.filter(x => x.customerId === customerId) : rows }
  createPrice(input: Create<Price>) { return this.savePrice(undefined, input, 0) }
  updatePrice(id: string, input: Update<Price>) { return this.savePrice(id, input as Create<Price>, input.version) }
  private savePrice(id: string | undefined, input: Create<Price>, version: number) { return this.request<Price>('/api/prices', { method: 'POST', body: JSON.stringify({ command: { id, ...input }, expectedVersion: version }) }) }

  async listShipments(query?: ListQuery) { return (await this.request<any[]>('/api/shipments')).map(x => this.mapShipment(x)).filter(x => !query?.search || `${x.shipmentNumber ?? ''}`.toLowerCase().includes(query.search.toLowerCase())) }
  async getShipment(id: string) { const value = (await this.listShipments()).find(x => x.id === id); if (!value) throw new SalesApiError(404, '출고를 찾을 수 없습니다.'); return value }
  async createShipment(input: Create<Shipment>) { const result = await this.request<any>('/api/shipments', { method: 'POST', body: JSON.stringify({ shipmentNumber: input.shipmentNumber, shipmentDate: input.shipmentDate, customerId: input.customerId, productId: input.productId, quantity: input.quantity }) }); return this.mapShipment(result.shipment) }
  updateShipment(id: string, input: Update<Shipment>) { return this.request<any>(`/api/shipments/${id}`, { method: 'PUT', body: JSON.stringify(input) }).then(x => this.mapShipment(x)) }
  cancelShipment(id: string, version: number) { return this.request<void>(`/api/shipments/${id}/cancel`, { method: 'POST', body: JSON.stringify({ expectedVersion: version }) }) }

  async listMonthlyRates(yearMonth: string) { const [year, month] = yearMonth.split('-'); return this.request<MonthlyRate[]>(`/api/rates/monthly?year=${year}&month=${Number(month)}`) }
  async updateMonthlyRate(input: MonthlyRate) { const [year, month] = input.yearMonth.split('-'); return this.request<MonthlyRate>('/api/rates/monthly', { method: 'PUT', body: JSON.stringify({ year: Number(year), month: Number(month), currency: input.currency, rate: input.rate, expectedVersion: 0 }) }) }
  listDailyRates(dateFrom: string, dateTo: string) { return this.request<DailyRate[]>(`/api/rates/daily?from=${dateFrom}&to=${dateTo}`) }
  async listSalesTargets(yearMonth: string) { const [year, month] = yearMonth.split('-'); return this.request<SalesTarget[]>(`/api/targets?year=${year}&month=${Number(month)}`) }
  async updateSalesTarget(input: SalesTarget) { const [year, month] = input.yearMonth.split('-'); return this.request<SalesTarget>('/api/targets', { method: 'PUT', body: JSON.stringify({ year: Number(year), month: Number(month), userId: input.userId, targetKrw: input.targetKrw, expectedVersion: input.version }) }) }
  async getDashboardSummary(query: DashboardQuery) { const raw = await this.request<any>(`/api/dashboard${this.query({ from: query.dateFrom, to: query.dateTo, ownerId: query.ownerId, customerId: query.customerId, productId: query.productId })}`); return { todaySalesKrw: raw.todaySalesKrw ?? 0, monthSalesKrw: raw.krwTotal, monthTargetKrw: raw.targetKrw, achievementPercent: raw.achievementRate ?? 0, currencySales: raw.byCurrency, ownerSales: raw.byOwner, customerSales: raw.byCustomer, productSales: raw.byProduct } as DashboardSummary }

  async validateImport(input: ImportRequest) { const form = new FormData(); form.append('file', input.file); form.append('columnMap', JSON.stringify(input.columnMap)); const raw = await this.request<any>(`/api/imports/validate?kind=${input.kind}`, { method: 'POST', body: form }); const counts = (status: string) => raw.rows.filter((x: any) => String(x.status).toLowerCase() === status).length; const result: ImportValidation = { token: raw.batchId, newCount: counts('new'), duplicateCount: counts('duplicate'), changeCandidateCount: counts('changecandidate'), errorCount: raw.errors.length + counts('invalid'), errors: [...raw.errors, ...raw.rows.flatMap((x: any) => x.errors)].map((x: any) => ({ row: x.rowNumber ?? 0, message: x.message })), canCommit: raw.errors.length === 0 && counts('invalid') === 0 }; return result }
  commitImport(token: string) { return this.request<void>(`/api/imports/${token}/commit`, { method: 'POST' }) }
}
