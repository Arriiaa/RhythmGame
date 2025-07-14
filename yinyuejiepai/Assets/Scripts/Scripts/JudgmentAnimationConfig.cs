using UnityEngine;
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "JudgmentAnimationConfig", menuName = "音乐节拍/判定动画配置", order = 2)]
public class JudgmentAnimationConfig : ScriptableObject
{
    public enum JudgmentLevel
    {
        Perfect,
        Good,
        Miss
    }

    [Serializable]
    public class AnimationTarget
    {
        [LabelText("目标对象名称")]
        [Tooltip("要播放动画的对象名称")]
        public string targetName;
        
        [LabelText("动画名称")]
        [Tooltip("要播放的动画名称，需要与SpriteAnimationController中的动画序列名称一致")]
        public string animationName;
    }

    [Serializable]
    public class JudgmentAnimation
    {
        [LabelText("判定等级")]
        [Tooltip("判定等级")]
        public JudgmentLevel level;
        
        [LabelText("动画目标列表")]
        [Tooltip("当达到此判定等级时要播放动画的目标列表")]
        [TableList]
        public List<AnimationTarget> targets = new List<AnimationTarget>();
    }
    
    [LabelText("判定动画配置")]
    [Tooltip("不同判定等级对应的动画配置")]
    [TableList]
    public List<JudgmentAnimation> judgmentAnimations = new List<JudgmentAnimation>();
    
    /// <summary>
    /// 获取指定判定等级的动画目标列表
    /// </summary>
    /// <param name="level">判定等级</param>
    /// <returns>动画目标列表</returns>
    public List<AnimationTarget> GetAnimationTargets(JudgmentLevel level)
    {
        foreach (var config in judgmentAnimations)
        {
            if (config.level == level)
            {
                return config.targets;
            }
        }
        
        // 调试信息已移除
        return null;
    }
} 