using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 角色判定配置示例脚本，展示如何在Inspector中配置角色按键
/// </summary>
public class CharacterJudgmentConfigExample : MonoBehaviour
{
    [Header("配置引用")]
    [Tooltip("角色判定配置")]
    public CharacterJudgmentConfig judgmentConfig;
    
    [Header("测试角色")]
    [Tooltip("要测试的角色对象")]
    public GameObject[] testCharacters;
    
    [Header("显示设置")]
    [Tooltip("显示当前按键的文本")]
    public TextMeshProUGUI statusText;
    
    void Start()
    {
        if (judgmentConfig == null)
        {
            return;
        }
        
        // 显示当前配置
        UpdateStatusText();
    }
    
    void Update()
    {
        // 定期更新状态文本
        if (Time.frameCount % 30 == 0) // 每30帧更新一次
        {
            UpdateStatusText();
        }
        
        // 测试角色按键输入
        TestCharacterKeyInput();
    }
    
    // 更新状态文本
    void UpdateStatusText()
    {
        if (statusText == null || judgmentConfig == null || testCharacters == null)
            return;
        
        string info = "角色按键配置:\n";
        
        foreach (var character in testCharacters)
        {
            if (character != null)
            {
                string characterName = character.name;
                KeyCode keyCode = judgmentConfig.GetKeyCodeForCharacter(characterName);
                
                info += $"{characterName}: {keyCode}\n";
            }
        }
        
        statusText.text = info;
    }
    
    // 测试角色按键输入
    void TestCharacterKeyInput()
    {
        if (testCharacters == null || judgmentConfig == null)
            return;
        
        foreach (var character in testCharacters)
        {
            if (character != null)
            {
                string characterName = character.name;
                KeyCode keyCode = judgmentConfig.GetKeyCodeForCharacter(characterName);
                
                if (Input.GetKeyDown(keyCode))
                {
                    // 调试信息已移除
                }
            }
        }
    }
} 