namespace SalesManagement.Core.Models;

public enum Role { Manager, Member }
public enum Currency { KRW, USD, CNY }
public enum ItemType { Product, Goods }
public enum EntityStatus { Active, Inactive }
public enum ShipmentStatus { Confirmed, Cancelled }
public enum RateFetchStatus { Success, Failed }
public enum ImportKind { Customer, Product, Price, Shipment }
public enum ImportStatus { Validating, Ready, Committed, Rejected }
public enum LifecycleType { Customer, Prospect }
public enum EmploymentStatus { Active, Former }
