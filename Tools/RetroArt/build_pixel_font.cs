// Companion to ui_sprites.py: sets up the menu sprites' import settings and builds the pixel Font from the
// glyph table it wrote. Run in the open Editor with:
//   unity command eval_file --file Tools/RetroArt/build_pixel_font.cs
// Re-run after changing the glyphs or the UI sprites.
var root = System.IO.Directory.GetCurrentDirectory();
var tableText = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Tools/RetroArt/pixel_font.json"));
UnityEditor.AssetDatabase.Refresh();

// 9-slice borders for the frames (left, bottom, right, top); everything else is a plain point-filtered sprite.
var borders = new System.Collections.Generic.Dictionary<string, Vector4>
{
    { "Assets/Sprites/UI/Panel.png", new Vector4(3, 3, 3, 3) },
    { "Assets/Sprites/UI/ButtonSelected.png", new Vector4(2, 2, 2, 2) },
};
foreach (var name in new[] { "EchoLogo", "EchoLogoRing", "Panel", "ButtonSelected", "BarSegment", "Cursor", "Pixel", "PixelFont", "Dodge" })
{
    string path = "Assets/Sprites/UI/" + name + ".png";
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    importer.textureType = UnityEditor.TextureImporterType.Sprite;
    importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
    importer.filterMode = FilterMode.Point;
    importer.mipmapEnabled = false;
    importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
    importer.npotScale = UnityEditor.TextureImporterNPOTScale.None;
    importer.wrapMode = name == "BarSegment" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
    importer.alphaIsTransparency = true;
    // One UI unit per texel, so an Image at native size is pixel-exact on the pixel-scaled canvas. World-space
    // callouts (Dodge) use the game's retro grid instead.
    importer.spritePixelsPerUnit = name == "Dodge" ? 17 : 1;
    importer.spriteBorder = borders.TryGetValue(path, out var border) ? border : Vector4.zero;
    importer.SaveAndReimport();
}

// Eval scripts can't declare types for JsonUtility, so read the flat glyph table directly.
string Field(string json, string key) => System.Text.RegularExpressions.Regex.Match(json, "\"" + key + "\":\\s*\"?([^\",}]*)").Groups[1].Value;
int size = int.Parse(Field(tableText, "size"));
int glyphHeight = int.Parse(Field(tableText, "glyphHeight"));
int lineHeight = int.Parse(Field(tableText, "lineHeight"));
int spaceAdvance = int.Parse(Field(tableText, "spaceAdvance"));
var infos = new System.Collections.Generic.List<CharacterInfo>();
void AddGlyph(int code, int x, int y, int w, int h, int advance)
{
    // Glyph quads are laid out with y up from the baseline; the glyphs sit on it.
    infos.Add(new CharacterInfo
    {
        index = code,
        uvBottomLeft = new Vector2((float)x / size, (float)y / size),
        uvBottomRight = new Vector2((float)(x + w) / size, (float)y / size),
        uvTopLeft = new Vector2((float)x / size, (float)(y + h) / size),
        uvTopRight = new Vector2((float)(x + w) / size, (float)(y + h) / size),
        minX = 0, maxX = w, minY = 0, maxY = h,
        advance = advance,
    });
}
foreach (System.Text.RegularExpressions.Match glyph in System.Text.RegularExpressions.Regex.Matches(tableText, "\\{[^{}]*\"char\"[^{}]*\\}"))
{
    string json = glyph.Value;
    string ch = System.Text.RegularExpressions.Regex.Match(json, "\"char\":\\s*\"((?:\\\\.|[^\"])*)\"").Groups[1].Value;
    ch = System.Text.RegularExpressions.Regex.Unescape(ch);
    int x = int.Parse(Field(json, "x")), y = int.Parse(Field(json, "y")), w = int.Parse(Field(json, "w")), h = int.Parse(Field(json, "h"));
    int advance = int.Parse(Field(json, "advance"));
    AddGlyph(ch[0], x, y, w, h, advance);
    if (char.IsLetter(ch[0]))
    {
        AddGlyph(char.ToLowerInvariant(ch[0]), x, y, w, h, advance);
    }
}
AddGlyph(' ', 0, 0, 0, 0, spaceAdvance);

const string fontPath = "Assets/Fonts/PixelFont.fontsettings";
const string materialPath = "Assets/Fonts/PixelFont.mat";
if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Fonts"))
{
    UnityEditor.AssetDatabase.CreateFolder("Assets", "Fonts");
}
var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/UI/PixelFont.png");
var material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(materialPath);
if (material == null)
{
    material = new Material(Shader.Find("UI/Default"));
    UnityEditor.AssetDatabase.CreateAsset(material, materialPath);
}
material.mainTexture = texture;
var font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(fontPath);
if (font == null)
{
    font = new Font("PixelFont");
    UnityEditor.AssetDatabase.CreateAsset(font, fontPath);
}
font.material = material;
font.characterInfo = infos.ToArray();
// Font size, ascent and line spacing aren't exposed by the Font API, so set them on the asset.
var serialized = new UnityEditor.SerializedObject(font);
serialized.FindProperty("m_FontSize").floatValue = glyphHeight;
serialized.FindProperty("m_Ascent").floatValue = glyphHeight;
serialized.FindProperty("m_LineSpacing").floatValue = lineHeight;
serialized.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(material);
UnityEditor.EditorUtility.SetDirty(font);
UnityEditor.AssetDatabase.SaveAssets();
return "[RetroArt] Pixel font with " + infos.Count + " glyphs";
