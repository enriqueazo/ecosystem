using System;
using System.Collections;
using System.Threading.Tasks;

namespace Ecosystem.Tests.Integration
{
    /// <summary>
    /// Espera a una Task cediendo fotogramas: el adaptador HTTP del SDK usa corrutinas y necesita
    /// el bucle de PlayMode, así que las pruebas de integración son [UnityTest] en PlayMode.
    /// </summary>
    internal static class TaskYield
    {
        public static IEnumerator Await(Task task)
        {
            while (!task.IsCompleted) yield return null;
            task.GetAwaiter().GetResult();
        }

        public static IEnumerator Await<T>(Task<T> task, Action<T> onResult)
        {
            while (!task.IsCompleted) yield return null;
            onResult(task.GetAwaiter().GetResult());
        }
    }
}
