import assert from "node:assert/strict";
import { mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { after, describe, it } from "node:test";
import { pathToFileURL } from "node:url";
import { generate } from "../../scripts/generate.ts";

const workRoot = mkdtempSync(join(tmpdir(), "generate-test-"));
after(() => rmSync(workRoot, { recursive: true, force: true }));

const demoContract = {
  id: "demo_echo",
  kind: "rpc",
  version: 3,
  auth: "session",
  maxRequestBytes: 256,
  request: {
    $schema: "https://json-schema.org/draft/2020-12/schema",
    type: "object",
    properties: { text: { type: "string", maxLength: 3 } },
  },
  response: {
    $schema: "https://json-schema.org/draft/2020-12/schema",
    type: "object",
    additionalProperties: false,
    required: ["ok"],
    properties: { ok: { type: "boolean" } },
  },
  errors: [{ code: "INVALID_ARGUMENT", grpcStatus: 3, when: "Petición inválida." }],
  examples: {
    request: { valid: [{}, { text: "abc" }], invalid: [[], { text: "abcd" }] },
    response: { valid: [{ ok: true }], invalid: [{ ok: true, extra: 1 }] },
  },
};

let caseCounter = 0;

function makeFixture(options: { withProtocol?: boolean } = {}) {
  const dir = join(workRoot, `case-${caseCounter++}`);
  const contractsDir = join(dir, "contracts");
  mkdirSync(join(contractsDir, "rpc"), { recursive: true });
  if (options.withProtocol !== false) {
    writeFileSync(join(contractsDir, "protocol.json"), JSON.stringify({ protocolVersion: 7 }));
  }
  writeFileSync(join(contractsDir, "rpc", "demo_echo.json"), JSON.stringify(demoContract));
  const packageJsonPath = join(dir, "package.json");
  writeFileSync(packageJsonPath, JSON.stringify({ name: "x", version: "1.2.3" }));
  return { contractsDir, packageJsonPath, outDir: join(dir, "generated") };
}

async function importGenerated(outDir: string) {
  return import(pathToFileURL(join(outDir, "contracts.js")).href);
}

describe("generate", () => {
  it("genera validadores que aceptan los ejemplos válidos y rechazan los inválidos", async () => {
    const fixture = makeFixture();
    generate(fixture);
    const mod = await importGenerated(fixture.outDir);

    for (const value of demoContract.examples.request.valid) {
      assert.equal(mod.validateDemoEchoRequest(value), true, JSON.stringify(value));
    }
    for (const value of demoContract.examples.request.invalid) {
      assert.equal(mod.validateDemoEchoRequest(value), false, JSON.stringify(value));
    }
    for (const value of demoContract.examples.response.valid) {
      assert.equal(mod.validateDemoEchoResponse(value), true, JSON.stringify(value));
    }
    for (const value of demoContract.examples.response.invalid) {
      assert.equal(mod.validateDemoEchoResponse(value), false, JSON.stringify(value));
    }
  });

  it("cuenta maxLength en caracteres, como un par sustituto UTF-16 = 1", async () => {
    const fixture = makeFixture();
    generate(fixture);
    const mod = await importGenerated(fixture.outDir);

    assert.equal(mod.validateDemoEchoRequest({ text: "😀😀😀" }), true);
    assert.equal(mod.validateDemoEchoRequest({ text: "😀😀😀😀" }), false);
  });

  it("genera las constantes de protocolo, módulo y contrato", async () => {
    const fixture = makeFixture();
    generate(fixture);
    const mod = await importGenerated(fixture.outDir);

    assert.equal(mod.PROTOCOL_VERSION, 7);
    assert.equal(mod.MODULE_VERSION, "1.2.3");
    assert.deepEqual(mod.DEMO_ECHO_CONTRACT, {
      id: "demo_echo",
      version: 3,
      maxRequestBytes: 256,
      errors: { INVALID_ARGUMENT: 3 },
    });
  });

  it("el código generado no tiene require ni dependencias externas", () => {
    const fixture = makeFixture();
    generate(fixture);
    const code = readFileSync(join(fixture.outDir, "contracts.js"), "utf8");

    assert.doesNotMatch(code, /require\(/);
    assert.doesNotMatch(code, /^\s*import /m);
  });

  it("genera las declaraciones de tipos para TypeScript", () => {
    const fixture = makeFixture();
    generate(fixture);
    const dts = readFileSync(join(fixture.outDir, "contracts.d.ts"), "utf8");

    assert.match(dts, /export declare const PROTOCOL_VERSION: number;/);
    assert.match(dts, /export declare function validateDemoEchoRequest\(data: unknown\): boolean;/);
  });

  it("falla con un mensaje claro si falta protocol.json", () => {
    const fixture = makeFixture({ withProtocol: false });

    assert.throws(() => generate(fixture), /protocol\.json/);
  });
});
