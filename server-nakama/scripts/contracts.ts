// Comprobación y huellas de los contratos de shared/contracts/ (FR-020).
// Uso: node scripts/contracts.ts check|lock [directorio]   (por defecto ../shared/contracts)
// Reglas: specs/002-servidor-typescript-ci/contracts/contract-format.md → Comprobaciones, y
// data-model.md → Contrato y Huella.

import { createHash } from "node:crypto";
import { existsSync, readFileSync, writeFileSync } from "node:fs";
import { basename, dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { Ajv2020 } from "ajv/dist/2020.js";
import {
  type CSharpOutput,
  defaultCSharpOutputs,
  generateCSharpSource,
  listContractFiles,
} from "./generate.ts";

interface ContractLike {
  id?: unknown;
  version?: unknown;
  errors?: Array<{ code?: unknown }>;
  examples?: Record<string, { valid?: unknown[]; invalid?: unknown[] }>;
  [key: string]: unknown;
}

const LOCK_FILE = "contracts.lock.json";
// Contenido normativo: lo que, si cambia, obliga a subir la versión (data-model.md → Huella.Valor).
const NORMATIVE_FIELDS = [
  "kind",
  "version",
  "auth",
  "maxRequestBytes",
  "request",
  "response",
  "errors",
];

function canonical(value: unknown): string {
  if (Array.isArray(value)) {
    return `[${value.map(canonical).join(",")}]`;
  }
  if (value !== null && typeof value === "object") {
    const entries = Object.keys(value as Record<string, unknown>)
      .sort()
      .map((key) => `${JSON.stringify(key)}:${canonical((value as Record<string, unknown>)[key])}`);
    return `{${entries.join(",")}}`;
  }
  return JSON.stringify(value);
}

export function fingerprint(contract: Record<string, unknown>): string {
  const normative = Object.fromEntries(NORMATIVE_FIELDS.map((field) => [field, contract[field]]));
  return `sha256:${createHash("sha256").update(canonical(normative)).digest("hex")}`;
}

function lockKey(contract: ContractLike): string {
  return `${contract.id}@${contract.version}`;
}

function readLock(contractsDir: string): Record<string, string> {
  const path = join(contractsDir, LOCK_FILE);
  return existsSync(path) ? JSON.parse(readFileSync(path, "utf8")) : {};
}

function readContracts(contractsDir: string): Array<{ rel: string; contract: ContractLike }> {
  return listContractFiles(contractsDir).map((path) => ({
    rel: relative(contractsDir, path).split("\\").join("/"),
    contract: JSON.parse(readFileSync(path, "utf8")) as ContractLike,
  }));
}

function checkProtocol(contractsDir: string): string[] {
  const path = join(contractsDir, "protocol.json");
  if (!existsSync(path)) return ["protocol.json no existe"];
  try {
    const version = (JSON.parse(readFileSync(path, "utf8")) as { protocolVersion?: unknown })
      .protocolVersion;
    if (typeof version === "number" && Number.isInteger(version) && version >= 1) return [];
  } catch (_error) {
    // Se informa abajo.
  }
  return ["protocol.json no es válido: protocolVersion debe ser un entero ≥ 1"];
}

function checkExamples(
  rel: string,
  contract: ContractLike,
  part: "request" | "response",
): string[] {
  const problems: string[] = [];
  let validate: (data: unknown) => boolean;
  try {
    validate = new Ajv2020({ strict: true }).compile(contract[part] as object);
  } catch (error) {
    return [`${rel}: ${part} no compila como JSON Schema 2020-12: ${(error as Error).message}`];
  }
  const examples = contract.examples?.[part];
  (examples?.valid ?? []).forEach((value, index) => {
    if (!validate(value))
      problems.push(`${rel}: examples.${part}.valid[${index}] no valida contra ${part}`);
  });
  (examples?.invalid ?? []).forEach((value, index) => {
    if (validate(value)) {
      problems.push(
        `${rel}: examples.${part}.invalid[${index}] valida contra ${part} y no debería`,
      );
    }
  });
  return problems;
}

export function checkContracts(contractsDir: string): string[] {
  const problems = checkProtocol(contractsDir);
  const metaSchema = JSON.parse(readFileSync(join(contractsDir, "contract.schema.json"), "utf8"));
  const metaAjv = new Ajv2020({ strict: true, allErrors: true });
  const validateContract = metaAjv.compile(metaSchema);
  const lock = readLock(contractsDir);
  const seen = new Map<string, string>();

  for (const { rel, contract } of readContracts(contractsDir)) {
    if (!validateContract(contract)) {
      problems.push(
        `${rel}: no cumple el metaesquema: ${metaAjv.errorsText(validateContract.errors)}`,
      );
      continue;
    }
    const id = contract.id as string;
    const expectedId = basename(rel, ".json");
    if (id !== expectedId) {
      problems.push(`${rel}: id "${id}" no coincide con el nombre del archivo (${expectedId})`);
    }
    const previous = seen.get(id);
    if (previous) problems.push(`id duplicado "${id}" en ${previous} y ${rel}`);
    seen.set(id, rel);

    const codes = new Set<unknown>();
    for (const error of contract.errors ?? []) {
      if (codes.has(error.code)) problems.push(`${rel}: código de error duplicado "${error.code}"`);
      codes.add(error.code);
    }

    problems.push(
      ...checkExamples(rel, contract, "request"),
      ...checkExamples(rel, contract, "response"),
    );

    const key = lockKey(contract);
    if (!(key in lock)) {
      problems.push(
        `${rel}: ${key} no está en ${LOCK_FILE}; si es una versión nueva, ejecuta npm run contracts:lock`,
      );
    } else if (lock[key] !== fingerprint(contract)) {
      problems.push(
        `${rel}: ${key}: la huella no coincide con ${LOCK_FILE}: el contenido normativo cambió; ` +
          'sube "version" y ejecuta npm run contracts:lock',
      );
    }
  }
  return problems;
}

// FR-013 (spec 003): el C# versionado del cliente debe coincidir con lo que genera la definición.
export function checkGeneratedCSharp(outputs: CSharpOutput[]): string[] {
  const problems: string[] = [];
  for (const output of outputs) {
    const expected = generateCSharpSource(output.contractsDir, output);
    const actual = existsSync(output.outFile) ? readFileSync(output.outFile, "utf8") : null;
    if (actual !== expected) {
      problems.push(
        `${basename(output.outFile)} desactualizado: ejecuta npm run generate y confirma el cambio (${output.outFile})`,
      );
    }
  }
  return problems;
}

export function lockContracts(contractsDir: string): void {
  const lock = readLock(contractsDir);
  for (const { contract } of readContracts(contractsDir)) {
    const key = lockKey(contract);
    const value = fingerprint(contract);
    if (key in lock && lock[key] !== value) {
      throw new Error(
        `${key} ya registrado con otra huella: el contenido normativo cambió; sube "version" ` +
          "en lugar de sobrescribir la huella.",
      );
    }
    lock[key] = value;
  }
  const sorted = Object.fromEntries(
    Object.keys(lock)
      .sort()
      .map((key) => [key, lock[key]]),
  );
  writeFileSync(join(contractsDir, LOCK_FILE), `${JSON.stringify(sorted, null, 2)}\n`);
}

if (import.meta.main) {
  const serverRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
  const [command, dirArg] = process.argv.slice(2);
  const contractsDir = dirArg ?? resolve(serverRoot, "../shared/contracts");
  if (command === "check") {
    const problems = checkContracts(contractsDir);
    if (!dirArg) problems.push(...checkGeneratedCSharp(defaultCSharpOutputs(serverRoot)));
    if (problems.length === 0) {
      console.log("contracts:check OK");
    } else {
      for (const problem of problems) console.error(`✘ ${problem}`);
      process.exitCode = 1;
    }
  } else if (command === "lock") {
    lockContracts(contractsDir);
    console.log(`contracts:lock: ${LOCK_FILE} actualizado`);
  } else {
    console.error("Uso: node scripts/contracts.ts check|lock [directorio]");
    process.exitCode = 64;
  }
}
