import { readFile } from "node:fs/promises";

const quality = await readFile("src/OrcaFacil.Application/Quality/QualityGateService.cs", "utf8");
const schema = await readFile("src/OrcaFacil.Persistence/Diagnostics/DatabaseSchemaContractService.cs", "utf8");
const failures = [];

for (const marker of [
  "schema.CheckRegistrationContractAsync(ct)", "sourceQuality.Evaluate(clock.UtcNow)",
  "File.Exists(page)", "_ToastHost", "_ConfirmDialog"
]) {
  if (!quality.includes(marker)) failures.push(`QualityGateService is missing real evidence collection: ${marker}`);
}
if (!quality.includes("rules.Add(")) failures.push("QualityGateService does not add evaluated rules.");
if (/return\s+new\s*\(\s*\[\s*\]/.test(quality)) failures.push("QualityGateService appears to return an empty fake gate.");
for (const marker of ["information_schema.columns", "pg_indexes", "information_schema.table_constraints", "__EFMigrationsHistory"]) {
  if (!schema.includes(marker)) failures.push(`Schema gate is missing real database evidence: ${marker}`);
}

if (failures.length) {
  console.error(failures.join("\n"));
  process.exit(1);
}
console.log("Quality gate no-fake check passed.");
