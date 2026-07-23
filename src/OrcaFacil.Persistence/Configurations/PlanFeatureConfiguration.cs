using Microsoft.EntityFrameworkCore; using Microsoft.EntityFrameworkCore.Metadata.Builders; using OrcaFacil.Domain.Entities;
namespace OrcaFacil.Persistence.Configurations;
public class PlanFeatureConfiguration:IEntityTypeConfiguration<PlanFeature>{public void Configure(EntityTypeBuilder<PlanFeature>b){b.ToTable("plan_features","orcafacil");b.ConfigureBase();b.Property(x=>x.PlanCode).HasMaxLength(40).IsRequired();b.Property(x=>x.FeatureCode).HasMaxLength(120).IsRequired();b.HasIndex(x=>new{x.PlanCode,x.FeatureCode}).IsUnique();}}
