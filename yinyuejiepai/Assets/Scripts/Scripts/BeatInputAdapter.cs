using UnityEngine;
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

/// <summary>
/// 节拍输入适配器
/// 连接CharacterInputManager和SimpleBeatManager，处理按键输入与节拍判定
/// </summary>
public class BeatInputAdapter : MonoBehaviour
{
    [TitleGroup("组件引用")]
    [Tooltip("角色输入管理器")]
    [Required("必须设置角色输入管理器")]
    public CharacterInputManager inputManager;
    
    [TitleGroup("组件引用")]
    [Tooltip("节拍管理器")]
    [Required("必须设置节拍管理器")]
    public SimpleBeatManager beatManager;
    
    [TitleGroup("判定设置")]
    [Tooltip("是否启用节拍判定")]
    public bool enableBeatJudgment = true;
    
    [TitleGroup("调试设置")]
    [Tooltip("是否显示调试信息")]
    public bool showDebugInfo = true;
    
    // 角色判定映射 (角色对象 -> 动作列表)
    private Dictionary<GameObject, List<CharacterBeatAction>> characterActionsMap = new Dictionary<GameObject, List<CharacterBeatAction>>();
    
    // 角色名称映射 (角色名称 -> 角色对象)
    private Dictionary<string, GameObject> nameToObjectMap = new Dictionary<string, GameObject>();
    
    void Start()
    {
        // 确保有输入管理器
        if (inputManager == null)
        {
            inputManager = FindObjectOfType<CharacterInputManager>();
            if (inputManager == null)
            {
                enabled = false;
                return;
            }
        }
        
        // 确保有节拍管理器
        if (beatManager == null)
        {
            beatManager = FindObjectOfType<SimpleBeatManager>();
            if (beatManager == null)
            {
                enabled = false;
                return;
            }
        }
        
        // 初始化映射
        InitializeMappings();
        
        // 订阅输入事件
        inputManager.OnCharacterObjectKeyPressed += OnCharacterKeyPressed;
        
    }
    
    void OnDestroy()
    {
        // 取消订阅输入事件
        if (inputManager != null)
        {
            inputManager.OnCharacterObjectKeyPressed -= OnCharacterKeyPressed;
        }
    }
    
    /// <summary>
    /// 初始化映射
    /// </summary>
    private void InitializeMappings()
    {
        // 清空映射
        characterActionsMap.Clear();
        nameToObjectMap.Clear();
        
        // 初始化角色动作映射
        if (beatManager != null && beatManager.beatActions != null)
        {
            foreach (var action in beatManager.beatActions)
            {
                if (action.character == null)
                    continue;
                    
                // 添加到角色映射
                if (!characterActionsMap.ContainsKey(action.character))
                {
                    characterActionsMap[action.character] = new List<CharacterBeatAction>();
                }
                characterActionsMap[action.character].Add(action);
                
                // 添加到名称映射
                if (!string.IsNullOrEmpty(action.character.name))
                {
                    nameToObjectMap[action.character.name] = action.character;
                }
            }
        }
        
        if (showDebugInfo)
        {
            // 调试信息已移除
        }
    }
    
    /// <summary>
    /// 处理角色按键按下事件
    /// </summary>
    private void OnCharacterKeyPressed(GameObject character, KeyCode key, float pressTime)
    {
        if (!enableBeatJudgment || character == null)
            return;
            
        // 查找角色的动作
        if (characterActionsMap.TryGetValue(character, out List<CharacterBeatAction> actions))
        {
            // 查找当前可以触发的动作
            foreach (var action in actions)
            {
                            // 调试输出每个动作的状态
            // 调试信息已移除
                
                if (action.requiresClick)
                {
                    // 判断是否在正确的时机
                    // 获取当前拍子信息
                    int currentGroup = beatManager.beatDetector.currentGroupNumber;
                    int currentBeat = beatManager.beatDetector.currentBeatInGroup;
                    
                    // 检查是否匹配当前拍点或在判定范围内
                    bool isInCorrectGroup = action.groupNumber == currentGroup;
                    bool isInCorrectBeat = action.beatNumber == currentBeat;
                    
                    // 调试信息已移除
                    
                    // 如果在正确的拍子组和拍子位置，且未执行过
                    if (isInCorrectGroup && isInCorrectBeat && !action.hasExecuted)
                    {
                        // 尝试触发判定
                        beatManager.TryJudge(action);
                        
                        // 打印按键触发信息
                        Debug.Log($"[按键触发] 角色 {character.name} 在正确时机按下按键 {key}，触发动作 {action.actionName}");
                        
                        break;
                    }
                }
            }
            
        }
        else
        {
            // 打印未找到动作映射的警告
            Debug.LogWarning($"[按键警告] 角色 {character.name} 按下按键 {key}，但未找到对应的动作映射");
        }
    }
    
    /// <summary>
    /// 刷新映射
    /// </summary>
    [Button("刷新映射")]
    public void RefreshMappings()
    {
        InitializeMappings();
    }
    
    /// <summary>
    /// 显示当前映射
    /// </summary>
    [Button("显示当前映射")]
    public void ShowCurrentMappings()
    {
        if (characterActionsMap == null || characterActionsMap.Count == 0)
        {
            // 调试信息已移除
            return;
        }
        
        // 调试信息已移除
        foreach (var pair in characterActionsMap)
        {
            // 调试信息已移除
            foreach (var action in pair.Value)
            {
                // Debug.Log($"    - 动作: {action.actionName}, 需要判定: {action.requiresClick}, 组号: {action.groupNumber}, 拍号: {action.beatNumber}");
            }
        }
    }
} 