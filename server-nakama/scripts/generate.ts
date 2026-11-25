// Genera src/generated/ a partir de shared/contracts/: validadores standalone de Ajv y constantes.
// Formato y reglas: specs/002-servidor-typescript-ci/contracts/contract-format.md → Generación.
//
// El código generado no puede depender de nada en ejecución (FR-004): el único ayudante de Ajv
// que aparece con los contratos actuales (ucs2length, para minLength/maxLength) se sustituye por
// una implementación propia equivalente; cualquier otro ayudante hace fallar la generación.

import { mkdirSync, readdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { Ajv2020 } from "ajv/dist/2020.js";
import standaloneModule from "ajv/dist/standalone/index.js";

// Módulo CommonJS: con "nodenext" el default import es el objeto exports; la función está en .default.
const standaloneCode = standaloneModule.default;

export interface GenerateOptions {
  contractsDir: string;
  packageJsonPath: string;
  outDir: string;
}

interface ContractError {
  code: string;
  grpcStatus: number;
}

interface Contract {
  id: string;
  version: number;
  maxRequestBytes: number;
  request: object;
  response: object;
  errors: ContractError[];
}

const ROOT_FILES = new Set(["protocol.json", "contract.schema.json", "contracts.lock.json"]);

// Misma semántica que ajv/dist/runtime/ucs2length: un par sustituto UTF-16 cuenta como 1.
const UCS2LENGTH_SOURCE = `function ucs2length(str) {
  var length = 0;
  var index = 0;
  var size = str.length;
  var code;
  while (index < size) {
    length++;
    code = str.charCodeAt(index++);
    if (code >= 0xd800 && code <= 0xdbff && index < size) {
      code = str.charCodeAt(index);
      if ((code & 0xfc00) === 0xdc00) index++;
    }
  }
  return length;
}`;

const UCS2LENGTH_REQUIRE = 'require("ajv/dist/runtime/ucs2length").default';

function readJson(path: string, label: string): unknown {
  try {
    return JSON.parse(readFileSync(path, "utf8"));
  } catch (error) {
    throw new Error(`No se pudo leer ${label} (${path}): ${(error as Error).message}`);
  }
}

function pascalCase(id: string): string {
  return id
    .split("_")
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join("");
}

function listContractFiles(contractsDir: string): string[] {
  const files = readdirSync(contractsDir, { recursive: true, withFileTypes: true })
    .filter((entry) => entry.isFile() && entry.name.endsWith(".json"))
    .map((entry) => join(entry.parentPath, entry.name))
    .filter(
      (path) => !(dirname(path) === contractsDir && ROOT_FILES.has(relative(contractsDir, path))),
    );
  return files.sort();
}

function readProtocolVersion(contractsDir: string): number {
  const protocolPath = join(contractsDir, "protocol.json");
  const protocol = readJson(protocolPath, "protocol.json") as { protocolVersion?: unknown };
  const version = protocol.protocolVersion;
  if (typeof version !== "number" || !Number.isInteger(version) || version < 1) {
    throw new Error(`protocol.json: protocolVersion debe ser un entero ≥ 1 (${protocolPath})`);
  }
  return version;
}

function readModuleVersion(packageJsonPath: string): string {
  const pkg = readJson(packageJsonPath, "package.json") as { version?: unknown };
  if (typeof pkg.version !== "string") {
    throw new Error(`package.json sin "version" (${packageJsonPath})`);
  }
  return pkg.version;
}

function buildValidatorsCode(contracts: Contract[]): string {
  const ajv = new Ajv2020({
    strict: true,
    code: { source: true, esm: true },
    unicodeRegExp: false,
  });
  const exports: Record<string, string> = {};
  for (const contract of contracts) {
    const name = pascalCase(contract.id);
    ajv.addSchema(contract.request, `${contract.id}#request`);
    ajv.addSchema(contract.response, `${contract.id}#response`);
    exports[`validate${name}Request`] = `${contract.id}#request`;
    exports[`validate${name}Response`] = `${contract.id}#response`;
  }
  let code = standaloneCode(ajv, exports);
  const usesUcs2length = code.includes(UCS2LENGTH_REQUIRE);
  code = code.split(UCS2LENGTH_REQUIRE).join("ucs2length");
  const remaining = code.match(/require\("([^"]+)"\)/g);
  if (remaining) {
    throw new Error(
      `El código generado necesita ayudantes de ejecución no admitidos: ${remaining.join(", ")}. ` +
        "Evita esa palabra clave de JSON Schema o amplía scripts/generate.ts con un ADR.",
    );
  }
  return usesUcs2length ? `${code}\n${UCS2LENGTH_SOURCE}\n` : `${code}\n`;
}

function contractConstName(id: string): string {
  return `${id.toUpperCase()}_CONTRACT`;
}

export function generate(options: GenerateOptions): void {
  const protocolVersion = readProtocolVersion(options.contractsDir);
  const moduleVersion = readModuleVersion(options.packageJsonPath);
  const contracts = listContractFiles(options.contractsDir).map(
    (path) => readJson(path, relative(options.contractsDir, path)) as Contract,
  );

  const header = "// Generado por scripts/generate.ts desde shared/contracts/. No editar.\n";
  const constants = [
    `export const PROTOCOL_VERSION = ${JSON.stringify(protocolVersion)};`,
    `export const MODULE_VERSION = ${JSON.stringify(moduleVersion)};`,
    ...contracts.map((contract) => {
      const errors = Object.fromEntries(contract.errors.map((e) => [e.code, e.grpcStatus]));
      const value = {
        id: contract.id,
        version: contract.version,
        maxRequestBytes: contract.maxRequestBytes,
        errors,
      };
      return `export const ${contractConstName(contract.id)} = ${JSON.stringify(value)};`;
    }),
  ];
  const js = `${header}${buildValidatorsCode(contracts)}${constants.join("\n")}\n`;

  const declarations = [
    header,
    "export declare const PROTOCOL_VERSION: number;",
    "export declare const MODULE_VERSION: string;",
    ...contracts.flatMap((contract) => {
      const name = pascalCase(contract.id);
      const errorKeys = contract.errors.map((e) => `readonly ${e.code}: number`).join("; ");
      return [
        `export declare const ${contractConstName(contract.id)}: {`,
        "  readonly id: string;",
        "  readonly version: number;",
        "  readonly maxRequestBytes: number;",
        `  readonly errors: { ${errorKeys} };`,
        "};",
        `export declare function validate${name}Request(data: unknown): boolean;`,
        `export declare function validate${name}Response(data: unknown): boolean;`,
      ];
    }),
  ].join("\n");

  mkdirSync(options.outDir, { recursive: true });
  writeFileSync(join(options.outDir, "contracts.js"), js);
  writeFileSync(join(options.outDir, "contracts.d.ts"), `${declarations}\n`);
}

if (import.meta.main) {
  const serverRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
  generate({
    contractsDir: resolve(serverRoot, "../shared/contracts"),
    packageJsonPath: join(serverRoot, "package.json"),
    outDir: join(serverRoot, "src/generated"),
  });
  console.log("Generado src/generated/ desde shared/contracts/");
}
