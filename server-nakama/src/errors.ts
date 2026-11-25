// Errores del módulo. Formato estándar: shared/README.md → Errores, y
// specs/002-servidor-typescript-ci/data-model.md → Error estándar.

// Códigos gRPC que usa Nakama. nkruntime.Codes es un `const enum` en las definiciones y no existe
// en ejecución tras la transpilación archivo a archivo de Babel, así que se declaran aquí; una
// prueba unitaria los compara con nakama-runtime/index.d.ts.
export const GrpcStatus = {
  INVALID_ARGUMENT: 3,
  PERMISSION_DENIED: 7,
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
