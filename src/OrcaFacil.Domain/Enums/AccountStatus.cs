namespace OrcaFacil.Domain.Enums;

public enum AccountStatus { Active, Inactive, Blocked, Closed }
public enum AccountMemberStatus { Invited, Active, Disabled, Blocked }
public enum PlanVersionStatus { Draft, Published, Archived }
public enum PlanFeatureValueType { Boolean, Integer, Decimal, Text }
public enum BillingCycle { Monthly, Annual, Manual, Custom }
public enum BillingInvoiceStatus { Draft, Pending, Issued, PartiallyPaid, Paid, Overdue, Cancelled, Uncollectible, Refunded }
public enum BillingPaymentStatus { Registered, Reversed, Cancelled }
public enum BillingPaymentMethod { PixManual, BankTransfer, Cash, CreditCardManual, External, Courtesy, Adjustment }
public enum SubscriptionChangeRequestType { Upgrade, Downgrade, Cancel, Reactivate, AddUsers, AddStorage, ChangeBillingCycle }
public enum SubscriptionChangeRequestStatus { Open, InReview, Approved, Rejected, Cancelled, Completed }
public enum SupportAccessMode { ReadOnly, Assisted }
