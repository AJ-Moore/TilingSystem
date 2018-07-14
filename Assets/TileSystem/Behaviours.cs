using UnityEngine;
using UnityEditor;

public class JCBehaviourAsset {
    [MenuItem("Assets/Create/CodenameSpace/JCBehaviour")]
    public static void CreateAsset() {
        ScriptableObjectUtility.CreateAsset<JohnConwayBehaviour>();
    }
}
