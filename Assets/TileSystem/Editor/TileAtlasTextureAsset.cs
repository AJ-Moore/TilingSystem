using UnityEngine;
using UnityEditor;

public class TileAtlasTextureAsset {
    [MenuItem("Assets/Create/CodenameSpace/Texture/TileAtlasTexture")]
    public static void CreateAsset() {
        ScriptableObjectUtility.CreateAsset<TileSystem.AtlasTexture>();
    }
}
