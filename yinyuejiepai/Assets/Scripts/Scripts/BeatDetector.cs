using UnityEngine;
using System.Collections.Generic;

public class BeatDetector : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public float sampleRate = 44100f;
    public float clipLength = 0.1f; // 采样时长
    
    [Header("Beat Detection Settings")]
    [Range(0.01f, 1.0f)]
    public float threshold = 0.1f;   // 能量阈值
    [Range(0.05f, 0.5f)]
    public float minBeatInterval = 0.2f; // 最小拍子间隔（秒）
    
    [Header("Debug Settings")]
    public bool showDebugInfo = true;
    
    private float[] samples;
    private float[] energyHistory;
    private int historySize = 43;
    private float lastPeakTime = 0f;
    
    // 拍子显示相关变量
    public float currentBPM { get; private set; }
    private int beatCount = 0;    // 总拍数，用于计算在第几个8拍
    public int currentBeatInGroup { get; private set; }   // 当前在8拍中的第几拍 (1-8)
    public int currentGroupNumber { get; private set; }   // 当前是第几个8拍组 (从1开始)
    public float lastBeatTime { get; private set; }
    public float beatInterval { get; private set; }
    
    // 调试信息
    public float currentEnergy { get; private set; }
    public float averageEnergy { get; private set; }
    private float maxEnergy = 0;
    private Queue<float> recentEnergies = new Queue<float>();
    private int energyQueueSize = 50;
    
    // 拍子检测相关
    private float expectedBeatInterval = 0f;
    private bool isFirstBeat = true;
    private bool isInitialized = false;

    void Start()
    {
        InitializeArrays();
        ResetValues();
        
        if (audioSource == null)
        {
            enabled = false;
            return;
        }
        
        isInitialized = true;
        // 调试信息已移除
    }

    void InitializeArrays()
    {
        int sampleSize = (int)(sampleRate * clipLength);
        samples = new float[sampleSize];
        energyHistory = new float[historySize];
        recentEnergies = new Queue<float>();
    }

    void ResetValues()
    {
        currentBPM = 0;
        lastBeatTime = 0;
        beatInterval = 0;
        currentEnergy = 0;
        maxEnergy = 0;
        averageEnergy = 0;
        recentEnergies.Clear();
        lastPeakTime = 0f;
        
        // 重置节奏模式计数
        beatCount = 0;
        currentBeatInGroup = 0;
        currentGroupNumber = 1;
        expectedBeatInterval = 0f;
        isFirstBeat = true;
    }

    void Update()
    {
        if (!isInitialized) return;
        
        if (audioSource != null && audioSource.isPlaying)
        {
            AnalyzeAudio();
        }
        else if (showDebugInfo)
        {
            // 调试信息已移除
        }
    }

    void AnalyzeAudio()
    {
        // 获取音频数据
        audioSource.GetOutputData(samples, 0);
        
        // 计算当前帧的能量
        float energy = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            energy += samples[i] * samples[i];
        }
        
        currentEnergy = Mathf.Sqrt(energy / samples.Length);
        maxEnergy = Mathf.Max(maxEnergy, currentEnergy);

        // 更新能量历史队列
        recentEnergies.Enqueue(currentEnergy);
        if (recentEnergies.Count > energyQueueSize)
        {
            recentEnergies.Dequeue();
        }

        // 计算平均能量
        float sum = 0;
        foreach (float e in recentEnergies)
        {
            sum += e;
        }
        averageEnergy = sum / recentEnergies.Count;

        // 更新能量历史数组
        System.Array.Copy(energyHistory, 1, energyHistory, 0, energyHistory.Length - 1);
        energyHistory[energyHistory.Length - 1] = currentEnergy;

        // 检测拍子
        DetectBeat();
    }

    void DetectBeat()
    {
        float currentTime = Time.time;
        
        // 确保有足够的能量历史数据
        if (recentEnergies.Count < energyQueueSize)
            return;

        // 检查是否超过最小拍子间隔
        if (currentTime - lastPeakTime < minBeatInterval)
            return;

        // 计算动态阈值
        float dynamicThreshold = averageEnergy + (maxEnergy - averageEnergy) * threshold;

        // 检测峰值
        if (currentEnergy > dynamicThreshold && 
            currentEnergy > energyHistory[energyHistory.Length - 2] &&
            currentEnergy > averageEnergy * 1.1f) // 确保显著高于平均值
        {
            // 更新拍子信息
            beatCount++;
            
            // 更新8拍组和当前拍位置
            currentBeatInGroup = (beatCount - 1) % 8 + 1;
            currentGroupNumber = (beatCount - 1) / 8 + 1;
            
            float previousBeatTime = lastBeatTime;
            lastBeatTime = currentTime;
            lastPeakTime = currentTime;

            // 计算和更新BPM
            if (previousBeatTime > 0)
            {
                beatInterval = currentTime - previousBeatTime;
                float instantBPM = 60f / beatInterval;
                
                if (isFirstBeat)
                {
                    currentBPM = instantBPM;
                    expectedBeatInterval = beatInterval;
                    isFirstBeat = false;
                }
                else
                {
                    // 平滑BPM值
                    currentBPM = Mathf.Lerp(currentBPM, instantBPM, 0.2f);
                    expectedBeatInterval = Mathf.Lerp(expectedBeatInterval, beatInterval, 0.2f);
                }
            }

            if (showDebugInfo)
            {
                // 调试信息已移除
            }
            
            // 拍点检测信息已移除
        }
    }

    public void ClearPeakTimes()
    {
        ResetValues();
    }

    // 获取当前节奏状态的描述
    public string GetBeatPatternInfo()
    {
        return $"第{currentGroupNumber}个八拍的\n第{currentBeatInGroup}拍";
    }
} 