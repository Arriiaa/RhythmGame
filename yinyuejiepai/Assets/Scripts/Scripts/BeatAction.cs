using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class BeatKeyAction
{
    [Tooltip("目标八拍组号")]
    public int targetGroupNumber;    // 目标八拍组号
    
    [Tooltip("目标拍号(1-8)")]
    [Range(1, 8)]
    public int targetBeat;           // 目标拍号(1-8)
    
    [Tooltip("角色名称，用于从配置中获取按键")]
    public string characterName;     // 角色名称，用于从配置中获取按键
    
    [Tooltip("要播放的音频")]
    public AudioClip audioClip;      // 要播放的音频
    
    [Tooltip("音量")]
    [Range(0, 1)]
    public float volume = 1f;        // 音量
}

public class BeatAction : MonoBehaviour
{
    [Tooltip("节拍检测器")]
    public BeatDetector beatDetector;
    
    [Tooltip("按键动作列表")]
    public List<BeatKeyAction> beatActions = new List<BeatKeyAction>();
    
    [Tooltip("角色判定配置")]
    public CharacterJudgmentConfig judgmentConfig; // 角色判定配置
    
    private Dictionary<KeyCode, AudioSource> audioSources = new Dictionary<KeyCode, AudioSource>();
    
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
        
        // 如果没有设置判定配置，尝试查找
        if (judgmentConfig == null)
        {
            var beatManager = FindObjectOfType<SimpleBeatManager>();
            if (beatManager != null && beatManager.judgmentConfig != null)
            {
                judgmentConfig = beatManager.judgmentConfig;
            }
        }
        
        // 为每个动作创建独立的AudioSource
        foreach (var action in beatActions)
        {
            KeyCode actualKeyCode = GetActualKeyCode(action);
            if (!audioSources.ContainsKey(actualKeyCode))
            {
                var audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.clip = action.audioClip;
                audioSource.volume = action.volume;
                audioSources[actualKeyCode] = audioSource;
            }
        }
    }
    
    // 获取实际按键
    KeyCode GetActualKeyCode(BeatKeyAction action)
    {
        // 如果角色名称是KeyCode字符串，直接解析
        if (!string.IsNullOrEmpty(action.characterName))
        {
            if (System.Enum.TryParse<KeyCode>(action.characterName, out KeyCode parsedKeyCode))
            {
                return parsedKeyCode;
            }
            
            // 如果不是KeyCode字符串，尝试从角色配置中获取
            if (judgmentConfig != null)
            {
                return judgmentConfig.GetKeyCodeForCharacter(action.characterName);
            }
        }
        // 没有配置就返回None
        return KeyCode.None;
    }
    
    void Update()
    {
        foreach (var action in beatActions)
        {
            // 获取实际按键
            KeyCode actualKeyCode = GetActualKeyCode(action);
            if (actualKeyCode == KeyCode.None) continue;
            
            // 检查是否在正确的八拍组和拍子位置
            bool isCorrectTiming = beatDetector.currentGroupNumber == action.targetGroupNumber &&
                                 beatDetector.currentBeatInGroup == action.targetBeat;
            
            // 检查按键是否被按下
            if (Input.GetKeyDown(actualKeyCode))
            {
                if (isCorrectTiming)
                {
                    // 正确时机按键 - 播放音频
                    PlayAudio(action, actualKeyCode);
                    // 调试信息已移除
                }
                else
                {
                    // 错误时机按键
                    // 调试信息已移除
                }
            }
        }
    }
    
    void PlayAudio(BeatKeyAction action, KeyCode actualKeyCode)
    {
        if (audioSources.TryGetValue(actualKeyCode, out AudioSource audioSource))
        {
            if (action.audioClip != null)
            {
                audioSource.clip = action.audioClip;
                audioSource.volume = action.volume;
                audioSource.Play();
            }
        }
    }
    
    // 添加新的按键动作
    public void AddBeatAction(int groupNumber, int beat, AudioClip clip, float volume = 1f, string characterName = "")
    {
        var newAction = new BeatKeyAction
        {
            targetGroupNumber = groupNumber,
            targetBeat = Mathf.Clamp(beat, 1, 8),
            characterName = characterName,
            audioClip = clip,
            volume = volume
        };
        
        beatActions.Add(newAction);
        
        // 获取实际按键
        KeyCode actualKeyCode = GetActualKeyCode(newAction);
        if (actualKeyCode == KeyCode.None) return;
        
        // 创建新的AudioSource（如果需要）
        if (!audioSources.ContainsKey(actualKeyCode))
        {
            var audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSources[actualKeyCode] = audioSource;
        }
    }
    
    // 添加新的按键动作（直接指定KeyCode）
    public void AddBeatActionWithKeyCode(int groupNumber, int beat, KeyCode keyCode, AudioClip clip, float volume = 1f)
    {
        var newAction = new BeatKeyAction
        {
            targetGroupNumber = groupNumber,
            targetBeat = Mathf.Clamp(beat, 1, 8),
            characterName = keyCode.ToString(), // 使用KeyCode作为角色名称
            audioClip = clip,
            volume = volume
        };
        
        beatActions.Add(newAction);
        
        // 创建新的AudioSource
        if (!audioSources.ContainsKey(keyCode))
        {
            var audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSources[keyCode] = audioSource;
        }
    }
    
    // 移除按键动作
    public void RemoveBeatAction(KeyCode key)
    {
        beatActions.RemoveAll(action => GetActualKeyCode(action) == key);
        
        // 移除对应的AudioSource
        if (audioSources.TryGetValue(key, out AudioSource audioSource))
        {
            Destroy(audioSource);
            audioSources.Remove(key);
        }
    }
    
    // 清除所有动作
    public void ClearAllActions()
    {
        beatActions.Clear();
        
        // 清除所有AudioSource
        foreach (var audioSource in audioSources.Values)
        {
            Destroy(audioSource);
        }
        audioSources.Clear();
    }
    
    // 重新加载按键配置
    public void ReloadKeyConfigs()
    {
        // 重新创建AudioSource
        foreach (var audioSource in audioSources.Values)
        {
            Destroy(audioSource);
        }
        audioSources.Clear();
        
        foreach (var action in beatActions)
        {
            KeyCode actualKeyCode = GetActualKeyCode(action);
            if (actualKeyCode == KeyCode.None) continue;
            if (!audioSources.ContainsKey(actualKeyCode))
            {
                var audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.clip = action.audioClip;
                audioSource.volume = action.volume;
                audioSources[actualKeyCode] = audioSource;
            }
        }
    }
} 