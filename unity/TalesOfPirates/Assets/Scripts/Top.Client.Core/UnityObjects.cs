using UnityEngine;

namespace Top.Client.Core
{
    public static class UnityObjects
    {
        public static void Destroy(Object destroyed)
        {
            if (destroyed == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(destroyed);
            }
            else
            {
                Object.DestroyImmediate(destroyed);
            }
        }
    }
}
