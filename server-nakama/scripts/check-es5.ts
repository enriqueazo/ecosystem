// Verifica que build/index.js es ES5 (la VM goja de Nakama no admite sintaxis posterior).
// Uso: node scripts/check-es5.ts [archivo]   (por defecto build/index.js)
// Decisión: specs/002-servidor-typescript-ci/research.md (R-2).

import { readFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { parse } from "acorn";

export type Es5Result = { ok: true } | { ok: false; message: string };

export function checkEs5File(path: string): Es5Result {
  let source: string;
  try {
    source = readFileSync(path, "utf8");
  } catch (error) {
    return { ok: false, message: `No se pudo leer ${path}: ${(error as Error).message}` };
  }
  try {
    parse(source, { ecmaVersion: 5, sourceType: "script", locations: true });
    return { ok: true };
  } catch (error) {
    const err = error as SyntaxError & { loc?: { line: number; column: number } };
    const where = err.loc ? `${err.loc.line}:${err.loc.column + 1}` : "?:?";
    return {
      ok: false,
      message: `${path}:${where}: sintaxis no válida en ES5 (${err.message}). Revisa la transpilación de Babel.`,
    };
  }
}

if (import.meta.main) {
  const serverRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
  const target = process.argv[2] ?? join(serverRoot, "build/index.js");
  const result = checkEs5File(target);
  if (result.ok) {
    console.log(`ES5 OK: ${target}`);
  } else {
    console.error(result.message);
    process.exitCode = 1;
  }
}
