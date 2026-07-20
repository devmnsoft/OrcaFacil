namespace OrcaFacil.Domain.Enums;
public enum UserRole { User, Admin, SuperAdmin }
public enum PlanType { Free, Pro }
public enum DocumentType { Budget, Receipt }
public enum BudgetStatus { Draft, Issued, Sent, Viewed, Approved, Rejected, Cancelled, Converted }
public enum ReceiptStatus { Draft, Issued, Cancelled }
public enum ClientDecision { Pending, Approved, Rejected }
public enum AppLogLevel { Debug, Info, Success, Warning, Error, Critical }
public enum SubscriptionStatus { None, Pending, Active, Cancelled, Expired, Failed }
public enum PaymentStatus { Pending, Approved, Rejected, Cancelled, Refunded, ChargedBack }
