using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

/// <summary>
/// 判定动画触发器，用于处理角色按键判定和显示判定图片
/// </summary>
[AddComponentMenu("音乐节拍/判定动画触发器")]
public class JudgmentAnimationTrigger : MonoBehaviour
{
    [TitleGroup("配置引用")]
    [LabelText("动画管理器")]
    [Tooltip("用于播放判定动画的管理器")]
    public JudgmentAnimationManager animationManager;
    
    [LabelText("节拍检测器")]
    [Tooltip("用于获取节拍信息的检测器")]
    public BeatDetector beatDetector;
    
    [LabelText("角色输入管理器")]
    [Tooltip("用于接收角色按键事件的管理器")]
    public CharacterInputManager inputManager;
    
    [LabelText("角色判定配置")]
    [Tooltip("角色判定图片配置")]
    public CharacterJudgmentConfig judgmentConfig;
    
    [TitleGroup("判定设置")]
    [LabelText("Perfect判定时间窗口(秒)")]
    [Tooltip("按键时间与节拍时间差在此范围内判定为Perfect")]
    [Range(0.01f, 0.2f)]
    public float perfectWindow = 0.05f;
    
    [LabelText("Good判定时间窗口(秒)")]
    [Tooltip("按键时间与节拍时间差在此范围内判定为Good")]
    [Range(0.05f, 0.3f)]
    public float goodWindow = 0.1f;
    
    [TitleGroup("判定图片设置")]
    [LabelText("显示判定图片")]
    [Tooltip("是否显示角色判定图片")]
    public bool showJudgmentImage = true;
    
    [TitleGroup("调试设置")]
    [LabelText("显示调试信息")]
    [Tooltip("是否在控制台显示调试信息")]
    public bool showDebug = true;  // 默认开启调试信息
    
    private float lastJudgmentTime = 0f;
    
    // 已显示判定图片的角色集合
    private HashSet<string> charactersWithJudgmentShown = new HashSet<string>();
    
    // 角色判定图片组件缓存
    private Dictionary<string, Image> characterJudgmentImages = new Dictionary<string, Image>();
    
    // 1. 增加字段
    public SimpleBeatManager beatManager;

    private void Start()
    {
        // 检查必要组件
        if (animationManager == null)
        {
            enabled = false;
            return;
        }
        
        if (beatDetector == null)
        {
            enabled = false;
            return;
        }
        
        if (inputManager == null)
        {
            enabled = false;
            return;
        }
        
        if (judgmentConfig == null)
        {
            enabled = false;
            return;
        }

        // 1. 增加字段
        if (beatManager == null)
        {
            enabled = false;
            return;
        }
        // 1. 事件订阅
        beatManager.OnCharacterJudged += OnCharacterJudged;
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        if (inputManager != null)
        {
            inputManager.OnCharacterKeyPressed -= OnCharacterKeyPressed;
        }
        // 4. OnDestroy中取消订阅
        if (beatManager != null)
        {
            beatManager.OnCharacterJudged -= OnCharacterJudged;
        }
    }
    
    /// <summary>
    /// 角色按键事件处理
    /// </summary>
    private void OnCharacterKeyPressed(string characterName, KeyCode keyCode, float pressTime)
    {
        ProcessJudgment(characterName);
    }
    
    /// <summary>
    /// 处理判定逻辑
    /// </summary>
    private void ProcessJudgment(string characterName)
    {
        float currentTime = Time.time;
        
        // 防止连续判定，至少间隔0.1秒
        if (currentTime - lastJudgmentTime < 0.1f)
        {
            return;
        }
        
        lastJudgmentTime = currentTime;
        
        // 获取当前时间与最近拍点的时间差
        float timeSinceLastBeat = currentTime - beatDetector.lastBeatTime;
        float timeToNextBeat = beatDetector.beatInterval - timeSinceLastBeat;
        
        // 取时间差的较小值作为判定依据
        float timeDifference = Mathf.Min(timeSinceLastBeat, timeToNextBeat);
        
        // 根据时间差判定等级
        if (timeDifference <= perfectWindow)
        {
            // Perfect判定
            animationManager.PlayPerfectAnimation();
            
            // 显示判定图片 - 移除了只显示一次的限制
            if (showJudgmentImage)
            {
                ShowJudgmentImage(characterName, JudgmentAnimationConfig.JudgmentLevel.Perfect, 0);
                // 记录已显示判定的角色
                if (!charactersWithJudgmentShown.Contains(characterName))
                {
                    charactersWithJudgmentShown.Add(characterName);
                }
            }
            
        }
        else if (timeDifference <= goodWindow)
        {
            // Good判定
            animationManager.PlayGoodAnimation();
            
            // 显示判定图片 - 移除了只显示一次的限制
            if (showJudgmentImage)
            {
                ShowJudgmentImage(characterName, JudgmentAnimationConfig.JudgmentLevel.Good, 0);
                // 记录已显示判定的角色
                if (!charactersWithJudgmentShown.Contains(characterName))
                {
                    charactersWithJudgmentShown.Add(characterName);
                }
            }
            
        }
        else
        {
            // Miss判定
            animationManager.PlayMissAnimation();
            
            // 显示判定图片 - 为Miss也添加判定图片显示
            if (showJudgmentImage)
            {
                ShowJudgmentImage(characterName, JudgmentAnimationConfig.JudgmentLevel.Miss, 0);
                // 记录已显示判定的角色
                if (!charactersWithJudgmentShown.Contains(characterName))
                {
                    charactersWithJudgmentShown.Add(characterName);
                }
            }
            
        }
    }
    
