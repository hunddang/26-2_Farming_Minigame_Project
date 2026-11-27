using System;
using System.Linq;
using NightCafe;
using UnityEngine;

internal static class CafeInventoryAuthoring
{
    private const float Width = 1920;
    private const float Height = 1080;
    private const float CellSize = 130;

    public static void Build(NightGameView view)
    {
        var root = view.transform;
        var hand = Slot(root, "Held Item", 104, false);

        var shade = CafeUiAuthoring.Panel(root, "Inventory Overlay", 0, 0, Width, Height, new Color(.035f, .035f, .06f, .68f));
        Stretch(shade.rectTransform);
        shade.raycastTarget = true;
        var inventory = shade.gameObject.AddComponent<InventoryView>();
        inventory.hand = hand;
        view.inventory = inventory;

        var panel = CafeUiAuthoring.Panel(shade.transform, "Inventory Panel", 0, 0, 624, 418, CafeUi.Ink);
        panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = panel.rectTransform.pivot = Vector2.one * .5f;
        panel.rectTransform.anchoredPosition = Vector2.zero;
        CafeUiAuthoring.Label(panel.transform, "인벤토리", 30, 21, 420, 42, 30);
        CafeUiAuthoring.Label(panel.transform, "Tab / Esc", 446, 26, 148, 33, 21, CafeUi.Muted, TextAnchor.MiddleRight);

        var grid = CafeUiAuthoring.Rect(panel.transform, "Slots", 34, 76, 556, 272);
        var layout = grid.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
        layout.cellSize = Vector2.one * CellSize;
        layout.spacing = Vector2.one * 12;
        layout.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 4;
        inventory.cells = new InventoryCell[NightGame.InventoryCapacity];
        for (int index = 0; index < inventory.cells.Length; index++)
            inventory.cells[index] = Slot(grid, $"Slot {index + 1}", CellSize, true);

        CafeUiAuthoring.Label(panel.transform, "우클릭 장착·문서 읽기   ·   좌클릭 드래그 이동",
            24, 369, 576, 28, 17, CafeUi.Muted, TextAnchor.MiddleCenter);

        inventory.dragIcon = CafeUiAuthoring.Panel(shade.transform, "Drag Icon", 0, 0, 68, 68, Color.white);
        inventory.dragIcon.rectTransform.pivot = Vector2.one * .5f;
        inventory.dragIcon.preserveAspect = true;
        inventory.dragIcon.enabled = false;
        inventory.icons = new Sprite[Enum.GetValues(typeof(InventoryItem)).Length];
        void Icon(InventoryItem item, string asset) => inventory.icons[(int)item] = CafeSceneAuthoring.Sprite(asset);
        Icon(InventoryItem.Milk, "milk"); Icon(InventoryItem.Chocolate, "chocolate"); Icon(InventoryItem.Almond, "almond");
        Icon(InventoryItem.Tapioca, "tapioca"); Icon(InventoryItem.Cream, "cream"); Icon(InventoryItem.Recipe, "recipe");
        Icon(InventoryItem.RuleBook, "book"); Icon(InventoryItem.WaterCup, "cup"); Icon(InventoryItem.Cloth, "paper");
        Icon(InventoryItem.Eraser, "note"); Icon(InventoryItem.Extinguisher, "extinguisher"); Icon(InventoryItem.Net, "capture-net");
        Icon(InventoryItem.Anchor, "anchor-pin"); Icon(InventoryItem.Roster, "paper"); Icon(InventoryItem.Accident, "book");
        Icon(InventoryItem.RepairRequest, "recipe"); Icon(InventoryItem.Diagram, "paper"); Icon(InventoryItem.Note, "note");
        Icon(InventoryItem.Badge, "staff-badge"); Icon(InventoryItem.VictimCup, "cup");
        ConfigureEquipmentSlots(inventory);
        shade.gameObject.SetActive(false);
    }

    internal static void ConfigureEquipmentSlots(InventoryView inventory)
    {
        if (inventory.equipment != null) return;
        var hand = inventory.hand;
        var root = inventory.transform.parent;
        var hud = CafeUiAuthoring.Rect(root, "Hand and Equipment HUD", 0, 0, 264, 150);
        hud.anchorMin = hud.anchorMax = hud.pivot = Vector2.zero;
        hud.anchoredPosition = Vector2.zero;
        hud.SetSiblingIndex(hand.transform.GetSiblingIndex());
        hand.transform.SetParent(hud, false);
        inventory.equipment = Slot(hud, "Equipped Item", 104, false);
        PositionHudSlot(hand, 24, "손");
        PositionHudSlot(inventory.equipment, 140, "장비");

        Array.Resize(ref inventory.icons, Enum.GetValues(typeof(InventoryItem)).Length);
        inventory.icons[(int)InventoryItem.Flashlight] = CafeSceneAuthoring.Sprite("flashlight-item");
        var panel = (RectTransform)inventory.transform.Find("Inventory Panel");
        panel.sizeDelta = new Vector2(panel.sizeDelta.x, 432);
        var help = panel.GetComponentsInChildren<UnityEngine.UI.Text>(true)
            .Single(text => text.text.StartsWith("우클릭 "));
        help.name = "Inventory Controls";
        help.text = "우클릭 장착·문서 읽기 · 손/장비 칸 우클릭 해제\n좌클릭 드래그 이동 · Esc 문서 닫기";
        help.rectTransform.anchoredPosition = new Vector2(24, -361);
        help.rectTransform.sizeDelta = new Vector2(576, 52);
    }

    private static void PositionHudSlot(InventoryCell cell, float x, string title)
    {
        var rect = (RectTransform)cell.transform;
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.anchoredPosition = new Vector2(x, 128);
        CafeUiAuthoring.Label(rect, title, 0, -24, rect.sizeDelta.x, 24, 17,
            CafeUi.Paper, TextAnchor.MiddleCenter);
    }

    private static InventoryCell Slot(Transform parent, string name, float size, bool interactive)
    {
        var frame = CafeUiAuthoring.Panel(parent, name, 0, 0, size, size, CafeUi.Muted);
        frame.raycastTarget = interactive;
        var cell = frame.gameObject.AddComponent<InventoryCell>();
        cell.frame = frame;
        CafeUiAuthoring.Panel(frame.transform, "Interior", 2, 2, size - 4, size - 4, new Color32(24, 26, 42, 255));
        cell.icon = CafeUiAuthoring.Panel(frame.transform, "Item Icon", size * .2f, 11, size * .6f, size - 42, Color.white);
        cell.icon.preserveAspect = true;
        cell.icon.enabled = false;
        cell.label = CafeUiAuthoring.Label(frame.transform, "", 4, size - 30, size - 8, 29,
            interactive ? 16 : 14, CafeUi.Paper, TextAnchor.MiddleCenter);
        cell.label.name = "Item Name";
        return cell;
    }

    public static void ConfigurePlayViewport(NightGame game)
    {
        game.mapCamera.rect = new Rect(0, 0, 1, 1);
        game.mapCamera.orthographicSize = Height / (2 * CafeWorld.PixelsPerTile);
        if (game.story == null) return;
        Stretch(game.story.darkness.rectTransform);
        var cinema = (RectTransform)game.story.cinema.transform;
        Stretch(cinema);
        game.story.cinemaFeed.rectTransform.anchoredPosition = new Vector2(555, -110);
        game.story.cinemaCaption.rectTransform.anchoredPosition = new Vector2(305, -630);
    }

    internal static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
