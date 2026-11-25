import assert from "node:assert/strict";
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { after, describe, it } from "node:test";
import { findForbiddenImports } from "../../scripts/check-imports.ts";

const root = mkdtempSync(join(tmpdir(), "check-imports-test-"));
after(() => rmSync(root, { recursive: true, force: true }));

let counter = 0;

function srcWith(files: Record<string, string>): string {
  const src = join(root, `src-${counter++}`);
  for (const [name, content] of Object.entries(files)) {
    const path = join(src, name);
    mkdirSync(join(path, ".."), { recursive: true });
    writeFileSync(path, content);
  }
  return src;
}

describe("findForbiddenImports", () => {
  it("acepta imports relativos, de generated/ y de tipos de nakama-runtime", () => {
    const src = srcWith({
      "main.ts": [
        'import { a } from "./a.ts";',
        'import { validate } from "./generated/contracts.js";',
        'import type { X } from "nakama-runtime";',
        'export { b } from "../otro/b.ts";',
      ].join("\n"),
      "a.ts": "export const a = 1;\n",
    });

    assert.deepEqual(findForbiddenImports(src), []);
  });

  it("rechaza imports de paquetes, nombrando archivo e import", () => {
    const src = srcWith({
      "main.ts": 'import Ajv from "ajv";\nimport { x } from "./x.ts";\n',
      "rpc/uno.ts": 'export * from "lodash";\n',
      "rpc/dos.ts": 'const m = require("fs");\nconst n = import("node:path");\n',
    });

    const found = findForbiddenImports(src).map((f) => `${f.file}:${f.specifier}`);

    assert.deepEqual(found.sort(), [
      "main.ts:ajv",
      "rpc/dos.ts:fs",
      "rpc/dos.ts:node:path",
      "rpc/uno.ts:lodash",
    ]);
  });

  it("rechaza imports de valor (no de tipo) de nakama-runtime", () => {
    const src = srcWith({ "main.ts": 'import { Codes } from "nakama-runtime";\n' });

    assert.deepEqual(
      findForbiddenImports(src).map((f) => f.specifier),
      ["nakama-runtime"],
    );
  });

  it("ignora src/generated/", () => {
    const src = srcWith({ "generated/contracts.js": 'const x = require("ajv/dist/runtime/x");\n' });

    assert.deepEqual(findForbiddenImports(src), []);
  });
});
