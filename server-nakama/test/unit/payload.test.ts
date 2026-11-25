import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { parseJsonPayload, utf8ByteLength } from "../../src/payload.ts";

describe("utf8ByteLength", () => {
  for (const [text, bytes] of [
    ["", 0],
    ["abc", 3],
    ["ñ", 2],
    ["€", 3],
    ["😀", 4],
    ["a€😀", 8],
  ] as const) {
    it(`"${text}" ocupa ${bytes} bytes`, () => {
      assert.equal(utf8ByteLength(text), bytes);
      assert.equal(utf8ByteLength(text), Buffer.byteLength(text, "utf8"));
    });
  }

  it("cuenta un sustituto aislado como 3 bytes (como lo reemplaza UTF-8)", () => {
    assert.equal(utf8ByteLength("\ud83d"), Buffer.byteLength("\ud83d", "utf8"));
  });
});

describe("parseJsonPayload", () => {
  it("trata la cadena vacía como un objeto vacío", () => {
    assert.deepEqual(parseJsonPayload(""), { ok: true, value: {} });
  });

  it("analiza JSON válido", () => {
    assert.deepEqual(parseJsonPayload('{"a":1}'), { ok: true, value: { a: 1 } });
    assert.deepEqual(parseJsonPayload("[]"), { ok: true, value: [] });
  });

  it("devuelve ok: false con JSON inválido, sin lanzar", () => {
    assert.deepEqual(parseJsonPayload("{no-json"), { ok: false });
  });
});
