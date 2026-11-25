// Casos compartidos entre las pruebas unitarias (función pura) y las de integración (RPC real en
// Nakama), para demostrar que el comportamiento coincide (spec 002, US2 escenario 6).
// Fuente: specs/002-servidor-typescript-ci/contracts/rpc-protocol-version.md → Casos compartidos.

export type ExpectedOutcome =
  | { ok: true }
  | { ok: false; code: "SESSION_REQUIRED" | "PAYLOAD_TOO_LARGE" | "INVALID_ARGUMENT" };

export interface ProtocolVersionCase {
  name: string;
  payload: string;
  session: boolean;
  expected: ExpectedOutcome;
}

const MAX_REQUEST_BYTES = 1024;

// {"p":""} ocupa 8 bytes; el relleno completa el tamaño pedido.
function objectOfBytes(totalBytes: number, unit: string, unitBytes: number): string {
  const fillBytes = totalBytes - 8;
  const units = Math.floor(fillBytes / unitBytes);
  const rest = fillBytes - units * unitBytes;
  return JSON.stringify({ p: unit.repeat(units) + "a".repeat(rest) });
}

export const protocolVersionCases: ProtocolVersionCase[] = [
  { name: "cuerpo vacío", payload: "", session: true, expected: { ok: true } },
  { name: "objeto vacío", payload: "{}", session: true, expected: { ok: true } },
  {
    name: "campos desconocidos",
    payload: '{"foo":1,"bar":"x"}',
    session: true,
    expected: { ok: true },
  },
  {
    name: "no es JSON",
    payload: "{no-json",
    session: true,
    expected: { ok: false, code: "INVALID_ARGUMENT" },
  },
  {
    name: "array",
    payload: "[]",
    session: true,
    expected: { ok: false, code: "INVALID_ARGUMENT" },
  },
  {
    name: "número",
    payload: "42",
    session: true,
    expected: { ok: false, code: "INVALID_ARGUMENT" },
  },
  {
    name: "cadena",
    payload: '"x"',
    session: true,
    expected: { ok: false, code: "INVALID_ARGUMENT" },
  },
  {
    name: "null",
    payload: "null",
    session: true,
    expected: { ok: false, code: "INVALID_ARGUMENT" },
  },
  {
    name: "ASCII de 1025 bytes",
    payload: objectOfBytes(MAX_REQUEST_BYTES + 1, "a", 1),
    session: true,
    expected: { ok: false, code: "PAYLOAD_TOO_LARGE" },
  },
  {
    name: "multibyte de exactamente 1024 bytes",
    payload: objectOfBytes(MAX_REQUEST_BYTES, "😀", 4),
    session: true,
    expected: { ok: true },
  },
  {
    name: "multibyte de 1025 bytes",
    payload: objectOfBytes(MAX_REQUEST_BYTES + 1, "😀", 4),
    session: true,
    expected: { ok: false, code: "PAYLOAD_TOO_LARGE" },
  },
  {
    name: "sin sesión de usuario",
    payload: "{}",
    session: false,
    expected: { ok: false, code: "SESSION_REQUIRED" },
  },
];
