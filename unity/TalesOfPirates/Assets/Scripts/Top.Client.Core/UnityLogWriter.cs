using System;
using Top.Logging;
using UnityEngine;

namespace Top.Client.Core
{
    public class UnityLogWriter : ILogWriter
    {
        private const string Tag = "[Top] ";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
            Log.Writer = new UnityLogWriter();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void InstallInEditor()
        {
            Install();
        }
#endif

        public void Write(LogLevel level, string message, Exception exception)
        {
            var text = exception == null ? Tag + message : $"{Tag}{message}: {exception}";

            switch (level)
            {
                case LogLevel.Warning:
                    Debug.LogWarning(text);

                    break;

                case LogLevel.Error:
                    Debug.LogError(text);

                    break;

                default:
                    Debug.Log(text);

                    break;
            }
        }
    }
}
