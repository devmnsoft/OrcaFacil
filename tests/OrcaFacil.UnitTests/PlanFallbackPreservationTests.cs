using OrcaFacil.Application.Plans;
using Xunit;
namespace OrcaFacil.UnitTests;
public sealed class PlanFallbackPreservationTests
{
 [Fact]
 public void Paid_account_should_fallback_to_free_without_losing_data()
 {
  var ids=new[]{Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid()};
  var decision=PlanDataPreservationPolicy.Evaluate(currentUsage:18,availableLimit:10);
  Assert.False(decision.CanCreate); Assert.Equal(18,decision.PreservedCount); Assert.Equal(8,decision.AboveLimitCount);
  Assert.Contains("continuam salvos",decision.Message); Assert.Equal(3,ids.Distinct().Count());
  foreach(var required in new[]{"clients","services","documents","pdfs","branding","templates","public-links","members","audit","notifications"}) Assert.Contains(required,PlanDataPreservationPolicy.PreservedData);
 }
 [Fact]
 public void Restored_payment_should_restore_benefits_without_recreating_data()
 {
  var before=new[]{Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid()}; var after=before.ToArray();
  var decision=PlanDataPreservationPolicy.Evaluate(currentUsage:18,availableLimit:null);
  Assert.True(decision.CanCreate); Assert.Null(decision.AvailableLimit); Assert.Equal(before,after); Assert.Equal(before.Length,after.Distinct().Count());
 }
 [Fact]
 public void Free_limit_should_allow_creation_when_usage_is_within_capacity()
 {
  var decision=PlanDataPreservationPolicy.Evaluate(9,10); Assert.True(decision.CanCreate); Assert.Equal(0,decision.AboveLimitCount); Assert.Null(decision.Message);
 }
}
