import { readFile } from "node:fs/promises";

const interfaceSource = await readFile("src/OrcaFacil.Application/Abstractions/IDatabaseSchemaContractService.cs", "utf8");
const implementationSource = await readFile("src/OrcaFacil.Persistence/Diagnostics/DatabaseSchemaContractService.cs", "utf8");
const normalizedInterface = interfaceSource.replace(/\s+/g, " ");
const normalizedImplementation = implementationSource.replace(/\s+/g, " ");
const failures = [];

const requiredInterfaceFragments = [
  "namespace OrcaFacil.Application.Abstractions;",
  "public interface IDatabaseSchemaContractService",
  "Task<DatabaseSchemaContractResult> CheckRegistrationContractAsync(CancellationToken ct = default);",
  "public sealed record DatabaseSchemaContractIssue( string Table, string? Column, string State, string RecommendedMigration);",
  "public sealed record DatabaseSchemaContractResult( bool IsValid, IReadOnlyList<DatabaseSchemaContractIssue> Issues, DateTimeOffset CheckedAt, bool HasPendingMigrations);"
];
for (const fragment of requiredInterfaceFragments) {
  if (!normalizedInterface.includes(fragment)) failures.push(`Real interface contract changed or is missing: ${fragment}`);
}
if (!normalizedImplementation.includes("DatabaseSchemaContractService(IConfiguration configuration) : IDatabaseSchemaContractService"))
  failures.push("DatabaseSchemaContractService does not implement IDatabaseSchemaContractService.");
if (!normalizedImplementation.includes("Task<DatabaseSchemaContractResult> CheckRegistrationContractAsync(CancellationToken ct = default)"))
  failures.push("DatabaseSchemaContractService method does not match the real interface signature.");

if (failures.length) {
  console.error(failures.join("\n"));
  process.exit(1);
}
console.log("Database schema contract interface/implementation check passed.");
