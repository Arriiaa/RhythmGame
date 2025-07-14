using UnityEngine;
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "CharacterJudgmentConfig", menuName = "音乐节拍/角色判定配置", order = 1)]
public class CharacterJudgmentConfig : ScriptableObject
{
    [Serializable]
    public class CharacterJudgmentData
    {
        [LabelText("角色名称")]
        [Tooltip("角色的名称标识")]
        public string characterName;
        
        [LabelText("判定图片组件名称")]
        [Tooltip("角色下用于显示判定图片的Image组件名称")]
        public string judgmentImageName = "JudgmentImage";
        
        [LabelText("判定图片数组")]
        [Tooltip("角色判定时可显示的图片数组")]
        public Sprite[] perfectImages;
        
        [LabelText("判定按键")]
        [Tooltip("角色的判定按键")]
        public KeyCode keyCode = KeyCode.None;
    }
    
    [LabelText("角色判定配置列表")]
    [Tooltip("各角色的判定图片和按键配置")]
    [TableList]
    public List<CharacterJudgmentData> characterConfigs = new List<CharacterJudgmentData>();
    
    // 通过角色名称获取判定图片数组
    public Sprite[] GetPerfectImagesForCharacter(string characterName)
    {
        foreach (var config in characterConfigs)
        {
            if (config.characterName == characterName)
            {
                return config.perfectImages;
            }
        }
        
        // 调试信息已移除
        return null;
    }
    
    // 通过角色名称获取Good判定图片数组（现在使用perfectImages）
    public Sprite[] GetGoodImagesForCharacter(string characterName)
    {
        // 直接使用Perfect图片数组
        return GetPerfectImagesForCharacter(characterName);
    }
    
    // 通过角色名称获取Miss判定图片数组（现在使用perfectImages）
    public Sprite[] GetMissImagesForCharacter(string characterName)
    {
        // 直接使用Perfect图片数组
        return GetPerfectImagesForCharacter(characterName);
    }
    
    // 通过角色名称获取按键
    public KeyCode GetKeyCodeForCharacter(string characterName)
    {
        KeyCode result = KeyCode.None;
        
        // 首先尝试精确匹配
        foreach (var config in characterConfigs)
        {
            if (config.characterName == characterName)
            {
                result = config.keyCode;
                break;
            }
        }
        
        // 如果精确匹配失败，尝试忽略大小写的匹配
        if (result == KeyCode.None)
        {
            foreach (var config in characterConfigs)
            {
                if (string.Equals(config.characterName, characterName, StringComparison.OrdinalIgnoreCase))
                {
                    // 调试信息已移除
                    result = config.keyCode;
                    break;
                }
            }
        }
        
        // 如果还是失败，尝试部分匹配（GameObject名称可能包含额外的信息）
        if (result == KeyCode.None)
        {
            foreach (var config in characterConfigs)
            {
                if (characterName.Contains(config.characterName) || config.characterName.Contains(characterName))
                {
                    // 调试信息已移除
                    result = config.keyCode;
                    break;
                }
            }
        }
        
        // 如果找不到配置或配置为Space，返回None
        if (result == KeyCode.None)
        {
            // 调试信息已移除
        }
        else if (result == KeyCode.Space)
        {
            // 调试信息已移除
            result = KeyCode.None;
        }
        
        return result;
    }
} 