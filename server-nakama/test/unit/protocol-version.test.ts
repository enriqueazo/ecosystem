import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { describe, it } from "node:test";
import {
  PROTOCOL_VERSION,
  validateProtocolVersionResponse,
} from "../../src/generated/contracts.js";
import { handleProtocolVersion } from "../../src/rpc/protocolVersion.ts";
import { protocolVersionCases } from "../cases/protocol-version.cases.ts";

const packageVersion: string = JSON.parse(
  readFileSync(new URL("../../package.json", import.meta.url), "utf8"),
).version;

describe("handleProtocolVersion (casos compartidos)", () => {
  for (const testCase of protocolVersionCases) {
    it(testCase.name, () => {
      const userId = testCase.session ? "00000000-0000-0000-0000-00000000abcd" : "";

      const outcome = handleProtocolVersion(testCase.payload, userId);

      if (testCase.expected.ok) {
        assert.equal(outcome.ok, true, JSON.stringify(outcome));
        if (!outcome.ok) return;
        assert.equal(validateProtocolVersionResponse(outcome.response), true);
        assert.deepEqual(Object.keys(outcome.response).sort(), [
          "moduleVersion",
          "protocolVersion",
        ]);
        assert.equal(outcome.response.protocolVersion, PROTOCOL_VERSION);
        assert.equal(outcome.response.moduleVersion, packageVersion);
      } else {
        assert.equal(outcome.ok, false, JSON.stringify(outcome));
        if (outcome.ok) return;
        assert.equal(outcome.error.code, testCase.expected.code);
      }
    });
  }

  it("trata userId undefined como sin sesión", () => {
    const outcome = handleProtocolVersion("{}", undefined);

    assert.equal(outcome.ok, false);
    assert.equal(outcome.ok ? "" : outcome.error.code, "SESSION_REQUIRED");
  });

  it("usa el código gRPC del contrato para cada error", () => {
    const expected: Record<string, number> = {
      SESSION_REQUIRED: 7,
      PAYLOAD_TOO_LARGE: 3,
      INVALID_ARGUMENT: 3,
    };
    for (const testCase of protocolVersionCases.filter((c) => !c.expected.ok)) {
      const outcome = handleProtocolVersion(testCase.payload, testCase.session ? "u" : "");
      assert.equal(outcome.ok, false);
      if (outcome.ok) continue;
      assert.equal(outcome.grpcStatus, expected[outcome.error.code], testCase.name);
    }
  });

  it("los mensajes de error no incluyen el contenido de la petición", () => {
    const outcome = handleProtocolVersion("{secreto-123", "u");

    assert.equal(outcome.ok, false);
    assert.doesNotMatch(outcome.ok ? "" : outcome.error.message, /secreto-123/);
  });
});
