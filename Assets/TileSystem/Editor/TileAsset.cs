using UnityEngine;
using UnityEditor;

public class TileAsset {
    [MenuItem("Assets/Create/CodenameSpace/TileSystem/Tile")]
    public static void CreateAsset() {
        ScriptableObjectUtility.CreateAsset<TileSystem.Tile>();
    }
}