// Empaqueta src/main.ts en un único build/index.js ES5 para la VM JavaScript (goja) de Nakama.
// Decisión y restricciones: specs/002-servidor-typescript-ci/research.md (R-2).
import { babel } from "@rollup/plugin-babel";
import commonjs from "@rollup/plugin-commonjs";
import { nodeResolve } from "@rollup/plugin-node-resolve";

const extensions = [".ts", ".js", ".mjs"];

export default {
  input: "src/main.ts",
  external: ["nakama-runtime"],
  // Nakama busca InitModule y las funciones RPC en el ámbito global: sin envoltorio.
  output: { file: "build/index.js", format: "es", strict: false },
  plugins: [
    nodeResolve({ extensions }),
    commonjs({ extensions: [".js"] }),
    babel({ extensions, babelHelpers: "bundled", exclude: [] }),
  ],
};
