using UnityEngine;
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

/// <summary>
/// 角色输入管理器
/// 负责处理所有角色的按键输入，与节拍系统解耦
/// </summary>
public class CharacterInputManager : MonoBehaviour
{
    [Serializable]
    public class CharacterInput
    {
        [LabelText("角色名称")]
        [Tooltip("角色的名称标识")]
        public string characterName;
        
        [LabelText("角色对象")]
        [Tooltip("对应的角色游戏对象")]
        public GameObject character;
        
        [LabelText("输入按键")]
        [Tooltip("触发角色动作的按键")]
        public KeyCode keyCode = KeyCode.None;
        
        [HideInInspector]
        public bool wasPressed = false;
        
        [HideInInspector]
        public float lastPressTime = 0f;
    }
    
    [TitleGroup("输入设置")]
    [TableList(ShowIndexLabels = true)]
    [LabelText("角色输入配置")]
    public List<CharacterInput> characterInputs = new List<CharacterInput>();
    
    [TitleGroup("配置设置")]
    [Tooltip("是否从CharacterJudgmentConfig加载按键配置")]
    public bool loadFromConfig = true;
    
    [TitleGroup("配置设置")]
    [Tooltip("是否保存按键配置到CharacterJudgmentConfig")]
    public bool saveToConfig = false;
    
    [TitleGroup("配置设置")]
    [Tooltip("角色判定配置")]
    [ShowIf("loadFromConfig")]
    public CharacterJudgmentConfig judgmentConfig;
    
    [TitleGroup("调试设置")]
    [Tooltip("是否显示调试信息")]
    public bool showDebugInfo = true;
    
    // 禁止使用的按键
    private readonly KeyCode[] forbiddenKeys = new KeyCode[] { KeyCode.Space, KeyCode.Escape, KeyCode.Return };
    
    // 角色输入事件 (角色名称, 按键, 按下时间)
    public event Action<string, KeyCode, float> OnCharacterKeyPressed;
    
    // 角色输入事件 (角色对象, 按键, 按下时间)
    public event Action<GameObject, KeyCode, float> OnCharacterObjectKeyPressed;
    
    // 角色输入映射 (按键 -> 角色输入)
    private Dictionary<KeyCode, CharacterInput> keyToCharacterMap = new Dictionary<KeyCode, CharacterInput>();
    
    // 角色名称映射 (角色名称 -> 角色输入)
    private Dictionary<string, CharacterInput> nameToCharacterMap = new Dictionary<string, CharacterInput>();
    
    // 角色对象映射 (角色对象 -> 角色输入)
    private Dictionary<GameObject, CharacterInput> objectToCharacterMap = new Dictionary<GameObject, CharacterInput>();
    
    void Awake()
    {
        // 确保不会在游戏启动时自动保存配置
        saveToConfig = false;
    }
    
    /// <summary>
    /// 确保角色名称与游戏对象正确关联
    /// </summary>
    [Button("关联角色对象")]
    public void EnsureCharacterObjectLinks()
    {
        // 查找场景中所有游戏对象
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        
        int linkedCount = 0;
        
        // 遍历所有角色输入配置
        foreach (var input in characterInputs)
        {
            // 如果已经有关联的对象，跳过
            if (input.character != null)
                continue;
                
            // 如果没有角色名称，跳过
            if (string.IsNullOrEmpty(input.characterName))
                continue;
                
            // 尝试查找匹配名称的游戏对象
            foreach (var obj in allObjects)
            {
                if (obj.name == input.characterName)
                {
                    input.character = obj;
                    linkedCount++;
                    
                    if (showDebugInfo)
                    {
                        // 调试信息已移除
                    }
                    break;
                }
            }
            
            // 如果还是没有找到，尝试部分匹配
            if (input.character == null)
            {
                foreach (var obj in allObjects)
                {
                    if (obj.name.Contains(input.characterName) || input.characterName.Contains(obj.name))
                    {
                        input.character = obj;
                        linkedCount++;
                        
                        if (showDebugInfo)
                        {
                            // 调试信息已移除
                        }
                        break;
                    }
                }
            }
        }
        
        // 重新初始化映射
        InitializeMappings();
        
        // 调试信息已移除
    }
    
    void Start()
    {
        // 确保角色名称与游戏对象正确关联
        EnsureCharacterObjectLinks();
        
        // 初始化映射
        InitializeMappings();
        
        if (showDebugInfo)
        {
            // 调试信息已移除
        }
    }
    
    void Update()
    {
        // 检测按键输入
        CheckKeyInput();
    }
    
    /// <summary>
    /// 从配置加载按键设置
    /// </summary>
    public void LoadFromConfig()
    {
        if (judgmentConfig == null)
        {
            return;
        }
        
        // 清空现有配置
        characterInputs.Clear();
        
        // 从配置加载
        foreach (var config in judgmentConfig.characterConfigs)
        {
            // 跳过空名称
            if (string.IsNullOrEmpty(config.characterName))
                continue;
                
            // 创建新的角色输入
            CharacterInput input = new CharacterInput
            {
                characterName = config.characterName,
                keyCode = config.keyCode
            };
            
            // 添加到列表
            characterInputs.Add(input);
            
            if (showDebugInfo)
            {
                // 调试信息已移除
            }
        }
        
        // 重新初始化映射
        InitializeMappings();
    }
    
