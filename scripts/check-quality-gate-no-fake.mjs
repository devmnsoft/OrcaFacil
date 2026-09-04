import { readFile } from "node:fs/promises";

const source = await readFile("src/OrcaFacil.Application/Quality/QualityGateService.cs", "utf8");
const failures = [];
if (!source.includes("schema.CheckRegistrationContractAsync")) failures.push("QualityGateService does not execute the schema contract");
if (/return\s+new\s*\([^;]*(?:true|100)[^;]*\);/s.test(source)) failures.push("QualityGateService appears to return an unconditional success");
if (!source.includes("FunctionalQualityService") || !source.includes("CriticalRoutes")) failures.push("QualityGateService lost source or route checks");
if (failures.length) { console.error(failures.join("\n")); process.exit(1); }
console.log("Quality Gate uses real schema, route, and source-quality evidence.");
