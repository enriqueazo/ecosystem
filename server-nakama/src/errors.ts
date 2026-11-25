// Errores del módulo. Formato estándar: shared/README.md → Errores, y
// specs/002-servidor-typescript-ci/data-model.md → Error estándar.

// Códigos gRPC que usa Nakama. nkruntime.Codes es un `const enum` en las definiciones y no existe
// en ejecución tras la transpilación archivo a archivo de Babel, así que se declaran aquí; una
// prueba unitaria los compara con nakama-runtime/index.d.ts.
export const GrpcStatus = {
  INVALID_ARGUMENT: 3,
  PERMISSION_DENIED: 7,
  INTERNAL: 13,
  UNAUTHENTICATED: 16,
} as const;

export interface StandardError {
  // Uno de los errors[].code del contrato; estable.
  code: string;
  // Breve, para depuración: sin datos de la petición, secretos ni trazas.
  message: string;
}

export interface NakamaError {
  code: number;
  message: string;
}

// Nakama solo transporta un código gRPC y un mensaje: el error estándar viaja como JSON en message.
export function toNakamaError(grpcStatus: number, error: StandardError): NakamaError {
  return {
    code: grpcStatus,
    message: JSON.stringify({ code: error.code, message: error.message }),
  };
}

const INTERNAL_ERROR: StandardError = { code: "INTERNAL", message: "Error interno del servidor." };

function isNakamaError(value: unknown): value is NakamaError {
  return (
    typeof value === "object" &&
    value !== null &&
    !(value instanceof Error) &&
    typeof (value as NakamaError).code === "number" &&
    typeof (value as NakamaError).message === "string"
  );
}

// Ejecuta la acción de un RPC. Los errores del contrato pasan sin cambios; cualquier otra excepción
// se registra solo en el servidor y se responde como INTERNAL, sin mensaje ni traza internos
// (si no, Nakama devuelve al cliente el stackTrace del módulo).
export function runSafely<T>(action: () => T, logUnexpected: (message: string) => void): T {
  try {
    return action();
  } catch (error) {
    if (isNakamaError(error)) {
      throw error;
    }
    logUnexpected(error instanceof Error ? `${error.name}: ${error.message}` : String(error));
    throw toNakamaError(GrpcStatus.INTERNAL, INTERNAL_ERROR);
  }
}
