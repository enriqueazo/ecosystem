import assert from "node:assert/strict";
import { copyFileSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { after, describe, it } from "node:test";
import {
  checkContracts,
  checkGeneratedCSharp,
  fingerprint,
  lockContracts,
} from "../../scripts/contracts.ts";
import { generateCSharpSource } from "../../scripts/generate.ts";

const realContracts = new URL("../../../shared/contracts/", import.meta.url);
const root = mkdtempSync(join(tmpdir(), "contracts-test-"));
after(() => rmSync(root, { recursive: true, force: true }));

const baseContract = {
  id: "demo_echo",
  kind: "rpc",
  version: 1,
  description: "Demo.",
  auth: "session",
  maxRequestBytes: 256,
  request: { $schema: "https://json-schema.org/draft/2020-12/schema", type: "object" },
  response: {
    $schema: "https://json-schema.org/draft/2020-12/schema",
    type: "object",
    additionalProperties: false,
    required: ["ok"],
    properties: { ok: { type: "boolean" } },
  },
  errors: [{ code: "INVALID_ARGUMENT", grpcStatus: 3, when: "Petición inválida." }],
  examples: {
    request: { valid: [{}], invalid: [[]] },
    response: { valid: [{ ok: true }], invalid: [{ ok: 1 }] },
  },
};

type Contract = typeof baseContract;
let counter = 0;

// Directorio de contratos válido y con huellas registradas; `mutate` lo altera después.
function fixture(mutate?: (contract: Contract) => void, options: { lock?: boolean } = {}) {
  const dir = join(root, `case-${counter++}`);
  mkdirSync(join(dir, "rpc"), { recursive: true });
  copyFileSync(new URL("contract.schema.json", realContracts), join(dir, "contract.schema.json"));
  writeFileSync(join(dir, "protocol.json"), JSON.stringify({ protocolVersion: 1 }));
  const contract: Contract = structuredClone(baseContract);
  const file = join(dir, "rpc", "demo_echo.json");
  writeFileSync(file, JSON.stringify(contract));
  writeFileSync(join(dir, "contracts.lock.json"), "{}\n");
  if (options.lock !== false) lockContracts(dir);
  if (mutate) {
    mutate(contract);
    writeFileSync(file, JSON.stringify(contract));
  }
  return { dir, file };
}

function problemsOf(dir: string): string {
  return checkContracts(dir).join("\n");
}

describe("checkContracts", () => {
  it("acepta un directorio de contratos válido", () => {
    assert.deepEqual(checkContracts(fixture().dir), []);
  });

  it("acepta los contratos reales del repositorio", () => {
    assert.deepEqual(checkContracts(realContracts.pathname), []);
  });

  it("1 · rechaza un contrato que no cumple el metaesquema", () => {
    const { dir } = fixture((c) => {
      (c as { kind: string }).kind = "evento";
    });

    assert.match(problemsOf(dir), /rpc\/demo_echo\.json.*metaesquema/);
  });

  it("2 · rechaza un esquema que no compila", () => {
    const { dir } = fixture((c) => {
      (c.request as Record<string, unknown>).properties = { a: { type: "entero" } };
    });

    assert.match(problemsOf(dir), /rpc\/demo_echo\.json.*request.*no compila/);
  });

  it("3 · rechaza un ejemplo válido que no valida", () => {
    const { dir } = fixture((c) => {
      c.examples.request.valid.push([] as never);
    });

    assert.match(problemsOf(dir), /examples\.request\.valid\[1\]/);
  });

  it("3 · rechaza un ejemplo inválido que valida", () => {
    const { dir } = fixture((c) => {
      c.examples.response.invalid.push({ ok: true } as never);
    });

    assert.match(problemsOf(dir), /examples\.response\.invalid\[1\]/);
  });

  it("4 · rechaza un id distinto del nombre del archivo", () => {
    const { dir } = fixture((c) => {
      c.id = "otro_nombre";
    });

    assert.match(problemsOf(dir), /id "otro_nombre".*demo_echo/);
  });

  it("4 · rechaza ids duplicados", () => {
    const { dir } = fixture();
    mkdirSync(join(dir, "otro"));
    writeFileSync(join(dir, "otro", "demo_echo.json"), JSON.stringify(baseContract));

    assert.match(problemsOf(dir), /id duplicado "demo_echo"/);
  });

  it("4 · rechaza códigos de error duplicados en un contrato", () => {
    const { dir } = fixture((c) => {
      c.errors.push({ code: "INVALID_ARGUMENT", grpcStatus: 3, when: "Otra vez." });
    });

    assert.match(problemsOf(dir), /código de error duplicado "INVALID_ARGUMENT"/);
  });

  it("5 · rechaza un cambio normativo sin subir la versión", () => {
    const { dir } = fixture((c) => {
      c.maxRequestBytes = 512;
    });

    assert.match(problemsOf(dir), /demo_echo@1.*huella.*sube "version"/);
  });

  it("5 · acepta un cambio no normativo (description) sin subir la versión", () => {
    const { dir } = fixture((c) => {
      c.description = "Otra descripción.";
    });

    assert.deepEqual(checkContracts(dir), []);
  });

  it("5 · rechaza un contrato sin huella registrada", () => {
    const { dir } = fixture(undefined, { lock: false });

    assert.match(problemsOf(dir), /demo_echo@1.*contracts\.lock\.json.*contracts:lock/);
  });

  it("6 · rechaza la ausencia de protocol.json", () => {
    const { dir } = fixture();
    rmSync(join(dir, "protocol.json"));

    assert.match(problemsOf(dir), /protocol\.json/);
  });
});

describe("lockContracts", () => {
  it("registra la huella con la clave id@version", () => {
    const { dir } = fixture(undefined, { lock: false });

    lockContracts(dir);

    const lock = JSON.parse(readFileSync(join(dir, "contracts.lock.json"), "utf8"));
    assert.deepEqual(Object.keys(lock), ["demo_echo@1"]);
    assert.match(lock["demo_echo@1"], /^sha256:[0-9a-f]{64}$/);
  });

  it("conserva las entradas anteriores al subir la versión", () => {
    const { dir } = fixture((c) => {
      c.version = 2;
      c.maxRequestBytes = 512;
    });

    lockContracts(dir);

    const lock = JSON.parse(readFileSync(join(dir, "contracts.lock.json"), "utf8"));
    assert.deepEqual(Object.keys(lock), ["demo_echo@1", "demo_echo@2"]);
    assert.deepEqual(checkContracts(dir), []);
  });

  it("se niega a sobrescribir la huella de un id@version ya registrado", () => {
    const { dir } = fixture((c) => {
      c.maxRequestBytes = 512;
    });

    assert.throws(() => lockContracts(dir), /demo_echo@1.*ya registrad.*sube "version"/);
  });

  it("la huella no depende del orden de las claves ni de la descripción", () => {
    const a = structuredClone(baseContract);
    const b = { ...structuredClone(baseContract), description: "otra" };
    const reordered = Object.fromEntries(Object.entries(a).reverse());

    assert.equal(fingerprint(a), fingerprint(b));
    assert.equal(fingerprint(a), fingerprint(reordered));
  });
});

describe("checkGeneratedCSharp", () => {
  const csharpOptions = {
    namespace: "Ecosystem.Contracts",
    includeProtocol: true,
    includeRuntime: true,
  };

  it("acepta un C# generado al día", () => {
    const { dir } = fixture();
    const outFile = join(dir, "Contracts.g.cs");
    writeFileSync(outFile, generateCSharpSource(dir, csharpOptions));

    assert.deepEqual(checkGeneratedCSharp([{ contractsDir: dir, outFile, ...csharpOptions }]), []);
  });

  it("rechaza un C# desactualizado", () => {
    const { dir } = fixture();
    const outFile = join(dir, "Contracts.g.cs");
    writeFileSync(outFile, generateCSharpSource(dir, csharpOptions));
    writeFileSync(join(dir, "protocol.json"), JSON.stringify({ protocolVersion: 2 }));

    assert.match(
      checkGeneratedCSharp([{ contractsDir: dir, outFile, ...csharpOptions }]).join("\n"),
      /Contracts\.g\.cs desactualizado: ejecuta npm run generate/,
    );
  });

  it("rechaza un C# que no existe", () => {
    const { dir } = fixture();

    assert.match(
      checkGeneratedCSharp([
        { contractsDir: dir, outFile: join(dir, "Falta.g.cs"), ...csharpOptions },
      ]).join("\n"),
      /Falta\.g\.cs desactualizado/,
    );
  });
});