    /// <summary>
    /// 保存按键设置到配置
    /// </summary>
    public void SaveToConfig()
    {
        if (judgmentConfig == null)
        {
            return;
        }
        
        // 清空现有配置
        judgmentConfig.characterConfigs.Clear();
        
        // 保存到配置
        foreach (var input in characterInputs)
        {
            // 跳过空名称
            if (string.IsNullOrEmpty(input.characterName))
                continue;
                
            // 创建新的角色配置
            CharacterJudgmentConfig.CharacterJudgmentData config = new CharacterJudgmentConfig.CharacterJudgmentData
            {
                characterName = input.characterName,
                keyCode = input.keyCode
            };
            
            // 添加到配置
            judgmentConfig.characterConfigs.Add(config);
            
            if (showDebugInfo)
            {
                // 调试信息已移除
            }
        }
        
#if UNITY_EDITOR
        // 在编辑器模式下标记资源为已修改并保存
        UnityEditor.EditorUtility.SetDirty(judgmentConfig);
        UnityEditor.AssetDatabase.SaveAssets();
        if (showDebugInfo)
        {
            // 调试信息已移除
        }
#endif
    }
    
    /// <summary>
    /// 初始化映射
    /// </summary>
    private void InitializeMappings()
    {
        // 清空映射
        keyToCharacterMap.Clear();
        nameToCharacterMap.Clear();
        objectToCharacterMap.Clear();
        
        // 如果从配置加载，则加载配置（只读取，不清空现有列表）
        if (loadFromConfig && judgmentConfig != null && characterInputs.Count == 0)
        {
            // 从配置加载
            foreach (var config in judgmentConfig.characterConfigs)
            {
                // 跳过空名称
                if (string.IsNullOrEmpty(config.characterName))
                    continue;
                    
                // 创建新的角色输入
                CharacterInput input = new CharacterInput
                {
                    characterName = config.characterName,
                    keyCode = config.keyCode
                };
                
                // 添加到列表
                characterInputs.Add(input);
                
                if (showDebugInfo)
                {
                    // 调试信息已移除
                }
            }
        }
        
        // 验证按键配置
        ValidateKeyConfig();
        
        // 初始化映射
        foreach (var input in characterInputs)
        {
            // 跳过None按键
            if (input.keyCode == KeyCode.None)
                continue;
                
            // 添加到按键映射
            keyToCharacterMap[input.keyCode] = input;
            
            // 添加到名称映射
            if (!string.IsNullOrEmpty(input.characterName))
            {
                nameToCharacterMap[input.characterName] = input;
            }
            
            // 添加到对象映射
            if (input.character != null)
            {
                objectToCharacterMap[input.character] = input;
            }
        }
        
        if (showDebugInfo)
        {
            // 调试信息已移除
        }
    }
    
    /// <summary>
    /// 验证按键配置
    /// </summary>
    private void ValidateKeyConfig()
    {
        // 检查是否有重复的按键或禁止的按键
        Dictionary<KeyCode, string> usedKeys = new Dictionary<KeyCode, string>();
        
        foreach (var input in characterInputs)
        {
            // 跳过None按键
            if (input.keyCode == KeyCode.None)
                continue;
                
            // 检查是否是禁止使用的按键
            if (IsForbiddenKey(input.keyCode))
            {
                if (showDebugInfo)
                {
                    // 调试信息已移除
                }
                input.keyCode = KeyCode.None;
                continue;
            }
            
            // 检查是否有重复的按键
            if (usedKeys.ContainsKey(input.keyCode))
            {
                if (showDebugInfo)
                {
                    // 调试信息已移除
                }
            }
            else
            {
                usedKeys[input.keyCode] = input.characterName;
            }
        }
    }
    
