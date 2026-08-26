namespace OrcaFacil.Domain.Enums;

public enum OmnichannelChannelType { WebChat, CustomerPortal, PartnerPortal, InboundEmail, OutboundEmail, WhatsAppPreparedText, WhatsAppApi, ApiWebhook, Internal, System }
public enum OmnichannelChannelStatus { NotConfigured, Configured, Healthy, Degraded, Failed, Disabled }
public enum OmnichannelConversationStatus { New, Open, WaitingCustomer, WaitingInternal, WaitingPartner, InProgress, Resolved, Closed, Spam, Archived }
public enum OmnichannelMessageType { Inbound, Outbound, InternalNote, SystemEvent, Draft, FailedDelivery }
public enum OmnichannelMessageStatus { Draft, Queued, Sent, Delivered, Read, Failed, Canceled, Received, Prepared }
