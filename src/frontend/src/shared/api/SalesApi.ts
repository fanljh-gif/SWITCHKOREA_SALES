import type {
  AuditLog,
  AssignmentChangeInput,
  CompanyAssignment,
  CompanyContact,
  CompanyRelationship,
  ContactInput,
  CustomerInquiryDto,
  CustomerInquiryInput,
  Customer,
  DailyRate,
  DashboardQuery,
  DashboardSummary,
  ImportRequest,
  ImportValidation,
  IncotermDto,
  InquiryActivityDto,
  InquiryAttachmentDto,
  InquiryQuery,
  LoginRequest,
  ListQuery,
  MonthlyRate,
  NetworkCompany,
  NetworkCompanyInput,
  NetworkQuery,
  NetworkTreeNode,
  Price,
  Product,
  QuoteCreateInput,
  QuoteDashboardDto,
  QuoteDispatchDto,
  QuoteDto,
  QuoteLanguage,
  QuoteQuery,
  QuoteResult,
  QuoteRevisionDto,
  QuoteRevisionInput,
  ProspectAssignmentInput,
  ProspectCandidate,
  ProspectCandidateQuery,
  ProspectConversionInput,
  ProspectReviewInput,
  RelationshipInput,
  ResearchRun,
  ResearchRunKind,
  ResearchSummary,
  ResearchTargetProfile,
  SalesTarget,
  Shipment,
  User,
} from './contracts'

type Create<T> = Omit<T, 'id' | 'version'>
type Update<T> = Partial<Create<T>> & { version: number }

export interface SalesApi {
  getCurrentUser(): Promise<User | null>
  login(input: LoginRequest): Promise<{ user: User; passwordChangeRequired: boolean }>
  logout(): Promise<void>
  changePassword(currentPassword: string, newPassword: string): Promise<void>

  listUsers(query?: ListQuery): Promise<User[]>
  getUser(id: string): Promise<User>
  createUser(input: Create<User>): Promise<User>
  updateUser(id: string, input: Update<User>): Promise<User>
  cancelUser(id: string, version: number): Promise<void>
  listAuditLogs(query?: ListQuery): Promise<AuditLog[]>

  listCustomers(query?: ListQuery): Promise<Customer[]>
  getCustomer(id: string): Promise<Customer>
  createCustomer(input: Create<Customer>): Promise<Customer>
  updateCustomer(id: string, input: Update<Customer>): Promise<Customer>

  listNetworkCompanies(query?: NetworkQuery): Promise<NetworkCompany[]>
  getNetworkCompany(id: string): Promise<NetworkCompany>
  createNetworkCompany(input: NetworkCompanyInput): Promise<NetworkCompany>
  updateNetworkCompany(id: string, input: NetworkCompanyInput & { expectedVersion: number }): Promise<NetworkCompany>
  disableNetworkCompany(id: string, expectedVersion: number): Promise<void>
  convertNetworkCompany(id: string, input: ProspectConversionInput): Promise<NetworkCompany>
  getNetworkTree(query: NetworkQuery): Promise<NetworkTreeNode[]>

  listCompanyRelationships(companyId?: string): Promise<CompanyRelationship[]>
  createNetworkRelationship(input: RelationshipInput): Promise<CompanyRelationship>
  updateNetworkRelationship(id: string, input: RelationshipInput & { expectedVersion: number }): Promise<CompanyRelationship>
  disableNetworkRelationship(id: string, expectedVersion: number): Promise<void>

  listCompanyContacts(companyId: string): Promise<CompanyContact[]>
  createCompanyContact(companyId: string, input: ContactInput): Promise<CompanyContact>
  updateCompanyContact(companyId: string, contactId: string, input: ContactInput & { expectedVersion: number }): Promise<CompanyContact>

  listCompanyAssignments(companyId: string): Promise<CompanyAssignment[]>
  changeCompanyAssignment(companyId: string, input: AssignmentChangeInput): Promise<CompanyAssignment>

