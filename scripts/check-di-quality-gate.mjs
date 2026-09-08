import { readFile } from "node:fs/promises";

const files = {
  application: "src/OrcaFacil.Application/DependencyInjection.cs",
  persistence: "src/OrcaFacil.Persistence/DependencyInjection.cs",
  api: "src/OrcaFacil.Api/Program.cs",
  web: "src/OrcaFacil.Web/Program.cs"
};
const source = Object.fromEntries(await Promise.all(
  Object.entries(files).map(async ([name, file]) => [name, await readFile(file, "utf8")])
));

const failures = [];
const requireMatch = (condition, message) => { if (!condition) failures.push(message); };
requireMatch(/TryAddScoped\s*<\s*QualityGateService\s*>/.test(source.application),
  "QualityGateService must be registered as scoped in Application.");
requireMatch(/TryAddScoped\s*<\s*IDatabaseSchemaContractService\s*,\s*DatabaseSchemaContractService\s*>/.test(source.persistence),
  "IDatabaseSchemaContractService must use the real scoped Persistence implementation.");
for (const host of ["api", "web"]) {
  requireMatch(source[host].includes("builder.Services.AddApplication(repositoryRoot);"), `${host} does not call AddApplication.`);
  requireMatch(source[host].includes("builder.Services.AddPersistence();"), `${host} does not call AddPersistence.`);
}
const combined = Object.values(source).join("\n");
requireMatch(!/(?:Add|TryAdd)Singleton\s*<\s*(?:QualityGateService|IDatabaseSchemaContractService)\b/.test(combined),
  "Quality gate database services cannot be singletons.");

if (failures.length) {
  console.error(failures.join("\n"));
  process.exit(1);
}
console.log("Quality gate DI registration check passed for API and Web.");
