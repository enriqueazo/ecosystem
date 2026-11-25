// FR-004: el código de src/ (excepto src/generated/) solo puede importar módulos relativos y,
// únicamente como tipos, "nakama-runtime". Cualquier paquete se empaquetaría en el artefacto
// que carga Nakama, que no admite APIs de Node ni dependencias sin justificar (ADR-0008).
// Uso: node scripts/check-imports.ts [directorio]   (por defecto src/)

import { readdirSync, readFileSync } from "node:fs";
import { dirname, join, relative, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";

export interface ForbiddenImport {
  file: string;
  specifier: string;
}

const STATIC_IMPORT = /^\s*(import|export)\s+(type\s+)?(?:[^;'"]*?\s+from\s+)?["']([^"']+)["']/gm;
const DYNAMIC_IMPORT = /\b(?:import|require)\s*\(\s*["']([^"']+)["']\s*\)/g;

function isRelative(specifier: string): boolean {
  return specifier.startsWith("./") || specifier.startsWith("../");
}

function sourceFiles(srcDir: string): string[] {
  return readdirSync(srcDir, { recursive: true, withFileTypes: true })
    .filter((entry) => entry.isFile() && /\.(ts|js|mjs)$/.test(entry.name))
    .map((entry) => join(entry.parentPath, entry.name))
    .filter((path) => relative(srcDir, path).split(sep)[0] !== "generated");
}

export function findForbiddenImports(srcDir: string): ForbiddenImport[] {
  const found: ForbiddenImport[] = [];
  for (const path of sourceFiles(srcDir)) {
    const file = relative(srcDir, path).split(sep).join("/");
    const source = readFileSync(path, "utf8");
    for (const match of source.matchAll(STATIC_IMPORT)) {
      const isTypeOnly = match[2] !== undefined;
      const specifier = match[3];
      if (isRelative(specifier)) continue;
      if (specifier === "nakama-runtime" && isTypeOnly) continue;
      found.push({ file, specifier });
    }
    for (const match of source.matchAll(DYNAMIC_IMPORT)) {
      if (!isRelative(match[1])) found.push({ file, specifier: match[1] });
    }
  }
  return found;
}

if (import.meta.main) {
  const serverRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
  const srcDir = process.argv[2] ?? join(serverRoot, "src");
  const found = findForbiddenImports(srcDir);
  if (found.length === 0) {
    console.log("check:imports OK: src/ solo importa módulos relativos y tipos de nakama-runtime");
  } else {
    for (const { file, specifier } of found) {
      console.error(`src/${file}: import no permitido "${specifier}" (FR-004, ADR-0008)`);
    }
    process.exitCode = 1;
  }
}
