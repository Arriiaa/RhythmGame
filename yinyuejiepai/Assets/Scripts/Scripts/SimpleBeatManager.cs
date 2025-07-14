using UnityEngine;
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.UI;

// 判定等级枚举
public enum BeatJudgment
{
    Perfect,
    Good,
    Miss,
    None
}

// 角色判定图片控制器
[Serializable]
public class CharacterJudgmentImages
{
    [LabelText("角色")]
    [Tooltip("角色对象")]
    public GameObject character;
    
    [LabelText("判定图片显示组件")]
    [Tooltip("用于显示判定图片的Image组件")]
    public Image judgmentImage;
    
    [LabelText("判定图片数组")]
    [Tooltip("所有判定都用此图片数组")]
    public Sprite[] perfectImages;
    
    [HideInInspector]
    public int currentImageIndex = -1;
    
    [HideInInspector]
    public float imageTimer = 0f;
}

// 简单的角色行为定义
[Serializable]
public class CharacterBeatAction
{
    [LabelText("行为名称")]
    [Tooltip("行为的名称标识")]
    public string actionName;
    
    [LabelText("目标角色")]
    [Tooltip("要执行行为的目标角色")]
    public GameObject character;
    
    [LabelText("八拍组号")]
    [Tooltip("目标八拍组号(从1开始)")]
    public int groupNumber = 1;
    
    [LabelText("拍号(1-8)")]
    [Tooltip("目标拍号(1-8)")]
    [Range(1, 8)]
    public int beatNumber = 1;
    
    [LabelText("音效")]
    [Tooltip("要播放的音效")]
    public AudioClip audioClip;
    
    [LabelText("动画名称")]
    [Tooltip("要播放的动画名称")]
    public string animationName;
    
    [LabelText("需要判定")]
    [Tooltip("是否需要根据拍子判定来触发")]
    public bool requiresClick = false;
    
    [LabelText("Perfect图片索引")]
    [Tooltip("Perfect判定时显示的图片索引")]
    [ShowIf("requiresClick")]
    [Range(0, 99)]
    public int perfectImageIndex = 0;
    
    [LabelText("关闭判定图片")]
    [Tooltip("执行此行为时是否关闭角色的判定图片")]
    public bool closeJudgmentImage = false;
    
    [HideInInspector]
    public KeyCode fallbackKeyCode = KeyCode.None;
    
    [HideInInspector]
    public bool hasExecuted = false;
    
    [HideInInspector]
    public bool isInCorrectTiming = false;
    
    [HideInInspector]
    public float targetBeatTime = 0f;
    
    [HideInInspector]
    public BeatJudgment lastJudgment = BeatJudgment.None;
}

public class SimpleBeatManager : MonoBehaviour
{
    // 1. 在类内添加事件
    public event Action<GameObject, BeatJudgment, int> OnCharacterJudged;
    
    // 游戏结束事件
    public event Action<int, int, int> OnGameEnding; // Perfect, Good, Miss
    public event Action<int, int, int> OnGameEnded;  // Perfect, Good, Miss

    [TitleGroup("基础设置")]
    [LabelText("节拍检测器")]
    [Tooltip("节拍检测器组件引用")]
    public BeatDetector beatDetector;
    
    [TitleGroup("角色行为配置")]
    [LabelText("角色行为列表")]
    [Tooltip("配置在特定拍子执行的角色行为")]
    [TableList]
    public List<CharacterBeatAction> beatActions = new List<CharacterBeatAction>();
    
    [TitleGroup("角色判定图片")]
    [LabelText("角色判定配置")]
    [Tooltip("角色判定图片配置ScriptableObject")]
    public CharacterJudgmentConfig judgmentConfig;
    
    [HideInInspector]
    public List<CharacterJudgmentImages> characterJudgmentImages = new List<CharacterJudgmentImages>();
    
    [TitleGroup("判定设置")]
    [LabelText("Perfect判定范围(拍)")]
    [Tooltip("点击时间与目标时间相差±0.1拍内为Perfect")]
    [Range(0.05f, 1.0f)]
    public float perfectRange = 0.1f;
    
    [LabelText("Good判定范围(拍)")]
    [Tooltip("点击时间与目标时间相差±0.3拍内为Good")]
    [Range(0.1f, 1.0f)]
    public float goodRange = 0.3f;
    
