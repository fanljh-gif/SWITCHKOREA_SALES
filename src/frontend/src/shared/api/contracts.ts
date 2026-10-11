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

export type ProspectCandidateStatus = 'collected' | 'enriching' | 'reviewPending' | 'duplicateSuspected' | 'onHold' | 'excluded' | 'approved' | 'converted'
export type ResearchRunKind = 'manual' | 'scheduled' | 'excel'
export type ProspectPriority = 'high' | 'normal' | 'low'
export type ResearchConfidence = 'high' | 'medium' | 'low'

export interface ResearchTargetProfile {
  id: string
  name: string
  regions: string[]
  industries: string[]
  includeKeywords: string[]
  excludeKeywords: string[]
  maxSearchResults: number
  maxAiAnalyses: number
  isActive: boolean
  nextRunAt?: string
  version: number
}

export interface ResearchRun {
  id: string
  profileId: string
  kind: ResearchRunKind
  status: 'queued' | 'running' | 'partial' | 'completed' | 'failed'
  startedAt: string
  completedAt?: string
  discoveredCount: number
  enrichedCount: number
  failedCount: number
  apiCallCount: number
  costKrw: number
  errorSummary?: string
}

export interface ResearchEvidence {
  id: string
  candidateId: string
  title: string
  url: string
  observedAt: string
  summary: string
  confidence: ResearchConfidence
  sourceHash: string
}

export interface ProspectScore {
  total: number
  productFit: number
  industryFit: number
  supplyChainFit: number
  regionFit: number
  companyPotential: number
  evidenceQuality: number
  priority: ProspectPriority
  ruleVersion: string
  assessedAt: string
}

export interface AiAssessment {
  summary: string
  recommendation: string
  risks: string[]
  model: string
  generatedAt: string
}

export interface DuplicateMatch {
  id: string
  candidateId: string
  targetType: 'candidate' | 'networkCompany' | 'customer'
  targetId: string
  targetName: string
  similarity: number
  reasons: string[]
  decision: 'pending' | 'duplicate' | 'distinct'
}

export interface ProspectCandidate {
  id: string
  companyName: string
  englishName?: string
  country: string
  region: string
  industry: string
  companySize: string
  domain: string
  website: string
  products: string[]
  applications: string[]
  ownerId?: string
  status: ProspectCandidateStatus
  discoveredBy: ResearchRunKind
  runId: string
  score: ProspectScore
  aiAssessment?: AiAssessment
  evidence: ResearchEvidence[]
  duplicateMatches: DuplicateMatch[]
  networkCompanyId?: string
  version: number
}

export interface ProspectCandidateQuery {
  search?: string
  region?: string
  industry?: string
  status?: ProspectCandidateStatus
  ownerId?: string
  runId?: string
}

export interface ResearchSummary {
  statusCounts: Record<ProspectCandidateStatus, number>
  monthlyUsage: { apiCallCount: number; costKrw: number; limitKrw: number; percent: number }
  targetProfile: ResearchTargetProfile
  recentRuns: ResearchRun[]
  partialError?: string
}

export interface ProspectAssignmentInput {
  ownerId: string
  expectedVersion: number
}

export interface ProspectReviewInput {
  decision: 'onHold' | 'exclude' | 'approve' | 'distinct' | 'duplicate'
  expectedVersion: number
  note?: string
}

export interface ImportRequest {
  kind: 'customer' | 'product' | 'price' | 'shipment'
  file: File
  columnMap: Record<string, string>
}

export type InquiryStatus = 'received' | 'assigned' | 'responding' | 'quoting' | 'answered' | 'closed'
export type QuoteStatus = 'draft' | 'approvalRequested' | 'approved' | 'rejected' | 'sent' | 'won' | 'lost' | 'onHold'
export type QuoteTaxType = 'exclusive' | 'inclusive' | 'exempt'
export type QuoteLanguage = 'ko' | 'en'
export type QuoteTradeType = 'domestic' | 'export'
export type QuoteResult = 'won' | 'lost' | 'onHold'

