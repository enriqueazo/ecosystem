using System;
using System.IO;
using UnityEngine;

namespace Ecosystem.Connection.Nakama
{
    /// <summary>
    /// Lee la configuración de conexión del entorno o de client-unity/LocalConfig/connection.local.json
    /// (fuera de Assets/: no se empaqueta en las builds). Reglas: contracts/client-config.md.
    /// </summary>
    public static class ConnectionConfigLoader
    {
        public static string LocalFilePath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "LocalConfig", "connection.local.json"));

        public static ConfigResult Load()
        {
            string fileText = null;
            try
            {
                if (File.Exists(LocalFilePath)) fileText = File.ReadAllText(LocalFilePath);
            }
            catch (IOException)
            {
                // Se trata como configuración ausente.
            }
            return ConnectionConfigParser.Load(Environment.GetEnvironmentVariable, fileText);
        }
    }
}