    [LabelText("Miss判定范围(拍)")]
    [Tooltip("点击时间与目标时间相差±0.5拍内为Miss")]
    [Range(0.3f, 1.5f)]
    public float missRange = 0.5f;
    
    [LabelText("Perfect图片显示时间")]
    [Tooltip("Perfect图片显示的持续时间(秒)")]
    [Range(0.1f, 2.0f)]
    public float perfectImageDuration = 0.5f;
    
    [TitleGroup("调试设置")]
    [LabelText("显示调试信息")]
    [Tooltip("是否显示调试信息")]
    public bool showDebugInfo = true;
    
    [TitleGroup("游戏结束设置")]
    [LabelText("所有行为完成后结束游戏")]
    [Tooltip("当所有角色行为都执行完毕后是否结束游戏")]
    public bool endGameWhenAllActionsComplete = true;
    
    [LabelText("游戏结束延迟时间")]
    [Tooltip("所有行为完成后延迟多少秒结束游戏")]
    [Range(0f, 5f)]
    public float endGameDelay = 2f;
    
    // 记录上一次处理的拍子
    private int lastProcessedBeat = 0;
    private int lastProcessedGroup = 0;
    
    // 缓存组件
    private Dictionary<GameObject, AudioSource> characterAudioSources = new Dictionary<GameObject, AudioSource>();
    private Dictionary<GameObject, SpriteAnimationController> characterAnimators = new Dictionary<GameObject, SpriteAnimationController>();
    
    // 判定统计
    [HideInInspector]
    public int perfectCount = 0;
    [HideInInspector]
    public int goodCount = 0;
    [HideInInspector]
    public int missCount = 0;
    
    // 游戏结束相关
    [HideInInspector]
    public bool isGameEnding = false;
    [HideInInspector]
    public bool isGameEnded = false;
    private float endGameTimer = 0f;
    private int totalActions = 0;
    private int completedActions = 0;
    
    // 显示所有节拍动作的详细信息
    [Button("显示所有节拍动作")]
    public void ShowAllBeatActions()
    {
        // 调试功能已移除
    }
    
    void Start()
    {
        // 确保有BeatDetector引用
        if (beatDetector == null)
        {
            beatDetector = FindObjectOfType<BeatDetector>();
            if (beatDetector == null)
            {
                enabled = false;
                return;
            }
        }
        
        // 从ScriptableObject初始化角色判定配置
        InitializeCharacterJudgmentFromConfig();
        
        // 预缓存角色组件
        foreach (var action in beatActions)
        {
            if (action.character != null)
            {
                CacheCharacterComponents(action.character);
            }
        }
        
        // 确保角色按键配置已加载
        if (judgmentConfig != null && showDebugInfo)
        {
            // 调试信息已移除
        }
        
        // 强制重置所有按键为None - 这行导致了配置被重置
        // ResetAllKeysToNone();
        
        // 重置所有行为，并确保所有角色的备用按键都被设置为None
        ResetBeatActions();
        
        // 显示所有节拍动作的详细信息
        if (showDebugInfo)
        {
            ShowAllBeatActions();
        }
        
        // 初始化游戏结束相关变量
        InitializeGameEndTracking();
    }
    
    // 初始化游戏结束跟踪
    void InitializeGameEndTracking()
    {
        totalActions = beatActions.Count;
        completedActions = 0;
        isGameEnding = false;
        isGameEnded = false;
        endGameTimer = 0f;
        
        Debug.Log($"[游戏结束] 初始化完成，总行为数: {totalActions}");
    }
    
