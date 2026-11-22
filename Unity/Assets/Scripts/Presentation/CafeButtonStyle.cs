using UnityEngine;
using UnityEngine.UI;

namespace NightCafe
{
    [RequireComponent(typeof(Button), typeof(Image))]
    public sealed class CafeButtonStyle : MonoBehaviour
    {
        public Sprite idleSprite, selectedSprite;
        public void Select(bool selected)
        {
            var image = GetComponent<Image>();
            image.sprite = selected ? selectedSprite : idleSprite;
            image.color = Color.white;
            foreach (var label in GetComponentsInChildren<Text>())
                label.color = selected ? CafeUi.Ink : CafeUi.Paper;
        }
    }
}
