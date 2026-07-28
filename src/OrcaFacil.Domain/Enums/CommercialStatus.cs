namespace OrcaFacil.Domain.Enums;

public enum DocumentStatus
{
    Draft, Ready, Sent, Viewed, InNegotiation, Approved, Rejected, Expired, Cancelled, ConvertedToWorkOrder
}

public enum DocumentRevisionStatus { Draft, Sent, Superseded, Approved, Rejected, ChangeRequested, Expired, Revoked }
public enum PublicAccessStatus { Active, Expired, Revoked }
public enum PublicDocumentDecisionType { Approved, Rejected, ChangeRequested }
public enum WorkOrderStatus { Planned, Scheduled, InProgress, WaitingCustomer, WaitingMaterial, Completed, Cancelled }
public enum FollowUpChannel { Phone, WhatsApp, Email, InPerson, Other }
public enum FollowUpResult { NoResponse, RequestedTime, RequestedChange, Interested, NotInterested, VerballyApproved, Other }