    // 从ScriptableObject初始化角色判定配置
    void InitializeCharacterJudgmentFromConfig()
    {
        if (judgmentConfig == null)
        {
            return;
        }
        
        // 清空现有配置
        characterJudgmentImages.Clear();
        
        // 为每个行为中的角色创建判定图片配置
        foreach (var action in beatActions)
        {
            if (action.character != null && action.requiresClick)
            {
                string characterName = action.character.name;
                
                // 获取角色配置
                CharacterJudgmentConfig.CharacterJudgmentData characterData = null;
                foreach (var config in judgmentConfig.characterConfigs)
                {
                    if (config.characterName == characterName)
                    {
                        characterData = config;
                        break;
                    }
                }
                
                if (characterData == null)
                {
                    continue;
                }
                
                Sprite[] perfectImages = characterData.perfectImages;
                
                if (perfectImages != null && perfectImages.Length > 0)
                {
                    // 检查是否已存在该角色的配置
                    bool exists = false;
                    foreach (var existing in characterJudgmentImages)
                    {
                        if (existing.character == action.character)
                        {
                            exists = true;
                            break;
                        }
                    }
                    
                    // 如果不存在，添加新配置
                    if (!exists)
                    {
                        // 尝试查找指定名称的判定图片显示组件
                        Image judgmentImage = null;
                        
                        // 如果指定了判定图片组件名称，则按名称查找
                        if (!string.IsNullOrEmpty(characterData.judgmentImageName))
                        {
                            Transform imageTransform = action.character.transform.Find(characterData.judgmentImageName);
                            if (imageTransform != null)
                            {
                                judgmentImage = imageTransform.GetComponent<Image>();
                            }
                        }
                        
                        // 如果按名称没找到，则尝试查找任意Image组件
                        if (judgmentImage == null)
                        {
                            judgmentImage = action.character.GetComponentInChildren<Image>();
                        }
                        
                        var newCharacterImages = new CharacterJudgmentImages
                        {
                            character = action.character,
                            judgmentImage = judgmentImage,
                            perfectImages = perfectImages
                        };
                        
                        characterJudgmentImages.Add(newCharacterImages);
                        
                        // 初始化时隐藏判定图片
                        if (judgmentImage != null)
                        {
                            judgmentImage.gameObject.SetActive(false);
                        }
                        
                        // 如果没有找到判定图片显示组件，输出警告
                        if (judgmentImage == null && showDebugInfo)
                        {
                            // 调试信息已移除
                        }
                        
                        if (showDebugInfo)
                        {
                            // 调试信息已移除
                        }
                    }
                }
            }
        }
    }
    
    // 初始化角色按键映射
    void InitializeCharacterKeyMap()
    {
        // This method is no longer needed as keyCode is directly used.
        // Keeping it for now in case it's called elsewhere or for future use.
    }
    
    // 自定义的特殊KeyCode，用于表示"无按键"
    private const KeyCode NO_KEY = KeyCode.F15; // 使用一个不常用的按键代替None
    
    // 获取角色的实际按键
    KeyCode GetActualKeyCode(CharacterBeatAction action)
    {
        if (action.character == null) 
        {
            if (showDebugInfo)
            {
                // 调试信息已移除
            }
            
            // 如果备用按键是None或Space，返回NO_KEY
            if (action.fallbackKeyCode == KeyCode.None || action.fallbackKeyCode == KeyCode.Space)
            {
                return NO_KEY;
            }
            
            return action.fallbackKeyCode;
        }
        
        // 始终尝试从角色配置中获取按键
        if (judgmentConfig != null)
        {
            string characterName = action.character.name;
            
            // 添加调试信息
            if (showDebugInfo)
            {
                // 调试信息已移除
            }
            
            KeyCode keyCode = judgmentConfig.GetKeyCodeForCharacter(characterName);
            
            // 添加调试信息
            if (showDebugInfo)
            {
                // 调试信息已移除
            }
            
            // 如果配置返回了None或Space，使用NO_KEY
            if (keyCode == KeyCode.None || keyCode == KeyCode.Space)
            {
                if (showDebugInfo)
                {
                    // 调试信息已移除
                }
                
                // 如果备用按键也是None或Space，返回NO_KEY
                if (action.fallbackKeyCode == KeyCode.None || action.fallbackKeyCode == KeyCode.Space)
                {
                    return NO_KEY;
                }
                
                return action.fallbackKeyCode;
            }
            
            return keyCode;
        }
        
        // 如果无法获取角色配置的按键，使用备用按键
        if (action.fallbackKeyCode == KeyCode.None || action.fallbackKeyCode == KeyCode.Space)
        {
            if (showDebugInfo)
            {
                // 调试信息已移除
            }
            return NO_KEY;
        }
        
        return action.fallbackKeyCode;
    }
    
    void CacheCharacterComponents(GameObject character)
    {
        // 缓存AudioSource
        if (!characterAudioSources.ContainsKey(character))
        {
            AudioSource audioSource = character.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = character.AddComponent<AudioSource>();
                if (showDebugInfo)
                {
                    // 调试信息已移除
                }
            }
            characterAudioSources[character] = audioSource;
        }
        
