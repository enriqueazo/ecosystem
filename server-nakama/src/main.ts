// Provisional (spec 002, T006): se sustituye por la capa real en T027.
function InitModule(
  _ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  _nk: nkruntime.Nakama,
  _initializer: nkruntime.Initializer,
): void {
  logger.info("ecosystem-server: módulo provisional cargado");
}

// Referencia para que el empaquetado no elimine InitModule (guía oficial de Nakama).
!InitModule && InitModule.bind(null);
