using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CharacterJudgmentConfig))]
public class CharacterJudgmentConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 绘制默认的Inspector
        DrawDefaultInspector();
        
        // 获取目标对象
        CharacterJudgmentConfig config = (CharacterJudgmentConfig)target;
        
        // 如果有更改，标记为已修改
        if (GUI.changed)
        {
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            // 调试信息已移除
        }
        
        // 添加保存按钮
        if (GUILayout.Button("保存配置"))
        {
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            // 调试信息已移除
        }
    }
} 