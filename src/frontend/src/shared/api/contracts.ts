export type Role = 'manager' | 'member'
export type Currency = 'KRW' | 'USD' | 'CNY'
export type ItemType = 'product' | 'goods'
export type ActiveStatus = 'active' | 'inactive'

export interface User {
  id: string
  loginId: string
  name: string
  email: string
  role: Role
  status: ActiveStatus
  passwordChangeRequired?: boolean
  lockedUntil?: string
}

export interface LoginRequest {
  loginId: string
  password: string
}

export interface AuthResult {
  user: User
  passwordChangeRequired: boolean
}

export interface Customer {
  id: string
  code: string
  companyName: string
  country: string
  defaultCurrency: Currency
  ownerId: string
  status: ActiveStatus
  version: number
}

export interface Product {
  id: string
  code: string
  name: string
  itemType: ItemType
  specification: string
  status: ActiveStatus
  version: number
}

export interface Price {
  id: string
  customerId: string
  productId: string
  currency: Currency
  unitPrice: number
  validFrom: string
  validTo?: string
  version: number
}

export interface Shipment {
  id: string
  shipmentNumber?: string
  shipmentDate: string
  customerId: string
  productId: string
  quantity: number
  unitPrice: number
  currency: Currency
  supplyAmount: number
  appliedRate: number
  krwAmount: number
  ownerId: string
  status: 'confirmed' | 'cancelled'
  version: number
}

export interface MonthlyRate {
  yearMonth: string
  currency: Currency
  rate: number
  updatedBy: string
  updatedAt: string
}

export interface DailyRate {
  date: string
  currency: Currency
  rate: number
  fetchedAt: string
  status: 'success' | 'failed'
}

export interface SalesTarget {
  yearMonth: string
  userId: string
  targetKrw: number
  version: number
}

export interface DashboardQuery {
  dateFrom: string
  dateTo: string
  ownerId?: string
  customerId?: string
  productId?: string
}

export interface CurrencySales {
  currency: Currency
  originalAmount: number
  krwAmount: number
  sharePercent: number
}

export interface DashboardSummary {
  todaySalesKrw: number
  monthSalesKrw: number
  monthTargetKrw: number
  achievementPercent: number
  currencySales: CurrencySales[]
  ownerSales?: SalesPerformance[]
  customerSales?: SalesPerformance[]
  productSales?: SalesPerformance[]
}

export interface SalesPerformance {
  id: string
  name: string
  krwAmount: number
}

export interface ImportRowError {
  row: number
  field?: string
  message: string
}

export interface ImportValidation {
  token: string
  newCount: number
  duplicateCount: number
  changeCandidateCount: number
  errorCount: number
  errors: ImportRowError[]
  canCommit: boolean
}

export interface ListQuery {
  search?: string
  status?: ActiveStatus
}

export interface ImportRequest {
  kind: 'customer' | 'product' | 'price' | 'shipment'
  file: File
  columnMap: Record<string, string>
}
