// Punto de entrada del módulo para la VM JavaScript de Nakama (capa fina, sin lógica propia).
// Nakama busca InitModule y las funciones RPC en el ámbito global y analiza el código para
// localizarlas: deben declararse con `function` en el nivel superior (research.md R-2).

import { toNakamaError } from "./errors.ts";
import { MODULE_VERSION } from "./generated/contracts.js";
import { handleProtocolVersion } from "./rpc/protocolVersion.ts";

function rpcProtocolVersion(
  ctx: nkruntime.Context,
  _logger: nkruntime.Logger,
  _nk: nkruntime.Nakama,
  payload: string,
): string {
  const outcome = handleProtocolVersion(payload, ctx.userId);
  if (!outcome.ok) {
    throw toNakamaError(outcome.grpcStatus, outcome.error);
  }
  return JSON.stringify(outcome.response);
}

function InitModule(
  _ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  _nk: nkruntime.Nakama,
  initializer: nkruntime.Initializer,
): void {
  // Debe coincidir con el id del contrato shared/contracts/rpc/protocol_version.json.
  initializer.registerRpc("protocol_version", rpcProtocolVersion);
  logger.info("ecosystem-server %s cargado: RPC protocol_version registrado", MODULE_VERSION);
}

// Referencia para que el empaquetado no elimine InitModule (guía oficial de Nakama).
!InitModule && InitModule.bind(null);