export interface InquiryActivityDto {
  id: string
  inquiryId: string
  kind: 'response' | 'memo' | 'status' | 'assignment'
  body: string
  actorId: string
  occurredAt: string
}

export interface InquiryAttachmentDto {
  id: string
  inquiryId: string
  fileName: string
  contentType: string
  size: number
  uploadedBy: string
  uploadedAt: string
}

export interface CustomerInquiryDto {
  id: string
  inquiryNumber: string
  title: string
  customerId: string
  customerName: string
  contactName: string
  contactEmail?: string
  ownerId: string
  receivedAt: string
  responseDueAt: string
  status: InquiryStatus
  summary: string
  resultMemo?: string
  activities: InquiryActivityDto[]
  attachments: InquiryAttachmentDto[]
  linkedQuoteIds: string[]
  version: number
}

export interface CustomerInquiryInput {
  title: string
  customerId: string
  contactName: string
  contactEmail?: string
  ownerId: string
  receivedAt: string
  responseDueAt: string
  summary: string
}

export interface InquiryQuery {
  search?: string
  ownerId?: string
  customerId?: string
  status?: InquiryStatus
  overdueOnly?: boolean
}

export interface QuoteLineDto {
  id: string
  productId: string
  description: string
  quantity: number
  unitPrice: number
  amount: number
}

export interface QuoteApprovalDto {
  id: string
  revisionId: string
  requestedBy: string
  requestedAt: string
  decidedBy?: string
  decidedAt?: string
  decision: 'pending' | 'approved' | 'rejected'
  reason?: string
}

export interface QuoteDispatchDto {
  id: string
  revisionId: string
  sentAt: string
  memo: string
  language: QuoteLanguage
  recordedBy: string
  pdfHash?: string
  version: number
}

export interface QuoteRevisionDto {
  id: string
  quoteId: string
  revisionNumber: number
  revisionLabel: string
  status: QuoteStatus
  currency: Currency
  tradeType: QuoteTradeType
  taxType: QuoteTaxType
  validUntil: string
  incotermCode: string | null
  namedPlace: string | null
  paymentTerms?: string | null
  deliveryTerms?: string | null
  notes?: string | null
  discountAmount: number
  shippingAmount: number
  otherAmount: number
  subtotalAmount: number
  supplyAmount: number
  taxAmount: number
  totalAmount: number
  lines: QuoteLineDto[]
  approvals: QuoteApprovalDto[]
  dispatches: QuoteDispatchDto[]
  rejectionReason?: string
  version: number
}

export interface QuoteDto {
  id: string
  quoteNumber: string
  inquiryId?: string
  customerId: string
  customerName: string
  ownerId: string
  currentRevisionId: string
  currentRevision: QuoteRevisionDto
  revisions: QuoteRevisionDto[]
  result?: QuoteResult
  resultNote?: string
  version: number
}

export interface QuoteLineInput {
  productId: string
  description: string
  quantity: number
  unitPrice: number
}

export interface QuoteCreateInput {
  inquiryId?: string
  customerId: string
  ownerId: string
  tradeType: QuoteTradeType
  currency: Currency
  taxType: QuoteTaxType
  validUntil: string
  incotermCode: string | null
  namedPlace: string | null
  paymentTerms?: string | null
  deliveryTerms?: string | null
  notes?: string | null
  discountAmount: number
  shippingAmount: number
  otherAmount: number
  lines: QuoteLineInput[]
}

export interface QuoteRevisionInput extends Omit<QuoteCreateInput, 'inquiryId' | 'customerId' | 'ownerId'> {
  expectedVersion: number
}

export interface QuoteQuery {
  search?: string
  ownerId?: string
  customerId?: string
  status?: QuoteStatus
  currency?: Currency
}

export interface QuoteDashboardDto {
  unansweredCount: number
  overdueCount: number
  approvalPendingCount: number
  monthlyQuoteCount: number
  currencyAmounts: Array<{ currency: Currency; amount: number }>
  resultCounts: Record<QuoteResult, number>
  winRatePercent: number
}

export interface IncotermDto {
  id: string
  code: string
  displayName: string
  status: ActiveStatus
  sortOrder: number
  version: number
}
