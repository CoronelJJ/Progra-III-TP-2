using TurnGame.Flow.Services;
using UnityEngine;

namespace TurnGame.Unity
{
    public sealed class UnityApplicationQuitter : IApplicationQuitter
    {
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
