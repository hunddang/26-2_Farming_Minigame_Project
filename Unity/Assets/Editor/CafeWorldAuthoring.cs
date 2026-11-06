using NightCafe;

// Scene authoring reuses the saved, editable 3D world rather than regenerating its layout.
public static class CafeWorldAuthoring
{
    public static CafeWorld Instantiate() => Cafe3DConversion.InstantiateWorld();
}
