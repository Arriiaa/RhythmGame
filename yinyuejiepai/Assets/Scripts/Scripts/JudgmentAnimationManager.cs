using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class JudgmentAnimationManager : MonoBehaviour
{
    [TitleGroup("配置")]
    [LabelText("判定动画配置")]
    [Required("请设置判定动画配置")]
    [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    public JudgmentAnimationConfig animationConfig;
    
    [TitleGroup("调试")]
    [LabelText("显示调试信息")]
    public bool showDebug = true;
    
    // 缓存找到的动画控制器
    private Dictionary<string, SpriteAnimationController> animationControllers = new Dictionary<string, SpriteAnimationController>();
    
    private void Start()
    {
        if (animationConfig == null)
        {
            enabled = false;
            return;
        }
        
        // 预缓存所有可能的动画控制器
        CacheAllAnimationControllers();
    }
    
    /// <summary>
    /// 预缓存所有可能的动画控制器
    /// </summary>
    private void CacheAllAnimationControllers()
    {
        animationControllers.Clear();
        
        // 获取场景中所有的SpriteAnimationController
        SpriteAnimationController[] controllers = FindObjectsOfType<SpriteAnimationController>();
        
        foreach (var controller in controllers)
        {
            if (controller != null && controller.gameObject != null)
            {
                string targetName = controller.gameObject.name;
                if (!animationControllers.ContainsKey(targetName))
                {
                    animationControllers.Add(targetName, controller);
                    
                    if (showDebug)
                    {
                        // 调试信息已移除
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 根据判定等级播放相应的动画
    /// </summary>
    /// <param name="level">判定等级</param>
    public void PlayJudgmentAnimation(JudgmentAnimationConfig.JudgmentLevel level)
    {
        if (animationConfig == null) return;
        
        var targets = animationConfig.GetAnimationTargets(level);
        if (targets == null || targets.Count == 0)
        {
            if (showDebug)
            {
                // 调试信息已移除
            }
            return;
        }
        
        // 播放每个目标的动画
        foreach (var target in targets)
        {
            PlayAnimationOnTarget(target.targetName, target.animationName);
        }
        
        if (showDebug)
        {
            // 调试信息已移除
        }
    }
    
    /// <summary>
    /// 在指定目标上播放动画
    /// </summary>
    /// <param name="targetName">目标对象名称</param>
    /// <param name="animationName">动画名称</param>
    private void PlayAnimationOnTarget(string targetName, string animationName)
    {
        if (string.IsNullOrEmpty(targetName) || string.IsNullOrEmpty(animationName))
        {
            return;
        }
        
        // 尝试从缓存中获取动画控制器
        SpriteAnimationController controller = null;
        if (!animationControllers.TryGetValue(targetName, out controller))
        {
            // 如果缓存中没有，尝试在场景中查找
            GameObject targetObj = GameObject.Find(targetName);
            if (targetObj != null)
            {
                controller = targetObj.GetComponent<SpriteAnimationController>();
                if (controller != null)
                {
                    // 添加到缓存
                    animationControllers[targetName] = controller;
                }
            }
        }
        
        if (controller != null)
        {
            // 播放动画
            controller.SetCurrentSequence(animationName, true);
            
            if (showDebug)
            {
                // 调试信息已移除
            }
        }
        else
        {
            // 调试信息已移除
        }
    }
    
    /// <summary>
    /// 播放Perfect判定动画
    /// </summary>
    public void PlayPerfectAnimation()
    {
        PlayJudgmentAnimation(JudgmentAnimationConfig.JudgmentLevel.Perfect);
    }
    
    /// <summary>
    /// 播放Good判定动画
    /// </summary>
    public void PlayGoodAnimation()
    {
        PlayJudgmentAnimation(JudgmentAnimationConfig.JudgmentLevel.Good);
    }
    
    /// <summary>
    /// 播放Miss判定动画
    /// </summary>
    public void PlayMissAnimation()
    {
        PlayJudgmentAnimation(JudgmentAnimationConfig.JudgmentLevel.Miss);
    }
} 