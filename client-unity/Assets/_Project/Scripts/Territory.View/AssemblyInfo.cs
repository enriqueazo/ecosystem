using System.Runtime.CompilerServices;

// Las pruebas leen las piezas y fijan disposiciones sin ampliar la API pública (principio IV).
[assembly: InternalsVisibleTo("Ecosystem.Tests.EditMode")]
[assembly: InternalsVisibleTo("Ecosystem.Tests.PlayMode")]
[assembly: InternalsVisibleTo("Ecosystem.Tests.Integration")]
