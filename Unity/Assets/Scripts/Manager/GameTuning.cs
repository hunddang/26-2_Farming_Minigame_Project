using UnityEngine;

namespace NightCafe
{
    public enum Ingredient { Milk, Chocolate, Almond, Tapioca, Cream }
    public enum ShiftPhase { Surveillance, Incident, Resolved, Complete, Failed }

    [CreateAssetMenu(menuName = "Night Cafe/Day One Tuning")]
    public sealed class GameTuning : ScriptableObject
    {
        [Min(10)] public float incidentSeconds = 180;
        [Min(0)] public float mistakePenaltySeconds = 20;
        [Min(0)] public float surgeSeconds = 5;
        [Min(1)] public float surgeMultiplier = 2;
        [Min(1)] public float moveSpeed = 4;
        [Min(1)] public float runMultiplier = 1.6f;
        [Min(0.5f)] public float interactionDistance = 1.9f;
        [Min(0)] public float cleanupSeconds = 3;
        public Ingredient[] recipe = { Ingredient.Milk, Ingredient.Chocolate };

        public static string IngredientName(Ingredient ingredient)
        {
            switch (ingredient)
            {
                case Ingredient.Milk: return "우유";
                case Ingredient.Chocolate: return "초콜릿";
                case Ingredient.Almond: return "아몬드";
                case Ingredient.Tapioca: return "타피오카";
                default: return "쿠킹크림";
            }
        }
    }
}
