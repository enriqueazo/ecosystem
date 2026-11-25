// Pruebas de integración contra el Nakama del entorno local (deploy/local), con el módulo cargado.
// Recorren la misma tabla de casos que las pruebas unitarias (US2, escenario 6).
// Requisitos y uso: server-nakama/README.md → Pruebas de integración.

import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { before, describe, it } from "node:test";
import { validateProtocolVersionResponse } from "../../src/generated/contracts.js";
import { protocolVersionCases } from "../cases/protocol-version.cases.ts";

const repoRoot = fileURLToPath(new URL("../../../", import.meta.url));
const envFile = `${repoRoot}deploy/local/.env`;

// Lee una variable del entorno o, si no, de deploy/local/.env (sin ejecutarlo).
function setting(name: string, fallback?: string): string {
  const fromEnv = process.env[name];
  if (fromEnv) return fromEnv;
  if (existsSync(envFile)) {
    const line = readFileSync(envFile, "utf8")
      .split("\n")
      .filter((l) => l.trim().startsWith(`${name}=`))
      .pop();
    const value = line?.slice(line.indexOf("=") + 1).trim();
    if (value) return value;
  }
  if (fallback !== undefined) return fallback;
  throw new Error(`Falta ${name} (entorno o deploy/local/.env)`);
}

const port = setting("NAKAMA_API_PORT", "7350");
const baseUrl = `http://127.0.0.1:${port}`;
const packageVersion: string = JSON.parse(
  readFileSync(new URL("../../package.json", import.meta.url), "utf8"),
).version;

const HTTP_FOR_GRPC: Record<number, number> = { 3: 400, 7: 403, 16: 401 };
const GRPC_FOR_CODE: Record<string, number> = {
  SESSION_REQUIRED: 7,
  PAYLOAD_TOO_LARGE: 3,
  INVALID_ARGUMENT: 3,
};

let sessionToken = "";

interface RpcResult {
  status: number;
  body: string;
}

async function callRpc(payload: string, auth: "session" | "http_key" | "none"): Promise<RpcResult> {
  const query =
    auth === "http_key"
      ? `&http_key=${encodeURIComponent(setting("NAKAMA_RUNTIME_HTTP_KEY"))}`
      : "";
  const headers: Record<string, string> = { "Content-Type": "application/json" };
  if (auth === "session") headers.Authorization = `Bearer ${sessionToken}`;
  const response = await fetch(`${baseUrl}/v2/rpc/protocol_version?unwrap${query}`, {
    method: "POST",
    headers,
    body: payload,
  });
  return { status: response.status, body: await response.text() };
}

before(async () => {
  const check = spawnSync(`${repoRoot}tools/check-backend.sh`, ["--module"], { encoding: "utf8" });
  if (check.status !== 0) {
    throw new Error(
      `Entorno local no disponible: consulta deploy/local/README.md.\n${check.stdout}${check.stderr}`,
    );
  }
  const serverKey = setting("NAKAMA_SERVER_KEY");
  const response = await fetch(`${baseUrl}/v2/account/authenticate/device?create=true`, {
    method: "POST",
    headers: {
      Authorization: `Basic ${Buffer.from(`${serverKey}:`).toString("base64")}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ id: "ecosystem-integration-test-0001" }),
  });
  assert.equal(response.status, 200, `autenticación de prueba: ${await response.clone().text()}`);
  sessionToken = ((await response.json()) as { token: string }).token;
});

describe("RPC protocol_version en Nakama (casos compartidos)", () => {
  for (const testCase of protocolVersionCases) {
    it(testCase.name, async () => {
      const result = await callRpc(testCase.payload, testCase.session ? "session" : "http_key");

      if (testCase.expected.ok) {
        assert.equal(result.status, 200, result.body);
        const response = JSON.parse(result.body) as Record<string, unknown>;
        assert.equal(validateProtocolVersionResponse(response), true, result.body);
        assert.deepEqual(Object.keys(response).sort(), ["moduleVersion", "protocolVersion"]);
        assert.equal(response.moduleVersion, packageVersion);
      } else {
        const grpc = GRPC_FOR_CODE[testCase.expected.code];
        assert.equal(result.status, HTTP_FOR_GRPC[grpc], result.body);
        const envelope = JSON.parse(result.body) as { code: number; message: string };
        assert.equal(envelope.code, grpc, result.body);
        const error = JSON.parse(envelope.message) as Record<string, unknown>;
        assert.equal(error.code, testCase.expected.code);
        assert.deepEqual(Object.keys(error).sort(), ["code", "message"]);
      }
    });
  }

  it("sin credenciales lo rechaza Nakama con 401, antes del módulo", async () => {
    const result = await callRpc("{}", "none");

    assert.equal(result.status, 401, result.body);
  });
});
