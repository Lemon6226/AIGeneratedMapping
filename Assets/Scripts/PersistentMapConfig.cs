// statická třída pro předávání dat mezi scénami
// při načtení nové scény se smažou všechny objekty, ale static přežije
public static class PersistentMapConfig
{
    // nastavení z hlavního menu, kolik spavnerů a itemů chceme
    public static bool HasCustomConfig { get; set; } = false;
    public static int SpawnerCount     { get; set; } = 2;
    public static int ItemCount        { get; set; } = 3;

    // reset po přečtení v generátoru mapy
    public static void Reset()
    {
        HasCustomConfig = false;
        SpawnerCount    = 2;
        ItemCount       = 3;
    }
}
