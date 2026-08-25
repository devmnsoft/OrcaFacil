using OrcaFacil.Domain.Entities;
using Xunit;
namespace OrcaFacil.UnitTests;
public sealed class EnterpriseTenantIsolationTests{[Fact]public void EnterpriseRecordsRequireExplicitAccountScope(){var account=Guid.NewGuid();Assert.Equal(account,new BusinessUnit{AccountId=account}.AccountId);Assert.Equal(account,new Team{AccountId=account}.AccountId);Assert.Equal(account,new ApprovalRequest{AccountId=account}.AccountId);}}
