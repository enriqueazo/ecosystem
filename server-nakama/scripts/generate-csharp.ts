// Genera validadores C# (cliente Unity) desde los contratos de shared/contracts/.
// Subconjunto admitido de JSON Schema 2020-12 y API generada:
// specs/003-sesion-invitado-cliente/contracts/csharp-contracts.md (ADR-0010).
// Cualquier palabra clave fuera del subconjunto hace fallar la generación.

import { existsSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { listContractFiles } from "./generate.ts";

export interface CSharpOptions {
  namespace: string;
  // Incluye ProtocolContracts.ProtocolVersion (desde protocol.json).
  includeProtocol: boolean;
  // Incluye la clase ContractRuntime (solo en el archivo de contratos reales).
  includeRuntime: boolean;
}

type Schema = Record<string, unknown>;

interface Contract {
  id: string;
  version: number;
  maxRequestBytes: number;
  request: Schema;
  response: Schema;
  examples: Record<"request" | "response", { valid: unknown[]; invalid: unknown[] }>;
}

const SUPPORTED_KEYWORDS = new Set([
  "$schema",
  "title",
  "description",
  "type",
  "properties",
  "required",
  "additionalProperties",
  "minimum",
  "maximum",
  "minLength",
  "maxLength",
  "pattern",
  "enum",
  "const",
]);
const SUPPORTED_TYPES = new Set([
  "object",
  "array",
  "string",
  "integer",
  "number",
  "boolean",
  "null",
]);
// Escapes con el mismo significado en JavaScript (sin u) y en .NET con RegexOptions.ECMAScript.
const PATTERN_ESCAPES = new Set(["\\d", "\\w", "\\s", "\\.", "\\-", "\\\\", "\\/", "\\^", "\\$"]);

const RUNTIME = `    /// <summary>Ayudantes de los validadores generados (misma semántica que Ajv en el servidor).</summary>
    public static class ContractRuntime
    {
        public static bool IsNumber(JToken d) => d.Type == JTokenType.Integer || d.Type == JTokenType.Float;

        // JSON Schema: un entero es un número sin parte fraccionaria (1.0 también lo es).
        public static bool IsInteger(JToken d)
        {
            if (d.Type == JTokenType.Integer) return true;
            if (d.Type != JTokenType.Float) return false;
            var value = (double)d;
            return !double.IsInfinity(value) && Math.Floor(value) == value;
        }

        // Longitud en caracteres: un par sustituto UTF-16 cuenta como 1 (como Ajv).
        public static int CodePointLength(string s)
        {
            var length = 0;
            for (var i = 0; i < s.Length; i++)
            {
                length++;
                if (char.IsHighSurrogate(s[i]) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1])) i++;
            }
            return length;
        }
    }`;

class UnsupportedSchemaError extends Error {}

function csString(value: string): string {
  return JSON.stringify(value);
}

function csVerbatim(value: string): string {
  return `@"${value.replace(/"/g, '""')}"`;
}

function pascalCase(id: string): string {
  return id
    .split("_")
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join("");
}

function isPrimitive(value: unknown): boolean {
  return value === null || ["string", "number", "boolean"].includes(typeof value);
}

function checkPattern(contractId: string, path: string, pattern: string): void {
  if (pattern.includes("(?")) {
    throw new UnsupportedSchemaError(
      `${contractId}: "pattern" con grupos especiales (?…) no admitido en ${path}`,
    );
  }
  for (let i = 0; i < pattern.length; i++) {
    if (pattern[i] !== "\\") continue;
    const escape = pattern.slice(i, i + 2);
    if (!PATTERN_ESCAPES.has(escape)) {
      throw new UnsupportedSchemaError(
        `${contractId}: "pattern" con el escape ${escape} no admitido en ${path}`,
      );
    }
    i++;
  }
}

class Emitter {
  private readonly methods: string[] = [];
  private readonly fields: string[] = [];
  private counter = 0;

  private readonly contractId: string;

  constructor(contractId: string) {
    this.contractId = contractId;
  }

  private fail(path: string, keyword: string, detail: string): never {
    throw new UnsupportedSchemaError(`${this.contractId}: "${keyword}" ${detail} en ${path}`);
  }

  private field(type: string, initializer: string): string {
    const name = `F${this.fields.length}`;
    this.fields.push(`        private static readonly ${type} ${name} = ${initializer};`);
    return name;
  }

  private typeCheck(type: unknown, path: string): string {
    if (typeof type !== "string" || !SUPPORTED_TYPES.has(type))
      this.fail(path, "type", `con valor ${JSON.stringify(type)} no admitido`);
    switch (type) {
      case "integer":
        return "ContractRuntime.IsInteger(d)";
      case "number":
        return "ContractRuntime.IsNumber(d)";
      default:
        return `d.Type == JTokenType.${type.charAt(0).toUpperCase()}${type.slice(1)}`;
    }
  }

  // Emite un método `private static bool Vn(JToken d)` para el esquema y devuelve su nombre.
  emit(schema: Schema, path: string): string {
    for (const keyword of Object.keys(schema)) {
      if (!SUPPORTED_KEYWORDS.has(keyword)) this.fail(path, keyword, "no admitido");
    }
    if ("additionalProperties" in schema && typeof schema.additionalProperties !== "boolean") {
      this.fail(path, "additionalProperties", "solo admite true o false,");
    }
    const name = `V${this.counter++}`;
    const lines: string[] = [];

    if (schema.type !== undefined) {
      // Ajv en modo estricto (servidor) no admite listas de tipos: el cliente tampoco.
      if (Array.isArray(schema.type)) this.fail(path, "type", "como lista de tipos no admitido");
      lines.push(`if (!(${this.typeCheck(schema.type, path)})) return false;`);
    }
    for (const keyword of ["enum", "const"] as const) {
      if (!(keyword in schema)) continue;
      const values = keyword === "enum" ? (schema.enum as unknown[]) : [schema.const];
      if (!Array.isArray(values) || !values.every(isPrimitive))
        this.fail(path, keyword, "solo admite valores primitivos");
      const checks = values.map(
        (value) =>
          `JToken.DeepEquals(d, ${this.field("JToken", `JToken.Parse(${csString(JSON.stringify(value))})`)})`,
      );
      lines.push(`if (!(${checks.join(" || ")})) return false;`);
    }

    const objectLines: string[] = [];
    for (const key of (schema.required as string[] | undefined) ?? []) {
      objectLines.push(`if (!o.ContainsKey(${csString(key)})) return false;`);
    }
    const properties = (schema.properties as Record<string, Schema> | undefined) ?? {};
    for (const [key, subschema] of Object.entries(properties)) {
      const sub = this.emit(subschema, `${path}.properties.${key}`);
      objectLines.push(
        `if (o.TryGetValue(${csString(key)}, out var p${sub}) && !${sub}(p${sub})) return false;`,
      );
    }
    if (schema.additionalProperties === false) {
      const allowed = this.field(
        "HashSet<string>",
        `new HashSet<string> { ${Object.keys(properties).map(csString).join(", ")} }`,
      );
      objectLines.push(
        `foreach (var property in o.Properties()) if (!${allowed}.Contains(property.Name)) return false;`,
      );
    }
    if (objectLines.length > 0) {
      lines.push(
        "if (d.Type == JTokenType.Object)",
        "{",
        "    var o = (JObject)d;",
        ...objectLines.map((l) => `    ${l}`),
        "}",
      );
    }

    const stringLines: string[] = [];
    if (typeof schema.minLength === "number")
      stringLines.push(
        `if (ContractRuntime.CodePointLength(s) < ${schema.minLength}) return false;`,
      );
    if (typeof schema.maxLength === "number")
      stringLines.push(
        `if (ContractRuntime.CodePointLength(s) > ${schema.maxLength}) return false;`,
      );
    if (typeof schema.pattern === "string") {
      checkPattern(this.contractId, path, schema.pattern);
      const regex = this.field(
        "Regex",
        `new Regex(${csVerbatim(schema.pattern)}, RegexOptions.ECMAScript)`,
      );
      stringLines.push(`if (!${regex}.IsMatch(s)) return false;`);
    }
    if (stringLines.length > 0) {
      lines.push(
        "if (d.Type == JTokenType.String)",
        "{",
        "    var s = (string)d;",
        ...stringLines.map((l) => `    ${l}`),
        "}",
      );
    }

    const numberLines: string[] = [];
    if (typeof schema.minimum === "number")
      numberLines.push(`if (n < ${schema.minimum}) return false;`);
    if (typeof schema.maximum === "number")
      numberLines.push(`if (n > ${schema.maximum}) return false;`);
    if (numberLines.length > 0) {
      lines.push(
        "if (ContractRuntime.IsNumber(d))",
        "{",
        "    var n = (double)d;",
        ...numberLines.map((l) => `    ${l}`),
        "}",
      );
    }

    lines.push("return true;");
    this.methods.push(
      [
        `        private static bool ${name}(JToken d)`,
        "        {",
        ...lines.map((l) => `            ${l}`),
        "        }",
      ].join("\n"),
    );
    return name;
  }

  render(): { fields: string[]; methods: string[] } {
    return { fields: this.fields, methods: this.methods };
  }
}

function examplesBlock(contract: Contract): string {
  const array = (name: string, values: unknown[]) =>
    `            public static readonly string[] ${name} = { ${values.map((v) => csString(JSON.stringify(v))).join(", ")} };`;
  return [
    "        /// <summary>Ejemplos del contrato, como texto JSON (pruebas de equivalencia con el servidor).</summary>",
    "        public static class Examples",
    "        {",
    array("ValidRequests", contract.examples.request.valid),
    array("InvalidRequests", contract.examples.request.invalid),
    array("ValidResponses", contract.examples.response.valid),
    array("InvalidResponses", contract.examples.response.invalid),
    "        }",
  ].join("\n");
}

function contractClass(contract: Contract): string {
  const emitter = new Emitter(contract.id);
  const request = emitter.emit(contract.request, "request");
  const response = emitter.emit(contract.response, "response");
  const { fields, methods } = emitter.render();
  return [
    `    /// <summary>Contrato ${contract.id} v${contract.version}.</summary>`,
    `    public static class ${pascalCase(contract.id)}Contract`,
    "    {",
    `        public const string Id = ${csString(contract.id)};`,
    `        public const int Version = ${contract.version};`,
    `        public const int MaxRequestBytes = ${contract.maxRequestBytes};`,
    "",
    `        public static bool ValidateRequest(JToken data) => data != null && ${request}(data);`,
    `        public static bool ValidateResponse(JToken data) => data != null && ${response}(data);`,
    "",
    examplesBlock(contract),
    "",
    ...fields,
    "",
    methods.join("\n\n"),
    "    }",
  ].join("\n");
}

export function generateCSharpSource(contractsDir: string, options: CSharpOptions): string {
  const contracts = listContractFiles(contractsDir).map(
    (path) => JSON.parse(readFileSync(path, "utf8")) as Contract,
  );
  const parts: string[] = [];
  if (options.includeProtocol) {
    const protocolPath = join(contractsDir, "protocol.json");
    if (!existsSync(protocolPath)) throw new Error(`Falta protocol.json en ${contractsDir}`);
    const { protocolVersion } = JSON.parse(readFileSync(protocolPath, "utf8")) as {
      protocolVersion: number;
    };
    parts.push(
      [
        "    /// <summary>Versión de protocolo que habla este cliente (shared/contracts/protocol.json).</summary>",
        "    public static class ProtocolContracts",
        "    {",
        `        public const int ProtocolVersion = ${protocolVersion};`,
        "    }",
      ].join("\n"),
    );
  }
  if (options.includeRuntime) parts.push(RUNTIME);
  for (const contract of contracts) parts.push(contractClass(contract));

  return [
    "// <auto-generated>",
    "// Generado por server-nakama/scripts/generate.ts desde shared/contracts/. No editar:",
    "// cambia el contrato y ejecuta `npm run generate` en server-nakama.",
    "// </auto-generated>",
    "using System;",
    "using System.Collections.Generic;",
    "using System.Text.RegularExpressions;",
    "using Newtonsoft.Json.Linq;",
    ...(options.includeRuntime ? [] : ["using Ecosystem.Contracts;"]),
    "",
    `namespace ${options.namespace}`,
    "{",
    parts.join("\n\n"),
    "}",
    "",
  ].join("\n");
}
