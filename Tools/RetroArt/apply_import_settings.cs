// Companion to retro_sprites.py: applies pixel-art import settings to every sprite it generated.
// Run in the open Editor with:
//   unity command eval_file --file Tools/RetroArt/apply_import_settings.cs
// Settings live in each sprite's .meta afterwards, so this only needs re-running when the manifest's
// sprites or pixels-per-unit change.
var manifestPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Tools/RetroArt/manifest.json");
// Eval scripts run inside a method and can't declare types for JsonUtility, so read the flat manifest directly.
var manifestText = System.IO.File.ReadAllText(manifestPath);
var entries = System.Text.RegularExpressions.Regex.Matches(manifestText, "\"path\":\\s*\"([^\"]+)\",\\s*\"ppu\":\\s*([0-9.]+)");
var retroPpu = System.Text.RegularExpressions.Regex.Match(manifestText, "\"retroPpu\":\\s*([0-9]+)").Groups[1].Value;
int updated = 0;
UnityEditor.AssetDatabase.StartAssetEditing();
try
{
    foreach (System.Text.RegularExpressions.Match entry in entries)
    {
        string path = entry.Groups[1].Value;
        float ppu = float.Parse(entry.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
        var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
        if (importer == null)
        {
            Debug.LogWarning("[RetroArt] No texture importer for " + path);
            continue;
        }
        importer.textureType = UnityEditor.TextureImporterType.Sprite;
        importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
        importer.npotScale = UnityEditor.TextureImporterNPOTScale.None;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        // UI sprites (ppu 0) are sized by their RectTransform, so their pixels-per-unit is left alone.
        if (ppu > 0f)
        {
            importer.spritePixelsPerUnit = (float)System.Math.Round(ppu, 2);
        }
        importer.SaveAndReimport();
        updated++;
    }
}
finally
{
    UnityEditor.AssetDatabase.StopAssetEditing();
}
return "[RetroArt] Updated " + updated + " of " + entries.Count + " sprites (retro PPU " + retroPpu + ")";

