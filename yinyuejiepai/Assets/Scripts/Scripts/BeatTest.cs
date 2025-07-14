using UnityEngine;

public class BeatTest : MonoBehaviour
{
    public AudioClip musicClip;
    public BeatDetector beatDetector;  // 改为public，从Inspector中设置
    private AudioSource audioSource;
    private BeatAction beatAction;
    
    // 拍子可视化效果
    private float visualFeedbackDuration = 0.1f;
    private float lastBeatVisualTime;
    private bool showBeatVisual;
    
    // GUI样式
    private GUIStyle labelStyle;
    private GUIStyle bigLabelStyle;
    private GUIStyle beatBoxStyle;
    private GUIStyle groupStyle;
    
    // 按键设置
    private bool isSettingKey = false;
    private int settingGroupNumber = 1;
    private int settingBeat = 1;
    public AudioClip[] availableAudioClips; // 在Inspector中设置可用的音频片段
    private int selectedAudioClipIndex = 0;
    private float settingVolume = 1f;

    void Start()
    {
        // 设置音频源
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = musicClip;
        audioSource.playOnAwake = false;

        // 检查BeatDetector引用
        if (beatDetector == null)
        {
            enabled = false;
            return;
        }
        
        // 设置BeatDetector的音频源
        beatDetector.audioSource = audioSource;
        
        // 设置按键动作管理器
        beatAction = gameObject.AddComponent<BeatAction>();
        beatAction.beatDetector = beatDetector;
    }
    
    void InitializeGUIStyles()
    {
        if (labelStyle == null)
        {
            // 普通标签样式
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 14;
            labelStyle.alignment = TextAnchor.MiddleLeft;
        }
        
        if (bigLabelStyle == null)
        {
            // 大号标签样式
            bigLabelStyle = new GUIStyle(GUI.skin.label);
            bigLabelStyle.normal.textColor = Color.white;
            bigLabelStyle.fontSize = 20;
            bigLabelStyle.alignment = TextAnchor.MiddleCenter;
            bigLabelStyle.fontStyle = FontStyle.Bold;
        }
        
        if (beatBoxStyle == null)
        {
            // 拍子框样式
            beatBoxStyle = new GUIStyle(GUI.skin.box);
            beatBoxStyle.fontSize = 24;
            beatBoxStyle.alignment = TextAnchor.MiddleCenter;
        }
        
        if (groupStyle == null)
        {
            // 组标签样式
            groupStyle = new GUIStyle(GUI.skin.label);
            groupStyle.fontSize = 16;
            groupStyle.normal.textColor = Color.yellow;
            groupStyle.alignment = TextAnchor.MiddleCenter;
        }
    }

