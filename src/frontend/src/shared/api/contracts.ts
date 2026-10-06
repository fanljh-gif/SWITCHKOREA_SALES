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

export interface AuditLog {
  id: string
  actorName: string
  occurredAt: string
  target: string
  action: string
  before?: string
  after?: string
}

export interface ListQuery {
  search?: string
  status?: ActiveStatus
}

export type NetworkLifecycleType = 'customer' | 'prospect'
export type EmploymentStatus = 'active' | 'former'

export interface NetworkCompany {
  id: string
  companyName: string
  country: string
  industry: string
  lifecycleType: NetworkLifecycleType
  isOem: boolean
  customerId: string | null
  currentOwnerId: string
  status: ActiveStatus
  version: number
}

export interface RelationshipSupply {
  id: string
  relationshipId: string
  productId: string
  applicationName: string
  notes?: string
}

export interface CompanyRelationship {
  id: string
  upstreamCompanyId: string
  downstreamCompanyId: string
  validFrom: string
  validTo?: string | null
  notes?: string
  status: ActiveStatus
  version: number
  supplies: RelationshipSupply[]
}

export interface CompanyContact {
  id: string
  companyId: string
  name: string
  department: string
  position: string
  businessRole: string
  mobilePhone: string
  email: string
  validFrom: string
  validTo?: string | null
  employmentStatus: EmploymentStatus
  version: number
}

export interface CompanyAssignment {
  id: string
  companyId: string
  userId: string
  validFrom: string
  validTo?: string | null
  isCurrent: boolean
  version: number
}

export interface NetworkTreeNode {
  company: NetworkCompany
  depth: number
  pathCompanyIds: string[]
  children: NetworkTreeNode[]
}

export interface NetworkQuery extends ListQuery {
  rootCompanyId?: string
  lifecycleType?: NetworkLifecycleType | 'oem'
  ownerId?: string
}

export interface NetworkCompanyInput {
  companyName: string
  country: string
  industry: string
  lifecycleType: NetworkLifecycleType
  isOem: boolean
  status: ActiveStatus
  ownerId?: string
}

export interface RelationshipInput {
  upstreamCompanyId: string
  downstreamCompanyId: string
  validFrom: string
  validTo?: string | null
  notes?: string
  status: ActiveStatus
  supplies: Array<{ productId: string; applicationName: string; notes?: string }>
}

export interface ContactInput {
  name: string
  department: string
  position: string
  businessRole: string
  mobilePhone: string
  email: string
  validFrom: string
  validTo?: string | null
  employmentStatus: EmploymentStatus
}

export interface AssignmentChangeInput {
  userId: string
  effectiveDate: string
  expectedVersion: number
}

export interface ProspectConversionInput {
  customerCode: string
  defaultCurrency: Currency
  expectedVersion: number
}

export interface ImportRequest {
  kind: 'customer' | 'product' | 'price' | 'shipment'
  file: File
  columnMap: Record<string, string>
}
