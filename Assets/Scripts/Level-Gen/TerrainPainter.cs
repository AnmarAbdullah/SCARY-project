using UnityEngine;
using ScaryGame.LevelGen;

public class TerrainPainter : MonoBehaviour
{
    [Tooltip("Optional per-cube override. If null, uses the profile from LevelConfig.")]
    public TerrainPaintProfile profileOverride;

    private Terrain terrain;

    private void Start()
    {
        terrain = FindObjectOfType<Terrain>();
        PaintBelowObject(this.gameObject);
    }

    [ContextMenu("Paint")]
    public void PainBelowTest()
    {
        if (terrain == null) terrain = FindObjectOfType<Terrain>();
        PaintBelowObject(this.gameObject);
    }

    private TerrainPaintProfile GetProfile()
    {
        if (profileOverride != null) return profileOverride;

        var config = FindObjectOfType<LevelGenerator>()?.config;
        if (config != null && config.terrainPaintProfile != null)
            return config.terrainPaintProfile;

        return null;
    }

    public void PaintBelowObject(GameObject obj)
    {
        TerrainPaintProfile profile = GetProfile();
        if (profile == null)
        {
            Debug.LogWarning($"[TerrainPainter] No paint profile found on {obj.name}. Assign one on LevelConfig or as a local override.", this);
            return;
        }

        TerrainData td = terrain.terrainData;

        Vector3 terrainPos = terrain.transform.position;
        Vector3 objPos = obj.transform.position;
        Vector3 objScale = obj.transform.lossyScale;

        float halfSizeX = objScale.x * 0.5f;
        float halfSizeZ = objScale.z * 0.5f;

        float angleDeg = obj.transform.eulerAngles.y;
        float angleRad = angleDeg * Mathf.Deg2Rad;
        float cosA = Mathf.Cos(angleRad);
        float sinA = Mathf.Sin(angleRad);

        float absCos = Mathf.Abs(cosA);
        float absSin = Mathf.Abs(sinA);
        float boundingHalfX = halfSizeX * absCos + halfSizeZ * absSin;
        float boundingHalfZ = halfSizeX * absSin + halfSizeZ * absCos;

        int mapW = td.alphamapWidth;
        int mapH = td.alphamapHeight;

        float centerU = (objPos.x - terrainPos.x) / td.size.x;
        float centerV = (objPos.z - terrainPos.z) / td.size.z;
        int centerPixX = Mathf.RoundToInt(centerU * mapW);
        int centerPixY = Mathf.RoundToInt(centerV * mapH);

        int boundPixX = Mathf.CeilToInt((boundingHalfX / td.size.x) * mapW);
        int boundPixY = Mathf.CeilToInt((boundingHalfZ / td.size.z) * mapH);

        int startX = Mathf.Clamp(centerPixX - boundPixX, 0, mapW);
        int startY = Mathf.Clamp(centerPixY - boundPixY, 0, mapH);
        int endX   = Mathf.Clamp(centerPixX + boundPixX, 0, mapW);
        int endY   = Mathf.Clamp(centerPixY + boundPixY, 0, mapH);
        int width  = endX - startX;
        int height = endY - startY;

        if (width <= 0 || height <= 0) return;

        float cornerRadius = profile.cornerRadius;
        bool sidesOnly = profile.sidesOnly;
        float halfExtentLocalX = (halfSizeX / td.size.x) * mapW;
        float halfExtentLocalZ = (halfSizeZ / td.size.z) * mapH;
        float cornerPixX = halfExtentLocalX * cornerRadius;
        float cornerPixZ = sidesOnly ? 0f : halfExtentLocalZ * cornerRadius;

        float[,,] maps = td.GetAlphamaps(startX, startY, width, height);

        int textureIndex = profile.textureIndex;
        float strength = profile.strength;
        float noiseInfluence = profile.noiseInfluence;
        float noiseContrast = profile.noiseContrast;
        Texture2D noiseTexture = profile.noiseTexture;
        float fade = profile.fade;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float worldPixX = (startX + x) - centerPixX;
                float worldPixY = (startY + y) - centerPixY;

                float localX =  worldPixX * cosA + worldPixY * sinA;
                float localY = -worldPixX * sinA + worldPixY * cosA;

                if (Mathf.Abs(localX) > halfExtentLocalX || Mathf.Abs(localY) > halfExtentLocalZ)
                    continue;

                float distX = Mathf.Max(0f, Mathf.Abs(localX) - (halfExtentLocalX - cornerPixX));
                float distY = Mathf.Max(0f, Mathf.Abs(localY) - (halfExtentLocalZ - cornerPixZ));
                float cornerDist = Mathf.Sqrt(distX * distX + distY * distY);
                float cornerR = Mathf.Min(cornerPixX, cornerPixZ);

                if (cornerR > 0f && cornerDist > cornerR)
                    continue;

                float normX = halfExtentLocalX > 0f ? 1f - Mathf.Abs(localX) / halfExtentLocalX : 1f;
                float normY = halfExtentLocalZ > 0f ? 1f - Mathf.Abs(localY) / halfExtentLocalZ : 1f;
                float edgeDist = sidesOnly ? normX : Mathf.Min(normX, normY);

                if (noiseTexture != null && noiseInfluence > 0f)
                {
                    if (sidesOnly && normY <= normX)
                    {
                        // pixel is closer to the forward/back edge — skip noise entirely
                    }
                    else
                    {
                        float noiseU = (localX / halfExtentLocalX) * 0.5f + 0.5f;
                        float noiseV = (localY / halfExtentLocalZ) * 0.5f + 0.5f;
                        int texX = Mathf.Clamp(Mathf.RoundToInt(noiseU * (noiseTexture.width - 1)), 0, noiseTexture.width - 1);
                        int texY = Mathf.Clamp(Mathf.RoundToInt(noiseV * (noiseTexture.height - 1)), 0, noiseTexture.height - 1);
                        float sample = noiseTexture.GetPixel(texX, texY).grayscale;
                        sample = Mathf.Clamp01((sample - 0.5f) * noiseContrast + 0.5f);

                        float noiseEat = noiseInfluence * (1f - sample);
                        if (edgeDist < noiseEat)
                            continue;
                    }
                }

                float edgeFactor = 1f;
                if (fade > 0f)
                    edgeFactor = Mathf.Lerp(1f, edgeDist, fade);

                float blend = Mathf.Clamp01(strength * edgeFactor);

                int layers = maps.GetLength(2);

                for (int l = 0; l < layers; l++)
                    maps[y, x, l] *= (1f - blend);

                maps[y, x, textureIndex] += blend;

                float total = 0;
                for (int l = 0; l < layers; l++) total += maps[y, x, l];
                if (total > 0f)
                    for (int l = 0; l < layers; l++) maps[y, x, l] /= total;
            }
        }

        td.SetAlphamaps(startX, startY, maps);
    }
}
