import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { describe, it } from "node:test";
import { GrpcStatus, runSafely, toNakamaError } from "../../src/errors.ts";

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
  for (const name of [
    "INVALID_ARGUMENT",
    "PERMISSION_DENIED",
    "INTERNAL",
    "UNAUTHENTICATED",
  ] as const) {
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

describe("runSafely", () => {
  it("devuelve el resultado si no hay errores", () => {
    assert.equal(
      runSafely(
        () => "ok",
        () => assert.fail("no debe registrar"),
      ),
      "ok",
    );
  });

  it("deja pasar los errores del contrato sin cambiarlos", () => {
    const contractError = toNakamaError(GrpcStatus.INVALID_ARGUMENT, { code: "X", message: "y" });

    assert.throws(
      () =>
        runSafely(
          () => {
            throw contractError;
          },
          () => assert.fail("no debe registrar"),
        ),
      (thrown) => thrown === contractError,
    );
  });

  it("convierte una excepción inesperada en INTERNAL sin detalles y la registra en el servidor", () => {
    const logged: string[] = [];

    assert.throws(
      () =>
        runSafely(
          () => {
            throw new TypeError("Cannot add property errors, object is not extensible");
          },
          (message) => logged.push(message),
        ),
      (thrown: { code: number; message: string }) => {
        assert.equal(thrown.code, 13);
        assert.deepEqual(JSON.parse(thrown.message), {
          code: "INTERNAL",
          message: "Error interno del servidor.",
        });
        return true;
      },
    );
    assert.deepEqual(logged, ["TypeError: Cannot add property errors, object is not extensible"]);
  });
});
