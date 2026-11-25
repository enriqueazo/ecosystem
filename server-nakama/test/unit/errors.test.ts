import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { describe, it } from "node:test";
import { GrpcStatus, toNakamaError } from "../../src/errors.ts";

// nkruntime.Codes es un const enum: no existe en ejecución, así que se compara con su definición.
function officialCode(name: string): number {
  const dts = readFileSync(
    new URL("../../node_modules/nakama-runtime/index.d.ts", import.meta.url),
    "utf8",
  );
  const match = dts.match(new RegExp(`\\b${name}\\s*=\\s*(\\d+)`));
  assert.ok(match, `${name} no aparece en nakama-runtime/index.d.ts`);
  return Number(match[1]);
}

describe("GrpcStatus", () => {
  for (const name of ["INVALID_ARGUMENT", "PERMISSION_DENIED", "UNAUTHENTICATED"] as const) {
    it(`${name} coincide con nkruntime.Codes`, () => {
      assert.equal(GrpcStatus[name], officialCode(name));
    });
  }
});

describe("toNakamaError", () => {
  it("transporta el error estándar como JSON en message, con el código gRPC en code", () => {
    const error = toNakamaError(GrpcStatus.INVALID_ARGUMENT, {
      code: "INVALID_ARGUMENT",
      message: "La petición debe ser un objeto JSON.",
    });

    assert.equal(error.code, 3);
    assert.deepEqual(JSON.parse(error.message), {
      code: "INVALID_ARGUMENT",
      message: "La petición debe ser un objeto JSON.",
    });
  });

  it("el mensaje contiene solo code y message", () => {
    const error = toNakamaError(GrpcStatus.PERMISSION_DENIED, {
      code: "SESSION_REQUIRED",
      message: "x",
    });

    assert.deepEqual(Object.keys(JSON.parse(error.message)).sort(), ["code", "message"]);
  });
});
