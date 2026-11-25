// Utilidades para la carga de los RPC (solo ES5: se ejecuta en la VM goja de Nakama).

// Bytes que ocupa la cadena en UTF-8, sin APIs de Node (Buffer) ni de la web (TextEncoder).
// Un sustituto UTF-16 aislado cuenta 3 bytes, como el carácter de reemplazo U+FFFD.
export function utf8ByteLength(text: string): number {
  let bytes = 0;
  for (let index = 0; index < text.length; index++) {
    const code = text.charCodeAt(index);
    if (code < 0x80) {
      bytes += 1;
    } else if (code < 0x800) {
      bytes += 2;
    } else if (code >= 0xd800 && code <= 0xdbff && index + 1 < text.length) {
      const next = text.charCodeAt(index + 1);
      if (next >= 0xdc00 && next <= 0xdfff) {
        bytes += 4;
        index++;
      } else {
        bytes += 3;
      }
    } else {
      bytes += 3;
    }
  }
  return bytes;
}

export type ParseResult = { ok: true; value: unknown } | { ok: false };

// Analiza la carga JSON sin lanzar. La cadena vacía equivale a {} (cuerpo vacío del RPC).
export function parseJsonPayload(payload: string): ParseResult {
  if (payload === "") {
    return { ok: true, value: {} };
  }
  try {
    return { ok: true, value: JSON.parse(payload) };
  } catch (_error) {
    return { ok: false };
  }
}
