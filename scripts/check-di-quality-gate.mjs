import { readFile } from "node:fs/promises";

const persistence = await readFile("src/OrcaFacil.Persistence/DependencyInjection.cs", "utf8");
const application = await readFile("src/OrcaFacil.Application/DependencyInjection.cs", "utf8");
const api = await readFile("src/OrcaFacil.Api/Program.cs", "utf8");
const web = await readFile("src/OrcaFacil.Web/Program.cs", "utf8");

const requirements = [
  [application, /TryAddScoped<QualityGateService>/, "QualityGateService must be scoped"],
  [persistence, /TryAddScoped<IDatabaseSchemaContractService,\s*DatabaseSchemaContractService>/, "schema contract implementation must be scoped"],
  [api, /AddPersistence\(\)/, "API must register persistence services"],
  [web, /AddPersistence\(\)/, "Web must register persistence services"],
];
const failures = requirements.filter(([source, pattern]) => !pattern.test(source)).map(([, , message]) => message);
if (failures.length) { console.error(failures.join("\n")); process.exit(1); }
console.log("Quality Gate DI registrations are valid for API and Web.");