    /// <summary>
    /// 检查是否是禁止使用的按键
    /// </summary>
    private bool IsForbiddenKey(KeyCode key)
    {
        foreach (var forbiddenKey in forbiddenKeys)
        {
            if (key == forbiddenKey)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// 检测按键输入
    /// </summary>
    private void CheckKeyInput()
    {
        if (showDebugInfo)
        {
            // 调试信息已移除
        }
        
        foreach (var input in characterInputs)
        {
            // 跳过None按键
            if (input.keyCode == KeyCode.None)
                continue;
                
            // 检测按键按下
            if (Input.GetKeyDown(input.keyCode))
            {
                // 记录按下时间
                input.lastPressTime = Time.time;
                input.wasPressed = true;
                
                if (showDebugInfo)
                {
                    // 调试信息已移除
                }
                
                // 触发事件
                if (!string.IsNullOrEmpty(input.characterName))
                {
                    if (showDebugInfo)
                    {
                        // 调试信息已移除
                    }
                    OnCharacterKeyPressed?.Invoke(input.characterName, input.keyCode, input.lastPressTime);
                }
                
                if (input.character != null)
                {
                    if (showDebugInfo)
                    {
                        // 调试信息已移除
                    }
                    OnCharacterObjectKeyPressed?.Invoke(input.character, input.keyCode, input.lastPressTime);
                    
                    // 打印按键按下信息
                    Debug.Log($"[按键按下] 角色 {input.character.name} 按下按键 {input.keyCode}");
                }
                else if (showDebugInfo)
                {
                    // 调试信息已移除
                }
            }
            // 检测按键抬起
            else if (Input.GetKeyUp(input.keyCode))
            {
                input.wasPressed = false;
            }
        }
    }
    
    /// <summary>
    /// 获取角色的按键
    /// </summary>
    public KeyCode GetCharacterKey(string characterName)
    {
        if (nameToCharacterMap.TryGetValue(characterName, out CharacterInput input))
        {
            return input.keyCode;
        }
        return KeyCode.None;
    }
    
    /// <summary>
    /// 获取角色的按键
    /// </summary>
    public KeyCode GetCharacterKey(GameObject character)
    {
        if (objectToCharacterMap.TryGetValue(character, out CharacterInput input))
        {
            return input.keyCode;
        }
        return KeyCode.None;
    }
    
    /// <summary>
    /// 设置角色的按键
    /// </summary>
    public void SetCharacterKey(string characterName, KeyCode key)
    {
        // 检查是否是禁止使用的按键
        if (IsForbiddenKey(key))
        {
            if (showDebugInfo)
            {
                // 调试信息已移除
            }
            return;
        }
        
        // 查找角色输入
        CharacterInput input = null;
        
        // 先从映射中查找
        if (nameToCharacterMap.TryGetValue(characterName, out input))
        {
            // 更新按键
            input.keyCode = key;
        }
        else
        {
            // 从列表中查找
            foreach (var item in characterInputs)
            {
                if (item.characterName == characterName)
                {
                    input = item;
                    input.keyCode = key;
                    break;
                }
            }
            
            // 如果没有找到，创建新的
            if (input == null)
            {
                input = new CharacterInput
                {
                    characterName = characterName,
                    keyCode = key
                };
                characterInputs.Add(input);
            }
        }
        
        // 重新初始化映射
        InitializeMappings();
        
        if (showDebugInfo)
        {
            // 调试信息已移除
        }
    }
    
    /// <summary>
    /// 设置角色的按键
    /// </summary>
    public void SetCharacterKey(GameObject character, KeyCode key)
    {
        if (character == null)
            return;
            
        // 检查是否是禁止使用的按键
        if (IsForbiddenKey(key))
        {
            if (showDebugInfo)
            {
                // 调试信息已移除
            }
            return;
        }
        
        // 查找角色输入
        CharacterInput input = null;
        
        // 先从映射中查找
        if (objectToCharacterMap.TryGetValue(character, out input))
        {
            // 更新按键
            input.keyCode = key;
        }
        else
        {
            // 从列表中查找
            foreach (var item in characterInputs)
            {
                if (item.character == character)
                {
                    input = item;
                    input.keyCode = key;
                    break;
                }
            }
            
            // 如果没有找到，创建新的
            if (input == null)
            {
                input = new CharacterInput
                {
                    characterName = character.name,
                    character = character,
                    keyCode = key
                };
                characterInputs.Add(input);
            }
        }
        
        // 重新初始化映射
        InitializeMappings();
        
        if (showDebugInfo)
        {
            // 调试信息已移除
        }
    }
    
    /// <summary>
    /// 重置所有角色的按键
    /// </summary>
    [Button("重置所有按键")]
    public void ResetAllKeys()
    {
        foreach (var input in characterInputs)
        {
            input.keyCode = KeyCode.None;
        }
        
        // 重新初始化映射
        InitializeMappings();
        
        if (showDebugInfo)
        {
            // 调试信息已移除
        }
    }
    
    /// <summary>
    /// 显示当前按键配置
    /// </summary>
    [Button("显示当前按键配置")]
    public void ShowCurrentKeyConfig()
    {
        if (characterInputs == null || characterInputs.Count == 0)
        {
            // 调试信息已移除
            return;
        }
        
        // 调试信息已移除
        foreach (var input in characterInputs)
        {
            // 调试信息已移除
        }
    }
    
    /// <summary>
    /// 应用配置
    /// </summary>
    [Button("应用配置")]
    public void ApplyConfig()
    {
        // 验证按键配置
        ValidateKeyConfig();
        
        // 重新初始化映射
        InitializeMappings();
        
        // 如果需要保存到配置，且明确设置了saveToConfig为true
        if (loadFromConfig && judgmentConfig != null && saveToConfig)
        {
            SaveToConfig();
            
            if (showDebugInfo)
            {
                // 调试信息已移除
            }
        }
        
        if (showDebugInfo)
        {
            // 调试信息已移除
        }
    }
} 