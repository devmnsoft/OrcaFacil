namespace OrcaFacil.Domain.Enums;

public enum AccountStatus { Active, Inactive, Blocked, Closed }
public enum AccountMemberStatus { Invited, Active, Disabled, Blocked }
public enum PlanVersionStatus { Draft, Published, Archived }
public enum PlanFeatureValueType { Boolean, Integer, Decimal, Text }
public enum BillingCycle { Monthly, Annual }
public enum BillingInvoiceStatus { Draft, Pending, Approved, Expired, Cancelled, Refunded }
public enum SupportAccessMode { ReadOnly, Assisted }
