using System.Linq;
using NightCafe;
using UnityEngine;

internal static class CafeLightingAuthoring
{
    public static void AddFlashlightPickup(CafeWorld world)
    {
        if (world.FlashlightPickup != null) return;
        var interactions = world.transform.Find("Security Room/Interactions");
        var sprite = CafeWorldAuthoring.Shape("손전등", Vector2.zero, new Vector2(.82f, .41f),
            Color.white, 5, parent: interactions);
        sprite.transform.position = CafeWorld.PlanPoint(CafeWorld.SecurityRoomLayer, 5.55f, 1.98f);
        sprite.gameObject.layer = CafeWorld.SecurityRoomLayer;
        sprite.sprite = CafeSceneAuthoring.Sprite("flashlight-item");
        var pickup = sprite.gameObject.AddComponent<CafeInteractable>();
        pickup.kind = InteractionKind.Flashlight;
        pickup.title = sprite.name;
        pickup.ConfigureMarker(sprite);
        world.FlashlightPickup = pickup;
        world.Interactables.Add(pickup);
    }

    public static void AddSecurityDoorLamp(NightRoutes routes)
    {
        if (routes.securityDoorLamp != null) return;
        const int lobby = 9;
        var door = routes.portals.Single(link => link.item.gameObject.layer == lobby &&
            link.paired.gameObject.layer == CafeWorld.SecurityRoomLayer).item.transform;
        var fixture = CafeWorldAuthoring.Shape("Red Security Door Lamp", new Vector2(.03f, 1.22f),
            new Vector2(.50f, .25f), new Color32(66, 31, 41, 255), 6, parent: door).transform;
        CafeWorldAuthoring.Shape("Red Lens", Vector2.zero, new Vector2(.34f, .16f),
            new Color32(255, 40, 47, 255), 7, parent: fixture);
        CafeWorldAuthoring.Shape("Lens Glint", new Vector2(0, .034f), new Vector2(.20f, .045f),
            new Color32(255, 147, 135, 255), 8, parent: fixture);
        routes.securityDoorLamp = fixture;
    }
}
