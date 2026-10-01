import type { SalesApi } from './SalesApi'
import type {
  Customer,
  DailyRate,
  DashboardQuery,
  DashboardSummary,
  ImportRequest,
  ImportValidation,
  ListQuery,
  MonthlyRate,
  Price,
  Product,
  SalesTarget,
  Shipment,
  User,
} from './contracts'

export class MockSalesApi implements SalesApi {
  async listUsers(_query?: ListQuery): Promise<User[]> { return [] }
  async getUser(_id: string): Promise<User> { throw new Error('User not found') }
  async createUser(input: Omit<User, 'id' | 'version'>): Promise<User> { return { ...input, id: crypto.randomUUID() } }
  async updateUser(_id: string, _input: Partial<Omit<User, 'id' | 'version'>> & { version: number }): Promise<User> { throw new Error('User not found') }
  async cancelUser(_id: string, _version: number): Promise<void> {}

  async listCustomers(_query?: ListQuery): Promise<Customer[]> { return [] }
  async getCustomer(_id: string): Promise<Customer> { throw new Error('Customer not found') }
  async createCustomer(input: Omit<Customer, 'id' | 'version'>): Promise<Customer> { return { ...input, id: crypto.randomUUID(), version: 1 } }
  async updateCustomer(_id: string, _input: Partial<Omit<Customer, 'id' | 'version'>> & { version: number }): Promise<Customer> { throw new Error('Customer not found') }

  async listProducts(_query?: ListQuery): Promise<Product[]> { return [] }
  async getProduct(_id: string): Promise<Product> { throw new Error('Product not found') }
  async createProduct(input: Omit<Product, 'id' | 'version'>): Promise<Product> { return { ...input, id: crypto.randomUUID(), version: 1 } }
  async updateProduct(_id: string, _input: Partial<Omit<Product, 'id' | 'version'>> & { version: number }): Promise<Product> { throw new Error('Product not found') }

  async listPrices(_customerId?: string): Promise<Price[]> { return [] }
  async createPrice(input: Omit<Price, 'id' | 'version'>): Promise<Price> { return { ...input, id: crypto.randomUUID(), version: 1 } }
  async updatePrice(_id: string, _input: Partial<Omit<Price, 'id' | 'version'>> & { version: number }): Promise<Price> { throw new Error('Price not found') }

  async listShipments(_query?: ListQuery): Promise<Shipment[]> { return [] }
  async getShipment(_id: string): Promise<Shipment> { throw new Error('Shipment not found') }
  async createShipment(input: Omit<Shipment, 'id' | 'version'>): Promise<Shipment> { return { ...input, id: crypto.randomUUID(), version: 1 } }
  async updateShipment(_id: string, _input: Partial<Omit<Shipment, 'id' | 'version'>> & { version: number }): Promise<Shipment> { throw new Error('Shipment not found') }
  async cancelShipment(_id: string, _version: number): Promise<void> {}

  async listMonthlyRates(_yearMonth: string): Promise<MonthlyRate[]> { return [] }
  async updateMonthlyRate(input: MonthlyRate): Promise<MonthlyRate> { return input }
  async listDailyRates(_dateFrom: string, _dateTo: string): Promise<DailyRate[]> { return [] }
  async listSalesTargets(_yearMonth: string): Promise<SalesTarget[]> { return [] }
  async updateSalesTarget(input: SalesTarget): Promise<SalesTarget> { return input }
  async getDashboardSummary(_query: DashboardQuery): Promise<DashboardSummary> {
    return { todaySalesKrw: 0, monthSalesKrw: 0, monthTargetKrw: 0, achievementPercent: 0, currencySales: [] }
  }

  async validateImport(_input: ImportRequest): Promise<ImportValidation> {
    return { token: '', newCount: 0, duplicateCount: 0, changeCandidateCount: 0, errorCount: 0, errors: [], canCommit: false }
  }
  async commitImport(_token: string): Promise<void> {}
}