    void Update()
    {
        // 更新拍子视觉效果
        if (showBeatVisual && Time.time - lastBeatVisualTime > visualFeedbackDuration)
        {
            showBeatVisual = false;
        }

        // 检查是否有新的拍子
        if (beatDetector.lastBeatTime > lastBeatVisualTime)
        {
            showBeatVisual = true;
            lastBeatVisualTime = beatDetector.lastBeatTime;
        }
        
        // 如果正在等待按键设置
        if (isSettingKey)
        {
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    // 添加新的按键动作
                    if (availableAudioClips != null && availableAudioClips.Length > 0)
                    {
                        beatAction.AddBeatActionWithKeyCode(
                            settingGroupNumber,
                            settingBeat,
                            key,
                            availableAudioClips[selectedAudioClipIndex],
                            settingVolume
                        );
                        // 调试信息已移除
                    }
                    isSettingKey = false;
                    break;
                }
            }
        }
    }

    void OnGUI()
    {
        // 确保GUI样式已初始化
        InitializeGUIStyles();
        
        // 控制按钮区域
        GUILayout.BeginArea(new Rect(10, 10, 300, 600));
        
        // 播放控制
        if (!audioSource.isPlaying)
        {
            if (GUILayout.Button("开始播放", GUILayout.Height(30)))
            {
                beatDetector.ClearPeakTimes();
                audioSource.Play();
            }
        }
        else
        {
            if (GUILayout.Button("停止播放", GUILayout.Height(30)))
            {
                audioSource.Stop();
            }
        }

        GUILayout.Space(20);

        // 显示节奏信息
        GUILayout.Label("节拍信息", bigLabelStyle);
        GUILayout.Space(10);
        
        // 显示八拍组信息
        GUILayout.Label($"第{beatDetector.currentGroupNumber}个八拍", groupStyle);
        
        // 显示当前拍位置
        GUILayout.BeginHorizontal();
        GUILayout.Label("当前拍:", labelStyle, GUILayout.Width(60));
        DrawBeatIndicators(beatDetector.currentBeatInGroup, 8);
        GUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // 显示BPM
        GUILayout.Label($"BPM: {beatDetector.currentBPM:F1}", labelStyle);
        
        if (audioSource.isPlaying)
        {
            GUILayout.Space(10);
            GUILayout.Label($"播放时间: {audioSource.time:F2}s", labelStyle);
            
            // 显示能量信息
            GUILayout.Space(10);
            GUILayout.Label("音频能量", labelStyle);
            
            // 显示能量条
            float energyBarWidth = 280;
            float energyBarHeight = 20;
            Rect energyBarRect = GUILayoutUtility.GetRect(energyBarWidth, energyBarHeight);
            
            // 背景
            GUI.backgroundColor = Color.gray;
            GUI.Box(energyBarRect, "");
            
            // 能量值
            Rect currentEnergyRect = new Rect(
                energyBarRect.x,
                energyBarRect.y,
                energyBarRect.width * beatDetector.currentEnergy,
                energyBarRect.height
            );
            GUI.backgroundColor = Color.yellow;
            GUI.Box(currentEnergyRect, "");
            
            // 阈值线
            float thresholdX = energyBarRect.x + energyBarRect.width * beatDetector.threshold;
            Color thresholdColor = Color.red;
            thresholdColor.a = 0.5f;
            GUI.color = thresholdColor;
            GUI.DrawTexture(
                new Rect(thresholdX - 1, energyBarRect.y, 2, energyBarRect.height),
                Texture2D.whiteTexture
            );
            
            // 显示拍子视觉反馈
            GUILayout.Space(10);
            GUI.backgroundColor = showBeatVisual ? Color.green : Color.gray;
            GUILayout.Box(showBeatVisual ? "♪" : "○", beatBoxStyle, GUILayout.Height(50), GUILayout.Width(50));
        }
        
        // 阈值调节滑块
        GUILayout.Space(20);
        GUILayout.Label($"检测阈值: {beatDetector.threshold:F2}", labelStyle);
        beatDetector.threshold = GUILayout.HorizontalSlider(
            beatDetector.threshold,
            0.01f,
            1.0f
        );
        
        // 按键设置区域
        GUILayout.Space(30);
        GUILayout.Label("按键设置", bigLabelStyle);
        
        // 选择八拍组
        GUILayout.BeginHorizontal();
        GUILayout.Label("目标八拍组:", labelStyle);
        if (GUILayout.Button("<", GUILayout.Width(30))) settingGroupNumber = Mathf.Max(1, settingGroupNumber - 1);
        GUILayout.Label(settingGroupNumber.ToString(), GUILayout.Width(30));
        if (GUILayout.Button(">", GUILayout.Width(30))) settingGroupNumber++;
        GUILayout.EndHorizontal();
        
        // 选择拍子
        GUILayout.BeginHorizontal();
        GUILayout.Label("目标拍子:", labelStyle);
        if (GUILayout.Button("<", GUILayout.Width(30))) settingBeat = Mathf.Max(1, settingBeat - 1);
        GUILayout.Label(settingBeat.ToString(), GUILayout.Width(30));
        if (GUILayout.Button(">", GUILayout.Width(30))) settingBeat = Mathf.Min(8, settingBeat + 1);
        GUILayout.EndHorizontal();
        
        // 选择音频
        if (availableAudioClips != null && availableAudioClips.Length > 0)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("音频:", labelStyle);
            if (GUILayout.Button("<", GUILayout.Width(30))) 
                selectedAudioClipIndex = (selectedAudioClipIndex - 1 + availableAudioClips.Length) % availableAudioClips.Length;
            GUILayout.Label(availableAudioClips[selectedAudioClipIndex].name, GUILayout.Width(100));
            if (GUILayout.Button(">", GUILayout.Width(30))) 
                selectedAudioClipIndex = (selectedAudioClipIndex + 1) % availableAudioClips.Length;
            GUILayout.EndHorizontal();
        }
        
        // 音量设置
        GUILayout.Label($"音量: {settingVolume:F2}", labelStyle);
        settingVolume = GUILayout.HorizontalSlider(settingVolume, 0f, 1f);
        
        // 设置按键按钮
        GUI.backgroundColor = isSettingKey ? Color.yellow : Color.white;
        if (GUILayout.Button(isSettingKey ? "请按下要设置的按键..." : "设置新按键"))
        {
            isSettingKey = !isSettingKey;
        }
        
        // 清除所有按键设置
        if (GUILayout.Button("清除所有按键设置"))
        {
            beatAction.ClearAllActions();
        }
        
        GUILayout.EndArea();
    }
    
    void DrawBeatIndicators(int currentBeat, int totalBeats)
    {
        GUILayout.BeginHorizontal();
        for (int i = 1; i <= totalBeats; i++)
        {
            GUI.backgroundColor = i == currentBeat ? Color.green : Color.gray;
            GUILayout.Box(i.ToString(), GUILayout.Width(25), GUILayout.Height(25));
        }
        GUILayout.EndHorizontal();
    }
} 