using OrcaFacil.Application.Contracts;
using OrcaFacil.Domain.Entities;
using Xunit;
namespace OrcaFacil.UnitTests;
public sealed class AdvancedContractRulesTests
{
 [Fact] public void SlaCountsOnlyBusinessHours(){var due=ContractSlaCalculator.CalculateDueAt(new DateTime(2026,8,21,17,0,0),120,true);Assert.Equal(new DateTime(2026,8,24,9,0,0),due);}
 [Fact] public void InactiveContractDoesNotGenerateRecurrence(){Assert.False(ContractRecurrenceRules.CanGenerate(ContractStatus.Suspended));Assert.False(ContractRecurrenceRules.CanGenerate(ContractStatus.Expired));Assert.True(ContractRecurrenceRules.CanGenerate(ContractStatus.Active));}
 [Fact] public void RecurrenceKeyIsStableAndPeriodScoped(){var account=Guid.NewGuid();var contract=Guid.NewGuid();var date=new DateOnly(2026,8,1);Assert.Equal(ContractRecurrenceRules.Key(account,contract,"billing",date),ContractRecurrenceRules.Key(account,contract,"billing",date));Assert.NotEqual(ContractRecurrenceRules.Key(account,contract,"billing",date),ContractRecurrenceRules.Key(account,contract,"billing",date.AddMonths(1)));}
 [Fact] public void NegativeAdjustmentIsRejected(){Assert.Throws<InvalidOperationException>(()=>ContractAdjustmentRules.Calculate(100,"valor fixo",null,-101));}
 [Fact] public void HealthIsDeterministicAndExplainable(){var input=new ContractHealthInput(true,false,true,true,true,false,true,true,true,true);var a=ContractHealthCalculator.Calculate(input);var b=ContractHealthCalculator.Calculate(input);Assert.Equal(a,b);Assert.Equal(80,a.Score);Assert.Contains("Há pagamentos vencidos",a.RiskFactors);Assert.NotEmpty(a.NextAction);}
}