    /// <summary>
    /// 显示角色的判定图片
    /// </summary>
    private void ShowJudgmentImage(string characterName, JudgmentAnimationConfig.JudgmentLevel level, int perfectImageIndex)
    {
        if (!showJudgmentImage) return;
        
        // Miss不显示图片
        if (level == JudgmentAnimationConfig.JudgmentLevel.Miss)
        {
            return;
        }
        
        // 获取或查找判定图片组件
        Image judgmentImage = GetJudgmentImageForCharacter(characterName);
        if (judgmentImage == null) 
        {
            return;
        }
        
        // 获取判定图片数组
        Sprite[] sprites = judgmentConfig.GetPerfectImagesForCharacter(characterName);
        if (sprites == null || sprites.Length == 0)
        {
            return;
        }
        
        // 选择图片索引：Perfect用0，Good用1（如果有）
        int spriteIndex = Mathf.Clamp(perfectImageIndex, 0, sprites.Length - 1);
        judgmentImage.sprite = sprites[spriteIndex];
        judgmentImage.enabled = true;
        
        // 确保图片有正确的尺寸和颜色
        if (judgmentImage.rectTransform.sizeDelta.x < 10 || judgmentImage.rectTransform.sizeDelta.y < 10)
        {
            judgmentImage.rectTransform.sizeDelta = new Vector2(100, 100);
        }
        Color imageColor = judgmentImage.color;
        if (imageColor.a < 1.0f)
        {
            imageColor.a = 1.0f;
            judgmentImage.color = imageColor;
        }
        
    }
    
    /// <summary>
    /// 隐藏角色的判定图片
    /// </summary>
    private void HideJudgmentImage(string characterName)
    {
        if (characterJudgmentImages.TryGetValue(characterName, out Image judgmentImage))
        {
            if (judgmentImage != null)
            {
                judgmentImage.enabled = false;
                
            }
        }
    }
    
    /// <summary>
    /// 获取角色的判定图片组件
    /// </summary>
    private Image GetJudgmentImageForCharacter(string characterName)
    {
        // 如果已经缓存，直接返回
        if (characterJudgmentImages.TryGetValue(characterName, out Image cachedImage))
        {
            return cachedImage;
        }
        
        // 查找角色对象
        GameObject characterObj = GameObject.Find(characterName);
        if (characterObj == null)
        {
            return null;
        }
        
        // 获取角色的判定图片组件名称
        string judgmentImageName = "JudgmentImage"; // 默认名称
        foreach (var config in judgmentConfig.characterConfigs)
        {
            if (config.characterName == characterName)
            {
                judgmentImageName = config.judgmentImageName;
                break;
            }
        }
        
        // 查找判定图片组件
        Transform judgmentImageTransform = characterObj.transform.Find(judgmentImageName);
        if (judgmentImageTransform == null)
        {
            return null;
        }
        
        // 获取Image组件
        Image judgmentImage = judgmentImageTransform.GetComponent<Image>();
        if (judgmentImage == null)
        {
            return null;
        }
        
        // 缓存并返回
        characterJudgmentImages[characterName] = judgmentImage;
        return judgmentImage;
    }
    
    /// <summary>
    /// 重置判定状态，清除已显示判定图片的角色记录
    /// </summary>
    [Button("重置判定状态")]
    public void ResetJudgmentState()
    {
        charactersWithJudgmentShown.Clear();
        
        // 隐藏所有角色的判定图片
        foreach (var kvp in characterJudgmentImages)
        {
            if (kvp.Value != null)
            {
                kvp.Value.enabled = false;
            }
        }
        
    }

    // 2. 事件处理方法签名
    private void OnCharacterJudged(GameObject character, BeatJudgment judgment, int perfectImageIndex)
    {
        string characterName = character != null ? character.name : null;
        if (string.IsNullOrEmpty(characterName)) return;

        // 播放动画
        switch (judgment)
        {
            case BeatJudgment.Perfect:
                animationManager.PlayPerfectAnimation();
                break;
            case BeatJudgment.Good:
                animationManager.PlayGoodAnimation();
                break;
            case BeatJudgment.Miss:
                animationManager.PlayMissAnimation();
                break;
        }
        // 显示图片（只在Perfect/Good时）
        if (judgment == BeatJudgment.Perfect || judgment == BeatJudgment.Good)
        {
            ShowJudgmentImage(characterName, 
                judgment == BeatJudgment.Perfect 
                    ? JudgmentAnimationConfig.JudgmentLevel.Perfect 
                    : JudgmentAnimationConfig.JudgmentLevel.Good,
                perfectImageIndex);
        }
    }
} 