        // 缓存SpriteAnimationController
        if (!characterAnimators.ContainsKey(character))
        {
            SpriteAnimationController animator = character.GetComponent<SpriteAnimationController>();
            if (animator != null)
            {
                characterAnimators[character] = animator;
                if (showDebugInfo)
                {
                    // 调试信息已移除
                }
            }
            else
            {
                // 调试信息已移除
            }
        }
    }
    
    void Update()
    {
        if (beatDetector == null) return;
        
        int currentGroup = beatDetector.currentGroupNumber;
        int currentBeat = beatDetector.currentBeatInGroup;
        
        // 检查是否有新的拍子
        if (currentGroup != lastProcessedGroup || currentBeat != lastProcessedBeat)
        {
            if (showDebugInfo)
            {
                // 调试信息已移除
            }
            
            // 处理当前拍点的所有行为
            ProcessBeatActions(currentGroup, currentBeat);
            
            // 更新已处理的拍点
            lastProcessedGroup = currentGroup;
            lastProcessedBeat = currentBeat;
        }
        
        // 检查需要点击的行为
        CheckClickActions();
        
        // 更新判定图片显示
        UpdateJudgmentImages();
        
        // 检查游戏结束条件
        CheckGameEndCondition();
    }
    
    void ProcessBeatActions(int groupNumber, int beatNumber)
    {
        foreach (var action in beatActions)
        {
            // 检查是否匹配当前拍点
            if (action.groupNumber == groupNumber && action.beatNumber == beatNumber)
            {
                // 设置目标拍子时间
                action.targetBeatTime = Time.time;
                
                if (action.requiresClick)
                {
                    // 需要点击的行为，标记为在正确时机
                    action.isInCorrectTiming = true;
                    action.hasExecuted = false;
                    
                    if (showDebugInfo)
                    {
                        // 调试信息已移除
                    }
                }
                else
                {
                    // 自动触发的行为
                    if (!action.hasExecuted)
                    {
                        ExecuteAction(action);
                        action.hasExecuted = true;
                        
                        if (showDebugInfo)
                        {
                            // 调试信息已移除
                        }
                    }
                }
            }
        }
    }
    
    void CheckClickActions()
    {
        // 该方法现在仅用于显示调试信息，不再直接处理按键输入
        // 按键输入由CharacterInputManager处理，通过BeatInputAdapter与节拍系统集成
        
        foreach (var action in beatActions)
        {
            if (action.character != null && showDebugInfo)
            {
                // 调试信息已移除
            }
        }
    }
    
    // 检查游戏结束条件
    void CheckGameEndCondition()
    {
        if (isGameEnded || !endGameWhenAllActionsComplete) return;
        
        // 如果游戏正在结束中，处理延迟
        if (isGameEnding)
        {
            endGameTimer -= Time.deltaTime;
            if (endGameTimer <= 0f)
            {
                EndGame();
            }
            return;
        }
        
        // 检查是否所有行为都已完成
        int completedCount = 0;
        foreach (var action in beatActions)
        {
            if (action.hasExecuted)
            {
                completedCount++;
            }
        }
        
        // 如果所有行为都已完成，开始游戏结束流程
        if (completedCount >= totalActions && totalActions > 0)
        {
            StartGameEndSequence();
        }
    }
    
    BeatJudgment CalculateJudgment(CharacterBeatAction action)
    {
        float clickTime = Time.time;
        float timeDiff = Mathf.Abs(clickTime - action.targetBeatTime);
        
        // 将时间差转换为拍数（基于当前BPM）
        float beatInterval = 60f / beatDetector.currentBPM;
        float beatDiff = timeDiff / beatInterval;
        
        BeatJudgment judgment;
        if (beatDiff <= perfectRange)
        {
            judgment = BeatJudgment.Perfect;
        }
        else if (beatDiff <= goodRange)
        {
            judgment = BeatJudgment.Good;
        }
        else if (beatDiff <= missRange)
        {
            judgment = BeatJudgment.Miss;
        }
        else
        {
            judgment = BeatJudgment.Miss; // 超出Miss范围也判定为Miss
        }
        
        // 打印判定计算详情
        Debug.Log($"[判定计算] 时间差: {timeDiff:F3}s, 拍差: {beatDiff:F3}拍, 判定: {judgment}");
        
        return judgment;
    }
    
    void UpdateJudgmentStats(BeatJudgment judgment)
    {
        switch (judgment)
        {
            case BeatJudgment.Perfect:
                perfectCount++;
                Debug.Log($"[判定统计] Perfect +1, 当前统计: Perfect={perfectCount}, Good={goodCount}, Miss={missCount}");
                break;
            case BeatJudgment.Good:
                goodCount++;
                Debug.Log($"[判定统计] Good +1, 当前统计: Perfect={perfectCount}, Good={goodCount}, Miss={missCount}");
                break;
            case BeatJudgment.Miss:
                missCount++;
                Debug.Log($"[判定统计] Miss +1, 当前统计: Perfect={perfectCount}, Good={goodCount}, Miss={missCount}");
                // Miss判定也会显示图片效果
                break;
        }
    }
    
    void ExecuteAction(CharacterBeatAction action)
    {
        if (action.character == null) return;
        
        // 播放音效
        if (action.audioClip != null && characterAudioSources.TryGetValue(action.character, out AudioSource audioSource))
        {
            audioSource.PlayOneShot(action.audioClip);
            if (showDebugInfo)
            {
                // 调试信息已移除
            }
        }
        
        // 播放动画
        if (!string.IsNullOrEmpty(action.animationName))
        {
            if (characterAnimators.TryGetValue(action.character, out SpriteAnimationController animator))
            {
                // 使用SpriteAnimationController播放动画
                animator.SetCurrentSequence(action.animationName, true);
                if (showDebugInfo)
                {
                    // 调试信息已移除
                }
            }
            else
            {
                // 调试信息已移除
            }
        }
        else if (showDebugInfo)
        {
            // 调试信息已移除
        }
        
        // 检查是否需要关闭判定图片
        if (action.closeJudgmentImage)
        {
            CloseCharacterJudgmentImage(action.character);
        }
    }
    
    /// <summary>
    /// 角色自定义输入时调用，尝试进行拍子判定
    /// </summary>
    public void TryJudge(CharacterBeatAction action)
    {
        if (action == null) return;
        
        // 调试信息已移除
        
        // 检查是否在正确的拍子组和拍子位置
        int currentGroup = beatDetector.currentGroupNumber;
        int currentBeat = beatDetector.currentBeatInGroup;
        bool isInCorrectGroup = action.groupNumber == currentGroup;
        bool isInCorrectBeat = action.beatNumber == currentBeat;
        
        // 调试信息已移除
        
        // 如果在正确的拍子组和拍子位置，或者已经标记为正确时机
        if ((isInCorrectGroup && isInCorrectBeat) || action.isInCorrectTiming)
        {
            if (!action.hasExecuted)
            {
                BeatJudgment judgment = CalculateJudgment(action);
                action.lastJudgment = judgment;
                UpdateJudgmentStats(judgment);
                ShowJudgmentEffect(judgment, action.character, action.perfectImageIndex);
                ExecuteAction(action);
                action.hasExecuted = true;
                action.isInCorrectTiming = false;

                // 新增：判定后触发事件，带上perfectImageIndex
                OnCharacterJudged?.Invoke(action.character, judgment, action.perfectImageIndex);

                // 打印判定结果
                string characterName = action.character != null ? action.character.name : "未知角色";
                Debug.Log($"[判定结果] 角色: {characterName}, 动作: {action.actionName}, 判定: {judgment}");
                return;
            }
            else
            {
                // 调试信息已移除
            }
        }
        else
        {
            // 调试信息已移除
        }
    }
    
    // 重置所有行为的执行状态
    [Button("重置所有行为")]
    public void ResetBeatActions()
    {
        foreach (var action in beatActions)
        {
            action.hasExecuted = false;
            action.isInCorrectTiming = false;
            action.lastJudgment = BeatJudgment.None;
            
            // 重置所有角色的备用按键为None
            action.fallbackKeyCode = KeyCode.None;
        }
        
        // 重置处理记录
        lastProcessedBeat = 0;
        lastProcessedGroup = 0;
        
        // 重置游戏结束状态
        InitializeGameEndTracking();
        
        if (showDebugInfo)
        {
            // 调试信息已移除
        }
    }
    
    // 重置判定统计
    [Button("重置判定统计")]
    public void ResetJudgmentStats()
    {
        perfectCount = 0;
        goodCount = 0;
        missCount = 0;
    }
    
    // 重新加载角色判定配置
    [Button("重新加载判定配置")]
    public void ReloadJudgmentConfig()
    {
        if (judgmentConfig != null)
        {
            InitializeCharacterJudgmentFromConfig();
            // 调试信息已移除
        }
        else
        {
            // 调试信息已移除
        }
    }
    
    [Button("重置所有按键为None")]
    public void ResetAllKeysToNone()
    {
        // 重置所有角色的备用按键
        foreach (var action in beatActions)
        {
            action.fallbackKeyCode = KeyCode.None;
        }
        
        // 如果有判定配置，也重置配置中的按键
        if (judgmentConfig != null)
        {
            foreach (var config in judgmentConfig.characterConfigs)
            {
                config.keyCode = KeyCode.None;
            }
        }
        
        // 调试信息已移除
    }
    
    // 获取判定统计信息
    public string GetJudgmentStats()
    {
        int total = perfectCount + goodCount + missCount;
        if (total == 0) return "暂无判定数据";
        
        return $"Perfect: {perfectCount}, Good: {goodCount}, Miss: {missCount}, 总计: {total}";
    }
    
    // 添加新的角色行为
    public void AddBeatAction(GameObject character, int groupNumber, int beatNumber, 
                             AudioClip audioClip = null, string animationName = null, bool requiresClick = false, int perfectImageIndex = 0, KeyCode fallbackKeyCode = KeyCode.None, bool closeJudgmentImage = false)
    {
        if (character == null) return;
        
        // 获取角色实际配置的按键
        KeyCode actualKeyCode = fallbackKeyCode;
        if (judgmentConfig != null)
        {
            actualKeyCode = judgmentConfig.GetKeyCodeForCharacter(character.name);
        }
        
        var newAction = new CharacterBeatAction
        {
            actionName = $"{character.name}_Action_{beatActions.Count}",
            character = character,
            groupNumber = groupNumber,
            beatNumber = Mathf.Clamp(beatNumber, 1, 8),
            audioClip = audioClip,
            animationName = animationName,
            requiresClick = requiresClick,
            perfectImageIndex = perfectImageIndex,
            fallbackKeyCode = fallbackKeyCode,
            closeJudgmentImage = closeJudgmentImage
        };
        
        beatActions.Add(newAction);
        
        // 确保角色有必要的组件
        CacheCharacterComponents(character);
    }
    
    // 添加角色判定图片配置
    public void AddCharacterJudgmentImages(GameObject character, Sprite[] perfectImages, Image judgmentImage = null)
    {
        if (character == null) return;
        
        // 如果没有指定判定图片显示组件，尝试在角色对象下查找
        if (judgmentImage == null)
        {
            judgmentImage = character.GetComponentInChildren<Image>();
            if (judgmentImage == null && showDebugInfo)
            {
                // 调试信息已移除
            }
        }
        
        // 检查是否已存在该角色的配置
        foreach (var characterImages in characterJudgmentImages)
        {
            if (characterImages.character == character)
            {
                characterImages.perfectImages = perfectImages;
                characterImages.judgmentImage = judgmentImage;
                return;
            }
        }
        
        // 添加新配置
        var newCharacterImages = new CharacterJudgmentImages
        {
            character = character,
            judgmentImage = judgmentImage,
            perfectImages = perfectImages
        };
        
        characterJudgmentImages.Add(newCharacterImages);
        
        // 初始化时隐藏判定图片
        if (judgmentImage != null)
        {
            judgmentImage.gameObject.SetActive(false);
        }
    }
    
    // 移除特定角色的所有行为
    public void RemoveCharacterActions(GameObject character)
    {
        if (character == null) return;
        beatActions.RemoveAll(action => action.character == character);
    }
    
    // 更新判定图片显示
    void UpdateJudgmentImages()
    {
        foreach (var characterImages in characterJudgmentImages)
        {
            if (characterImages.perfectImages != null && 
                characterImages.currentImageIndex >= 0 && 
                characterImages.currentImageIndex < characterImages.perfectImages.Length)
            {
                if (characterImages.imageTimer > 0)
                {
                    characterImages.imageTimer -= Time.deltaTime;
                    if (characterImages.imageTimer <= 0)
                    {
                        // 重置图片索引
                        characterImages.currentImageIndex = -1;
                        
                        // 图片显示后保持显示状态，不隐藏
                        // 注释掉隐藏逻辑，让图片一直显示
                        // if (characterImages.judgmentImage != null)
                        // {
                        //     characterImages.judgmentImage.gameObject.SetActive(false);
                        // }
                    }
                }
            }
        }
    }
    
    // 获取角色的判定图片配置
    CharacterJudgmentImages GetCharacterJudgmentImages(GameObject character)
    {
        if (character == null) return null;
        
        foreach (var characterImages in characterJudgmentImages)
        {
            if (characterImages.character == character)
            {
                return characterImages;
            }
        }
        
        return null;
    }
    
    // 显示判定效果
    void ShowJudgmentEffect(BeatJudgment judgment, GameObject character, int imageIndex)
    {
        // 调试信息已移除
        if (character == null) return;
        
        // Perfect和Miss判定都显示图片
        if (judgment != BeatJudgment.Perfect && judgment != BeatJudgment.Miss)
        {
            // 调试信息已移除
            return;
        }
        
        var characterImages = GetCharacterJudgmentImages(character);
        if (characterImages == null || characterImages.perfectImages == null || characterImages.perfectImages.Length == 0)
        {
            // 调试信息已移除
            return;
        }
        
        // 打印判定效果显示信息
        string judgmentType = judgment == BeatJudgment.Perfect ? "Perfect" : "Miss";
        Debug.Log($"[判定效果] 角色 {character.name} 显示{judgmentType}判定图片，索引: {imageIndex}");
        imageIndex = Mathf.Clamp(imageIndex, 0, characterImages.perfectImages.Length - 1);
        if (characterImages.perfectImages[imageIndex] != null)
        {
            characterImages.currentImageIndex = imageIndex;
            characterImages.imageTimer = perfectImageDuration;
            if (characterImages.judgmentImage != null)
            {
                // 显示判定图片组件
                characterImages.judgmentImage.gameObject.SetActive(true);
                characterImages.judgmentImage.sprite = characterImages.perfectImages[imageIndex];
                // 调试信息已移除
                if (showDebugInfo)
                {
                    // 调试信息已移除
                }
            }
            else if (showDebugInfo)
            {
                // 调试信息已移除
            }
        }
    }
    
    // 开始游戏结束序列
    void StartGameEndSequence()
    {
        if (isGameEnding || isGameEnded) return;
        
        isGameEnding = true;
        endGameTimer = endGameDelay;
        
        Debug.Log($"[游戏结束] 所有行为已完成，{endGameDelay}秒后结束游戏");
        Debug.Log($"[游戏结束] 最终统计: Perfect={perfectCount}, Good={goodCount}, Miss={missCount}");
        
        // 这里可以触发游戏结束事件，比如显示结算界面等
        OnGameEnding?.Invoke(perfectCount, goodCount, missCount);
    }
    
    // 结束游戏
    void EndGame()
    {
        if (isGameEnded) return;
        
        isGameEnded = true;
        Debug.Log("[游戏结束] 游戏已结束");
        
        // 这里可以执行具体的游戏结束逻辑
        // 比如返回主菜单、显示结算界面等
        OnGameEnded?.Invoke(perfectCount, goodCount, missCount);
        
        // 示例：停止音频播放
        if (beatDetector != null && beatDetector.audioSource != null)
        {
            beatDetector.audioSource.Stop();
        }
    }
    
    // 关闭角色的判定图片
    public void CloseCharacterJudgmentImage(GameObject character)
    {
        if (character == null) return;
        
        var characterImages = GetCharacterJudgmentImages(character);
        if (characterImages != null && characterImages.judgmentImage != null)
        {
            characterImages.judgmentImage.gameObject.SetActive(false);
            characterImages.currentImageIndex = -1;
            characterImages.imageTimer = 0f;
            
            Debug.Log($"[关闭判定图片] 角色 {character.name} 的判定图片已关闭");
        }
    }
    
    // 获取角色的按键配置
    public KeyCode GetCharacterKeyCode(GameObject character)
    {
        if (character == null) return KeyCode.None;
        
        string characterName = character.name;
        
        if (judgmentConfig != null)
        {
            return judgmentConfig.GetKeyCodeForCharacter(characterName);
        }
        
        // 默认返回None
        return KeyCode.None;
    }
} 