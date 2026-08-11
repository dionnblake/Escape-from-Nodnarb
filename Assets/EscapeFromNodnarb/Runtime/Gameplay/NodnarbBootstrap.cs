using UnityEngine;

namespace EscapeFromNodnarb
{
    public static class NodnarbBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            EnsureGame();
        }

        public static NodnarbGame EnsureGame()
        {
            NodnarbGame existing = Object.FindFirstObjectByType<NodnarbGame>();
            if (existing != null)
            {
                return existing;
            }

            GameObject root = new GameObject("EscapeFromNodnarb");
            Object.DontDestroyOnLoad(root);
            return root.AddComponent<NodnarbGame>();
        }
    }
}