  getResearchSummary(): Promise<ResearchSummary>
  listProspectCandidates(query?: ProspectCandidateQuery): Promise<ProspectCandidate[]>
  getProspectCandidate(id: string): Promise<ProspectCandidate>
  saveResearchTargetProfile(input: ResearchTargetProfile & { expectedVersion: number }): Promise<ResearchTargetProfile>
  startResearchRun(kind: ResearchRunKind, profileId?: string): Promise<ResearchRun>
  assignProspectCandidate(id: string, input: ProspectAssignmentInput): Promise<ProspectCandidate>
  reviewProspectCandidate(id: string, input: ProspectReviewInput): Promise<ProspectCandidate>
  convertProspectCandidate(id: string, expectedVersion: number): Promise<ProspectCandidate>

  listProducts(query?: ListQuery): Promise<Product[]>
  getProduct(id: string): Promise<Product>
  createProduct(input: Create<Product>): Promise<Product>
  updateProduct(id: string, input: Update<Product>): Promise<Product>

  listPrices(customerId?: string): Promise<Price[]>
  createPrice(input: Create<Price>): Promise<Price>
  updatePrice(id: string, input: Update<Price>): Promise<Price>

  listShipments(query?: ListQuery): Promise<Shipment[]>
  getShipment(id: string): Promise<Shipment>
  createShipment(input: Create<Shipment>): Promise<Shipment>
  updateShipment(id: string, input: Update<Shipment>): Promise<Shipment>
  cancelShipment(id: string, version: number): Promise<void>

  listMonthlyRates(yearMonth: string): Promise<MonthlyRate[]>
  updateMonthlyRate(input: MonthlyRate): Promise<MonthlyRate>
  listDailyRates(dateFrom: string, dateTo: string): Promise<DailyRate[]>
  listSalesTargets(yearMonth: string): Promise<SalesTarget[]>
  updateSalesTarget(input: SalesTarget): Promise<SalesTarget>
  getDashboardSummary(query: DashboardQuery): Promise<DashboardSummary>

  getQuotationDashboard(): Promise<QuoteDashboardDto>
  listInquiries(query?: InquiryQuery): Promise<CustomerInquiryDto[]>
  getInquiry(id: string): Promise<CustomerInquiryDto>
  saveInquiry(input: CustomerInquiryInput & { id?: string; expectedVersion?: number }): Promise<CustomerInquiryDto>
  assignInquiry(id: string, ownerId: string, expectedVersion: number): Promise<CustomerInquiryDto>
  changeInquiryStatus(id: string, status: CustomerInquiryDto['status'], resultMemo: string | undefined, expectedVersion: number): Promise<CustomerInquiryDto>
  addInquiryActivity(id: string, kind: InquiryActivityDto['kind'], body: string): Promise<InquiryActivityDto>
  uploadInquiryAttachment(id: string, file: File): Promise<InquiryAttachmentDto>
  downloadInquiryAttachment(id: string, attachmentId: string): Promise<Blob>

  listQuotes(query?: QuoteQuery): Promise<QuoteDto[]>
  getQuote(id: string): Promise<QuoteDto>
  createQuote(input: QuoteCreateInput): Promise<QuoteDto>
  saveQuoteRevision(quoteId: string, revisionId: string, input: QuoteRevisionInput): Promise<QuoteRevisionDto>
  submitQuoteRevision(quoteId: string, revisionId: string, expectedVersion: number): Promise<QuoteRevisionDto>
  approveQuoteRevision(quoteId: string, revisionId: string, expectedVersion: number): Promise<QuoteRevisionDto>
  rejectQuoteRevision(quoteId: string, revisionId: string, reason: string, expectedVersion: number): Promise<QuoteRevisionDto>
  createQuoteRevision(quoteId: string, expectedVersion: number): Promise<QuoteRevisionDto>
  downloadQuotePdf(quoteId: string, revisionId: string, language: QuoteLanguage): Promise<Blob>
  recordQuoteDispatch(quoteId: string, revisionId: string, input: { sentAt: string; memo: string; language?: QuoteLanguage; expectedVersion: number }): Promise<QuoteDispatchDto>
  recordQuoteResult(quoteId: string, revisionId: string, input: { result: QuoteResult; note: string; expectedVersion: number }): Promise<QuoteDto>
  listIncoterms(): Promise<IncotermDto[]>
  saveIncoterm(input: Omit<IncotermDto, 'id' | 'version'> & { id?: string; expectedVersion?: number }): Promise<IncotermDto>

  validateImport(input: ImportRequest): Promise<ImportValidation>
  commitImport(token: string): Promise<void>
}
