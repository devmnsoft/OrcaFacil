namespace OrcaFacil.Domain.Enums;

public enum TenantDomainType { App, PortalCliente, PortalParceiro, PublicSite, Api, Universal }
public enum TenantDomainStatus { Draft, PendingVerification, Verified, Active, Failed, Suspended, Deactivated, Removed }
public enum TenantDomainVerificationMethod { Txt, Cname, HtmlFile, ManualSuperAdmin }
public enum TenantDomainSslStatus { Unknown, Pending, Valid, Invalid, Expired, ExpiringSoon, NotManaged, ManualRequired }
public enum TenantEmailDomainStatus { Pending, Verified, Failed, Suspended, Deactivated }
public enum DnsPolicyStatus { Unknown, Pending, Valid, Invalid, ManualRequired }
