import assert from "node:assert/strict";
import { mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { after, describe, it } from "node:test";
import { checkEs5File } from "../../scripts/check-es5.ts";

const dir = mkdtempSync(join(tmpdir(), "check-es5-test-"));
after(() => rmSync(dir, { recursive: true, force: true }));

function fileWith(name: string, source: string): string {
  const path = join(dir, name);
  writeFileSync(path, source);
  return path;
}

describe("checkEs5File", () => {
  it("acepta código ES5", () => {
    const path = fileWith("ok.js", "function InitModule(a) { var x = [a]; return x.length; }\n");

    assert.deepEqual(checkEs5File(path), { ok: true });
  });

  for (const [name, source] of [
    ["const", "var a = 1;\nconst b = 2;\n"],
    ["función flecha", "var a = 1;\nvar f = () => a;\n"],
    ["clase", "var a = 1;\nclass C {}\n"],
  ] as const) {
    it(`rechaza ${name} indicando línea y columna`, () => {
      const path = fileWith(`${name}.js`, source);

      const result = checkEs5File(path);

      assert.equal(result.ok, false);
      assert.match(result.ok ? "" : result.message, /:2:\d+/);
    });
  }

  it("informa de un archivo inexistente", () => {
    const result = checkEs5File(join(dir, "no-existe.js"));

    assert.equal(result.ok, false);
    assert.match(result.ok ? "" : result.message, /no-existe\.js/);
  });
});
