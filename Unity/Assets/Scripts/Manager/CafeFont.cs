using UnityEngine;
using UnityEngine.UI;

namespace NightCafe
{
    // The prefab supplies Nanum Gothic; an OS font is only a fallback for unconfigured objects.
    [ExecuteAlways]
    public sealed class CafeFont : MonoBehaviour
    {
        public Font overrideFont;
        private void OnEnable() => Refresh();
        private void OnValidate() => Refresh();
        public void Refresh()
        {
            foreach (var text in GetComponentsInChildren<Text>(true))
                text.font = overrideFont != null ? overrideFont : CafeUi.Font;
        }
    }
}
