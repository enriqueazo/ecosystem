// Lógica pura del RPC técnico protocol_version (sin dependencias del host de Nakama).
// Contrato: shared/contracts/rpc/protocol_version.json.
// Comportamiento y orden de validación: specs/002-servidor-typescript-ci/contracts/rpc-protocol-version.md

import type { StandardError } from "../errors.ts";
import {
  MODULE_VERSION,
  PROTOCOL_VERSION,
  PROTOCOL_VERSION_CONTRACT,
  validateProtocolVersionRequest,
} from "../generated/contracts.js";
import { parseJsonPayload, utf8ByteLength } from "../payload.ts";

export interface ProtocolVersionResponse {
  protocolVersion: number;
  moduleVersion: string;
}

export type ProtocolVersionOutcome =
  | { ok: true; response: ProtocolVersionResponse }
  | { ok: false; grpcStatus: number; error: StandardError };

const contractErrors = PROTOCOL_VERSION_CONTRACT.errors;

function failure(grpcStatus: number, code: string, message: string): ProtocolVersionOutcome {
  return { ok: false, grpcStatus, error: { code, message } };
}

export function handleProtocolVersion(
  payload: string,
  userId: string | undefined,
): ProtocolVersionOutcome {
  if (!userId) {
    return failure(
      contractErrors.SESSION_REQUIRED,
      "SESSION_REQUIRED",
      "Este RPC requiere una sesión de usuario.",
    );
  }

  if (utf8ByteLength(payload) > PROTOCOL_VERSION_CONTRACT.maxRequestBytes) {
    return failure(
      contractErrors.PAYLOAD_TOO_LARGE,
      "PAYLOAD_TOO_LARGE",
      `La petición supera ${PROTOCOL_VERSION_CONTRACT.maxRequestBytes} bytes.`,
    );
  }

  const parsed = parseJsonPayload(payload);
  if (!parsed.ok || !validateProtocolVersionRequest(parsed.value)) {
    return failure(
      contractErrors.INVALID_ARGUMENT,
      "INVALID_ARGUMENT",
      "La petición debe ser un objeto JSON.",
    );
  }

  return {
    ok: true,
    response: { protocolVersion: PROTOCOL_VERSION, moduleVersion: MODULE_VERSION },
  };
}
