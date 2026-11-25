// Empaqueta src/main.ts en un único build/index.js ES5 para la VM JavaScript (goja) de Nakama.
// Decisión y restricciones: specs/002-servidor-typescript-ci/research.md (R-2) y ADR-0006.
// El código no importa paquetes en ejecución (FR-004, check:imports), así que no hace falta
// resolver node_modules: Rollup solo une src/ y src/generated/.
import { babel } from "@rollup/plugin-babel";

export default {
  input: "src/main.ts",
  external: ["nakama-runtime"],
  // Nakama busca InitModule y las funciones RPC en el ámbito global: sin envoltorio.
  output: { file: "build/index.js", format: "es", strict: false },
  plugins: [babel({ extensions: [".ts", ".js"], babelHelpers: "bundled" })],
};
