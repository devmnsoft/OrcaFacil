namespace OrcaFacil.Domain.Enums;

public enum SupportTicketCategory { Access, Budget, Payment, Receipt, Plan, TechnicalError, Suggestion, Other }
public enum SupportTicketStatus { New, Open, InReview, WaitingForUser, WaitingCustomer, WaitingInternal, InProgress, Escalated, PendingDevelopment, PendingRelease, Resolved, Closed, Canceled, Reopened }
public enum SupportTicketPriority { Low, Normal, High, Urgent, Critical, Incident }
public enum SupportMessageType { PublicReply, InternalNote, SystemEvent, CustomerReply, PartnerReply, EmailReply }
public enum SupportIncidentStatus { Investigating, Identified, Monitoring, Resolved, Canceled }
public enum SupportProblemStatus { Open, Investigating, KnownError, Resolved, Closed }
