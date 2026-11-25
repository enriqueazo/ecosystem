import assert from "node:assert/strict";
import { mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { after, describe, it } from "node:test";
import { Ajv2020 } from "ajv/dist/2020.js";
import { generateCSharpSource } from "../../scripts/generate.ts";

const fixtureContract = JSON.parse(
  readFileSync(new URL("../fixtures/contracts/rpc/keyword_coverage.json", import.meta.url), "utf8"),
);
const root = mkdtempSync(join(tmpdir(), "generate-csharp-test-"));
after(() => rmSync(root, { recursive: true, force: true }));
let counter = 0;

function contractsDirWith(contract: Record<string, unknown>, protocolVersion = 7): string {
  const dir = join(root, `case-${counter++}`);
  mkdirSync(join(dir, "rpc"), { recursive: true });
  writeFileSync(join(dir, "protocol.json"), JSON.stringify({ protocolVersion }));
  writeFileSync(join(dir, "rpc", `${contract.id}.json`), JSON.stringify(contract));
  return dir;
}

function withRequestSchema(schema: Record<string, unknown>): Record<string, unknown> {
  return { ...structuredClone(fixtureContract), request: schema };
}

const options = { namespace: "Ecosystem.Contracts", includeProtocol: true, includeRuntime: true };

describe("ejemplos del contrato de cobertura (referencia Ajv)", () => {
  it("Ajv acepta los ejemplos válidos y rechaza los inválidos", () => {
    for (const part of ["request", "response"] as const) {
      const validate = new Ajv2020({ strict: true }).compile(fixtureContract[part]);
      for (const value of fixtureContract.examples[part].valid) {
        assert.equal(validate(value), true, `${part} válido: ${JSON.stringify(value)}`);
      }
      for (const value of fixtureContract.examples[part].invalid) {
        assert.equal(validate(value), false, `${part} inválido: ${JSON.stringify(value)}`);
      }
    }
  });
});

describe("generateCSharpSource", () => {
  it("genera la versión de protocolo y las constantes del contrato", () => {
    const code = generateCSharpSource(contractsDirWith(fixtureContract), options);

    assert.match(code, /namespace Ecosystem\.Contracts/);
    assert.match(code, /public const int ProtocolVersion = 7;/);
    assert.match(code, /public static class KeywordCoverageContract/);
    assert.match(code, /public const string Id = "keyword_coverage";/);
    assert.match(code, /public const int Version = 1;/);
    assert.match(code, /public const int MaxRequestBytes = 512;/);
    assert.match(code, /public static bool ValidateRequest\(JToken data\)/);
    assert.match(code, /public static bool ValidateResponse\(JToken data\)/);
  });

  it("incluye los ejemplos como texto JSON", () => {
    const code = generateCSharpSource(contractsDirWith(fixtureContract), options);

    assert.match(code, /ValidRequests/);
    assert.match(code, /InvalidRequests/);
    assert.match(code, /ValidResponses/);
    assert.match(code, /InvalidResponses/);
    assert.ok(code.includes(JSON.stringify(JSON.stringify({ name: "ab", extra: 1 }))));
  });

  it("traduce cada palabra clave del subconjunto", () => {
    const code = generateCSharpSource(contractsDirWith(fixtureContract), options);

    assert.match(code, /JTokenType\.Object/);
    assert.match(code, /ContainsKey\("name"\)/);
    assert.match(code, /ContractRuntime\.IsInteger/);
    assert.match(code, /ContractRuntime\.CodePointLength/);
    assert.match(code, /RegexOptions\.ECMAScript/);
    assert.match(code, /JToken\.DeepEquals/);
    assert.match(code, /JTokenType\.Array/);
    assert.match(code, /JTokenType\.Null/);
    assert.match(code, /JTokenType\.Boolean/);
  });

  it("omite la versión de protocolo y el runtime si se piden solo los contratos", () => {
    const code = generateCSharpSource(contractsDirWith(fixtureContract), {
      namespace: "Ecosystem.Tests.EditMode.Generated",
      includeProtocol: false,
      includeRuntime: false,
    });

    assert.doesNotMatch(code, /ProtocolVersion =/);
    assert.doesNotMatch(code, /class ContractRuntime/);
    assert.match(code, /namespace Ecosystem\.Tests\.EditMode\.Generated/);
  });

  for (const [name, schema, keyword] of [
    ["oneOf", { type: "object", oneOf: [{ type: "object" }] }, "oneOf"],
    [
      "type como lista",
      { type: "object", properties: { a: { type: ["string", "integer"] } } },
      "type",
    ],
    [
      "additionalProperties como esquema",
      { type: "object", additionalProperties: { type: "string" } },
      "additionalProperties",
    ],
    ["enum con objetos", { type: "object", properties: { a: { enum: [{ x: 1 }] } } }, "enum"],
    [
      "patrón con lookahead",
      { type: "object", properties: { a: { type: "string", pattern: "^(?=a)" } } },
      "pattern",
    ],
    [
      "patrón con clase Unicode",
      { type: "object", properties: { a: { type: "string", pattern: "\\p{L}" } } },
      "pattern",
    ],
  ] as const) {
    it(`falla con una palabra clave no admitida: ${name}`, () => {
      const dir = contractsDirWith(withRequestSchema(schema as Record<string, unknown>));

      assert.throws(
        () => generateCSharpSource(dir, options),
        new RegExp(`keyword_coverage.*${keyword}`),
      );
    });
  }

  it("es determinista", () => {
    const dir = contractsDirWith(fixtureContract);

    assert.equal(generateCSharpSource(dir, options), generateCSharpSource(dir, options));
  });
});
