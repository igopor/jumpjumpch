using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class TerrainTexturePainter : EditorWindow
{
    private Terrain terrain;
    private float grassHeight = 5f;
    private float rockSteepness = 30f;
    private float snowHeight = 50f;
    private float sandHeight = 2f;
    private float blendWidth = 2f;

    // Terrain layer references - assign these in the inspector
    private TerrainLayer grassLayer;
    private TerrainLayer rockLayer;
    private TerrainLayer snowLayer;
    private TerrainLayer sandLayer;
    private TerrainLayer grassDryLayer;
    private TerrainLayer soilLayer;

    private bool useVibrantColors = true;
    private float saturationBoost = 1.3f;
    private float brightnessBoost = 1.1f;

    [MenuItem("Tools/Terrain Texture Painter")]
    public static void ShowWindow()
    {
        GetWindow<TerrainTexturePainter>("Terrain Texture Painter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Terrain Texture Painter", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);

        EditorGUILayout.Space();
        GUILayout.Label("Terrain Layers (drag from Project view)", EditorStyles.boldLabel);
        grassLayer = (TerrainLayer)EditorGUILayout.ObjectField("Grass (low-mid height)", grassLayer, typeof(TerrainLayer), false);
        grassDryLayer = (TerrainLayer)EditorGUILayout.ObjectField("Grass Dry (mid height)", grassDryLayer, typeof(TerrainLayer), false);
        rockLayer = (TerrainLayer)EditorGUILayout.ObjectField("Rock (steep)", rockLayer, typeof(TerrainLayer), false);
        snowLayer = (TerrainLayer)EditorGUILayout.ObjectField("Snow (high)", snowLayer, typeof(TerrainLayer), false);
        sandLayer = (TerrainLayer)EditorGUILayout.ObjectField("Sand (low)", sandLayer, typeof(TerrainLayer), false);
        soilLayer = (TerrainLayer)EditorGUILayout.ObjectField("Soil (mid)", soilLayer, typeof(TerrainLayer), false);

        EditorGUILayout.Space();
        GUILayout.Label("Height Settings", EditorStyles.boldLabel);
        sandHeight = EditorGUILayout.FloatField("Sand Max Height", sandHeight);
        grassHeight = EditorGUILayout.FloatField("Grass Max Height", grassHeight);
        snowHeight = EditorGUILayout.FloatField("Snow Min Height", snowHeight);
        rockSteepness = EditorGUILayout.FloatField("Rock Min Steepness (degrees)", rockSteepness);
        blendWidth = EditorGUILayout.FloatField("Blend Width", blendWidth);

        EditorGUILayout.Space();
        GUILayout.Label("Color Settings (Cartoon/Vibrant)", EditorStyles.boldLabel);
        useVibrantColors = EditorGUILayout.Toggle("Boost Saturation/Brightness", useVibrantColors);
        if (useVibrantColors)
        {
            saturationBoost = EditorGUILayout.Slider("Saturation Boost", saturationBoost, 1f, 2f);
            brightnessBoost = EditorGUILayout.Slider("Brightness Boost", brightnessBoost, 1f, 1.5f);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Auto-Assign Layers from Terrain"))
        {
            AutoAssignLayers();
        }

        EditorGUILayout.Space();

        GUI.enabled = terrain != null;
        if (GUILayout.Button("Paint Terrain Textures", GUILayout.Height(40)))
        {
            PaintTerrain();
        }
        GUI.enabled = true;

        if (GUILayout.Button("Boost Layer Colors (Cartoon Effect)", GUILayout.Height(30)))
        {
            BoostLayerColors();
        }
    }

    private void AutoAssignLayers()
    {
        if (terrain == null || terrain.terrainData == null) return;

        TerrainLayer[] layers = terrain.terrainData.terrainLayers;
        foreach (TerrainLayer layer in layers)
        {
            string name = layer.name.ToLower();
            if (name.Contains("grass") && !name.Contains("dry") && !name.Contains("moss") && !name.Contains("soil"))
            {
                if (name.Contains("a") || name.Contains("b"))
                    grassLayer = layer;
            }
            else if (name.Contains("grass") && (name.Contains("dry") || name.Contains("soil")))
            {
                grassDryLayer = layer;
            }
            else if (name.Contains("rock"))
            {
                rockLayer = layer;
            }
            else if (name.Contains("snow"))
            {
                snowLayer = layer;
            }
            else if (name.Contains("sand"))
            {
                sandLayer = layer;
            }
            else if (name.Contains("soil") || name.Contains("mud"))
            {
                soilLayer = layer;
            }
        }

        Repaint();
    }

    private void PaintTerrain()
    {
        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogError("No terrain selected!");
            return;
        }

        TerrainData data = terrain.terrainData;

        // Collect all layers we want to use
        List<TerrainLayer> paintLayers = new List<TerrainLayer>();
        Dictionary<TerrainLayer, int> layerIndexMap = new Dictionary<TerrainLayer, int>();

        System.Action<TerrainLayer> addLayer = (layer) =>
        {
            if (layer != null && !layerIndexMap.ContainsKey(layer))
            {
                layerIndexMap[layer] = paintLayers.Count;
                paintLayers.Add(layer);
            }
        };

        addLayer(sandLayer);
        addLayer(grassLayer);
        addLayer(grassDryLayer);
        addLayer(soilLayer);
        addLayer(rockLayer);
        addLayer(snowLayer);

        if (paintLayers.Count < 2)
        {
            Debug.LogError("Need at least 2 terrain layers to paint!");
            return;
        }

        // Assign layers to terrain data
        data.terrainLayers = paintLayers.ToArray();

        // Get heightmap dimensions
        int hmWidth = data.heightmapResolution;
        int hmHeight = data.heightmapResolution;
        int alphamapWidth = data.alphamapResolution;
        int alphamapHeight = data.alphamapResolution;
        int numLayers = paintLayers.Count;

        // Get heights
        float[,] heights = data.GetHeights(0, 0, hmWidth, hmHeight);

        // Get steepness (normalized)
        float[,] steepness = new float[hmWidth, hmHeight];
        for (int y = 0; y < hmHeight; y++)
        {
            for (int x = 0; x < hmWidth; x++)
            {
                // Calculate steepness from heightmap
                float steep = 0f;
                if (x > 0 && x < hmWidth - 1 && y > 0 && y < hmHeight - 1)
                {
                    float dx = (heights[x + 1, y] - heights[x - 1, y]) * data.size.x / hmWidth;
                    float dz = (heights[x, y + 1] - heights[x, y - 1]) * data.size.z / hmHeight;
                    float normalY = Mathf.Sqrt(1f / (1f + dx * dx + dz * dz));
                    steep = Mathf.Acos(Mathf.Clamp01(normalY)) * Mathf.Rad2Deg;
                }
                steepness[x, y] = steep;
            }
        }

        // Create alphamap
        float[,,] alphamap = new float[alphamapWidth, alphamapHeight, numLayers];

        // For each alphamap pixel, determine which layer to paint
        for (int ay = 0; ay < alphamapHeight; ay++)
        {
            for (int ax = 0; ax < alphamapWidth; ax++)
            {
                // Map alphamap coords to heightmap coords
                float hx = (float)ax / alphamapWidth * hmWidth;
                float hy = (float)ay / alphamapHeight * hmHeight;
                int hx0 = Mathf.FloorToInt(hx);
                int hy0 = Mathf.FloorToInt(hy);
                int hx1 = Mathf.Min(hx0 + 1, hmWidth - 1);
                int hy1 = Mathf.Min(hy0 + 1, hmHeight - 1);
                float fx = hx - hx0;
                float fy = hy - hy0;

                // Bilinear interpolate height and steepness
                float height = Mathf.Lerp(
                    Mathf.Lerp(heights[hx0, hy0], heights[hx1, hy0], fx),
                    Mathf.Lerp(heights[hx0, hy1], heights[hx1, hy1], fx),
                    fy
                );

                float steep = Mathf.Lerp(
                    Mathf.Lerp(steepness[hx0, hy0], steepness[hx1, hy0], fx),
                    Mathf.Lerp(steepness[hx0, hy1], steepness[hx1, hy1], fx),
                    fy
                );

                // Convert height to world units
                float worldHeight = height * data.size.y;

                // Calculate layer weights
                float[] weights = new float[numLayers];
                float totalWeight = 0f;

                for (int i = 0; i < numLayers; i++)
                {
                    TerrainLayer layer = paintLayers[i];
                    string layerName = layer.name.ToLower();
                    float weight = 0f;

                    if (layerName.Contains("sand"))
                    {
                        // Sand in low areas
                        weight = Mathf.Clamp01(1f - (worldHeight - sandHeight) / blendWidth);
                        if (worldHeight > sandHeight + blendWidth) weight = 0f;
                    }
                    else if (layerName.Contains("snow"))
                    {
                        // Snow on high areas
                        weight = Mathf.Clamp01((worldHeight - snowHeight) / blendWidth);
                        if (worldHeight < snowHeight - blendWidth) weight = 0f;
                        // Reduce snow on steep slopes
                        if (steep > 50f) weight *= Mathf.Clamp01(1f - (steep - 50f) / 20f);
                    }
                    else if (layerName.Contains("rock"))
                    {
                        // Rock on steep areas
                        weight = Mathf.Clamp01((steep - rockSteepness) / 15f);
                        // Reduce rock in very low areas (sand zone)
                        if (worldHeight < sandHeight) weight *= Mathf.Clamp01((worldHeight - sandHeight + blendWidth) / blendWidth);
                        // Reduce rock in very high areas (snow zone)
                        if (worldHeight > snowHeight) weight *= Mathf.Clamp01(1f - (worldHeight - snowHeight) / blendWidth);
                    }
                    else if (layerName.Contains("grass") && layerName.Contains("dry"))
                    {
                        // Dry grass on mid-high areas, not too steep
                        weight = Mathf.Clamp01((worldHeight - grassHeight * 0.5f) / blendWidth);
                        if (worldHeight > snowHeight) weight *= Mathf.Clamp01(1f - (worldHeight - snowHeight + blendWidth) / blendWidth);
                        if (steep > rockSteepness) weight *= Mathf.Clamp01(1f - (steep - rockSteepness) / 15f);
                    }
                    else if (layerName.Contains("soil") || layerName.Contains("mud"))
                    {
                        // Soil on gentle mid slopes
                        weight = Mathf.Clamp01(1f - Mathf.Abs(worldHeight - grassHeight * 0.7f) / (grassHeight * 0.5f));
                        if (steep > rockSteepness * 0.7f) weight *= Mathf.Clamp01(1f - (steep - rockSteepness * 0.7f) / 15f);
                    }
                    else if (layerName.Contains("grass"))
                    {
                        // Grass on low-mid areas, gentle slopes
                        weight = Mathf.Clamp01(1f - (worldHeight - grassHeight) / blendWidth);
                        if (worldHeight < sandHeight) weight *= Mathf.Clamp01((worldHeight - sandHeight + blendWidth) / blendWidth);
                        if (steep > rockSteepness) weight *= Mathf.Clamp01(1f - (steep - rockSteepness) / 15f);
                    }
                    else
                    {
                        // Default: distribute based on inverse of other layers
                        weight = 0.1f;
                    }

                    weights[i] = Mathf.Max(0f, weight);
                    totalWeight += weights[i];
                }

                // Normalize weights
                if (totalWeight > 0.001f)
                {
                    for (int i = 0; i < numLayers; i++)
                    {
                        alphamap[ay, ax, i] = weights[i] / totalWeight;
                    }
                }
                else
                {
                    // Fallback: put everything on first layer
                    alphamap[ay, ax, 0] = 1f;
                }
            }
        }

        // Apply alphamap
        data.SetAlphamaps(0, 0, alphamap);

        Debug.Log($"Terrain painted with {numLayers} layers!");
        AssetDatabase.Refresh();
    }

    private void BoostLayerColors()
    {
        if (terrain == null || terrain.terrainData == null) return;

        TerrainLayer[] layers = terrain.terrainData.terrainLayers;
        int modifiedCount = 0;

        foreach (TerrainLayer layer in layers)
        {
            if (layer == null || layer.diffuseTexture == null) continue;

            string path = AssetDatabase.GetAssetPath(layer.diffuseTexture);
            if (string.IsNullOrEmpty(path)) continue;

            // Check if texture is readable
            string assetPath = path;
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) continue;

            bool wasReadable = importer.isReadable;
            importer.isReadable = true;
            importer.SaveAndReimport();

            // Load texture
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null) continue;

            // Get pixels
            Color[] pixels = tex.GetPixels();
            Color[] newPixels = new Color[pixels.Length];

            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];

                if (useVibrantColors)
                {
                    // Boost saturation
                    float gray = c.grayscale;
                    c.r = Mathf.Lerp(gray, c.r, saturationBoost);
                    c.g = Mathf.Lerp(gray, c.g, saturationBoost);
                    c.b = Mathf.Lerp(gray, c.b, saturationBoost);

                    // Boost brightness
                    c.r *= brightnessBoost;
                    c.g *= brightnessBoost;
                    c.b *= brightnessBoost;
                }

                // Clamp
                c.r = Mathf.Clamp01(c.r);
                c.g = Mathf.Clamp01(c.g);
                c.b = Mathf.Clamp01(c.b);
                c.a = 1f;

                newPixels[i] = c;
            }

            tex.SetPixels(newPixels);
            tex.Apply();

            // Save the texture
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(assetPath, bytes);

            // Restore readable state
            importer.isReadable = wasReadable;
            importer.SaveAndReimport();

            modifiedCount++;
        }

        Debug.Log($"Boosted colors for {modifiedCount} terrain layers!");
        AssetDatabase.Refresh();
    }
}
