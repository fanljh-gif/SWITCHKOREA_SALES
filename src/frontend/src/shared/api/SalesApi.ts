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

  validateImport(input: ImportRequest): Promise<ImportValidation>
  commitImport(token: string): Promise<void>
}
