using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

[System.Serializable]
public class AnimationSequence
{
    [LabelText("动画名称")]
    [Required("动画名称不能为空")]
    public string name;

    [LabelText("起始帧")]
    [MinValue(0)]
    [OnValueChanged("ValidateFrameRange")]
    public int startFrame;

    [LabelText("结束帧")]
    [MinValue(0)]
    [OnValueChanged("ValidateFrameRange")]
    public int endFrame;

    [LabelText("触发按键")]
    [HorizontalGroup("按键设置")]
    public KeyCode triggerKey = KeyCode.None;

    [LabelText("跟随节拍")]
    [HorizontalGroup("节拍设置", Width = 100)]
    public bool playOnBeat = true;

    [LabelText("节拍间隔")]
    [HorizontalGroup("节拍设置")]
    [ShowIf("playOnBeat")]
    [MinValue(1), MaxValue(8)]
    public int beatInterval = 1;

    [LabelText("帧率")]
    [HideIf("playOnBeat")]
    [MinValue(1)]
    public float manualFPS = 12f;

    [LabelText("循环播放")]
    [Tooltip("关闭此选项后，动画将只播放一次就返回默认动画")]
    public bool looping = true;

    private void ValidateFrameRange()
    {
        if (endFrame < startFrame)
        {
            endFrame = startFrame;
        }
    }
}

public class SpriteAnimationController : SerializedMonoBehaviour
{
    [TitleGroup("图集设置")]
    [Required("请设置精灵图集")]
    [PreviewField(100)]
    [LabelText("精灵图集")]
    [OnValueChanged("UpdateEditorSprite")]
    public Sprite spriteSheet;

    [TitleGroup("图集设置")]
    [HorizontalGroup("图集设置/大小")]
    [LabelText("帧宽度")]
    [MinValue(1)]
    [OnValueChanged("UpdateEditorSprite")]
    public int frameWidth = 32;

    [HorizontalGroup("图集设置/大小")]
    [LabelText("帧高度")]
    [MinValue(1)]
    [OnValueChanged("UpdateEditorSprite")]
    public int frameHeight = 32;

    [TitleGroup("图集设置")]
    [LabelText("总帧数")]
    [MinValue(1)]
    [OnValueChanged("ValidateTotalFrames")]
    public int totalFrames = 8;

    [TitleGroup("动画序列")]
    [TableList(ShowIndexLabels = true)]
    [LabelText("动画序列列表")]
    public List<AnimationSequence> sequences = new List<AnimationSequence>();

    [TitleGroup("动画序列")]
    [ValueDropdown("GetSequenceNames")]
    [LabelText("默认动画")]
    public string defaultSequence;

    [TitleGroup("节拍设置")]
    [Required("请设置节拍检测器")]
    [LabelText("节拍检测器")]
    [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    public BeatDetector beatDetector;

    [TitleGroup("调试设置")]
    [LabelText("显示调试信息")]
    public bool showDebug = true;

    // 私有成员
    private Image image;
    private float frameTimer;
    private int currentFrame;
    private Rect[] frameRects;
    private int lastBeatCount;
    private bool isInitialized = false;
    private bool isPlayingBeatAnimation = false;
    private float beatAnimationStartTime;
    private AnimationSequence currentSequence;
    [OdinSerialize]
    private Dictionary<string, AnimationSequence> sequenceMap = new Dictionary<string, AnimationSequence>();
    private bool isPlayingTempSequence = false;

    #if UNITY_EDITOR
    private IEnumerable<string> GetSequenceNames()
    {
        return sequences.Select(s => s.name);
    }

    private void ValidateTotalFrames()
    {
        if (spriteSheet != null)
        {
            int maxPossibleFrames = (spriteSheet.texture.width / frameWidth) * (spriteSheet.texture.height / frameHeight);
            if (totalFrames > maxPossibleFrames)
            {
                totalFrames = maxPossibleFrames;
            }
        }
        
        UpdateEditorSprite();
    }

    // 在编辑器中更新精灵图像
    private void UpdateEditorSprite()
    {
        if (!Application.isPlaying && spriteSheet != null)
        {
            Image img = GetComponent<Image>();
            if (img != null && frameWidth > 0 && frameHeight > 0)
            {
                // 创建第一帧的Sprite
                Sprite firstFrameSprite = Sprite.Create(
                    spriteSheet.texture,
                    new Rect(0, spriteSheet.texture.height - frameHeight, frameWidth, frameHeight),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                
                img.sprite = firstFrameSprite;
            }
        }
    }

    // 在Inspector中的值发生变化时调用
    private void OnValidate()
    {
        UpdateEditorSprite();
    }

    [Button("重置动画")]
    [TitleGroup("调试设置")]
    private void EditorResetAnimation()
    {
        ResetAnimation();
    }
    #endif

    void OnEnable()
    {
        Initialize();
    }

    void Initialize()
    {
        image = GetComponent<Image>();
        if (image == null)
        {
            enabled = false;
            return;
        }
        
        if (spriteSheet == null)
        {
            enabled = false;
            return;
        }

        // 初始化序列映射
        sequenceMap.Clear();
        foreach (var seq in sequences)
        {
            if (string.IsNullOrEmpty(seq.name))
            {
                continue;
            }
            
            if (seq.startFrame < 0 || seq.endFrame >= totalFrames || seq.startFrame > seq.endFrame)
            {
                continue;
            }
            
            sequenceMap[seq.name] = seq;
        }

        InitializeFrames();
        
        // 设置默认序列
        if (!string.IsNullOrEmpty(defaultSequence) && sequenceMap.ContainsKey(defaultSequence))
        {
            SetCurrentSequence(defaultSequence);
        }
        else if (sequences.Count > 0)
        {
            SetCurrentSequence(sequences[0].name);
        }
        
        isInitialized = true;
    }
    
    void InitializeFrames()
    {
        if (frameWidth <= 0 || frameHeight <= 0 || totalFrames <= 0)
        {
            enabled = false;
            return;
        }

        frameRects = new Rect[totalFrames];
        float normalizedWidth = (float)frameWidth / spriteSheet.texture.width;
        float normalizedHeight = (float)frameHeight / spriteSheet.texture.height;

        for (int i = 0; i < totalFrames; i++)
        {
            float x = (i * frameWidth) % spriteSheet.texture.width;
            float y = spriteSheet.texture.height - frameHeight - (((i * frameWidth) / spriteSheet.texture.width) * frameHeight);
            
            frameRects[i] = new Rect(
                x / spriteSheet.texture.width,
                y / spriteSheet.texture.height,
                normalizedWidth,
                normalizedHeight
            );
        }
    }

    void UpdateSpriteFrame(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= totalFrames) return;
        
        Rect rect = frameRects[frameIndex];
        int texWidth = spriteSheet.texture.width;
        int texHeight = spriteSheet.texture.height;
        int x = (int)(rect.x * texWidth);
        int y = (int)(rect.y * texHeight);
        if (x < 0 || y < 0 || x + frameWidth > texWidth || y + frameHeight > texHeight)
        {
            return;
        }
        image.sprite = Sprite.Create(
            spriteSheet.texture,
            new Rect(x, y, frameWidth, frameHeight),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }
    
    void Update()
    {
        if (!isInitialized || currentSequence == null) return;

        // 检查按键触发
        foreach (var seq in sequences)
        {
            if (Input.GetKeyDown(seq.triggerKey))
            {
                SetCurrentSequence(seq.name, true); // 设置为临时序列
                break;
            }
        }

        if (currentSequence.playOnBeat && beatDetector != null)
        {
            // 获取当前总拍数
            int currentBeatCount = (beatDetector.currentGroupNumber - 1) * 8 + beatDetector.currentBeatInGroup;
            
            // 检查是否需要开始新的动画循环
            if (currentBeatCount > lastBeatCount && currentBeatCount % currentSequence.beatInterval == 0)
            {
                // 开始新的动画循环
                isPlayingBeatAnimation = true;
                beatAnimationStartTime = Time.time;
                currentFrame = currentSequence.startFrame;
                lastBeatCount = currentBeatCount;
            }

            // 如果正在播放节拍动画
            if (isPlayingBeatAnimation)
            {
                // 计算动画进度
                float beatDuration = beatDetector.beatInterval;
                float animationProgress = (Time.time - beatAnimationStartTime) / beatDuration;
                int frameCount = currentSequence.endFrame - currentSequence.startFrame + 1;
                
                // 计算当前应该显示的帧
                int frameOffset = Mathf.FloorToInt(animationProgress * frameCount);
                int targetFrame = currentSequence.startFrame + frameOffset;
                
                // 如果动画还没结束且需要更新帧
                if (frameOffset < frameCount && targetFrame != currentFrame)
                {
                    currentFrame = targetFrame;
                    UpdateSpriteFrame(currentFrame);
                }
                // 如果动画结束
                else if (frameOffset >= frameCount)
                {
                    isPlayingBeatAnimation = false;
                    
                    // 如果是临时序列且不循环，返回默认序列
                    if (isPlayingTempSequence && !currentSequence.looping)
                    {
                        ReturnToDefaultSequence();
                    }
                    else
                    {
                        currentFrame = currentSequence.startFrame;
                        UpdateSpriteFrame(currentFrame);
                    }
                }
            }
        }
        else
        {
            // 使用普通帧率播放
            frameTimer += Time.deltaTime;
            if (frameTimer >= 1f / currentSequence.manualFPS)
            {
                frameTimer = 0f;
                currentFrame++;
                
                // 检查是否到达序列末尾
                if (currentFrame > currentSequence.endFrame)
                {
                    // 如果是临时序列且不循环，返回默认序列
                    if (isPlayingTempSequence && !currentSequence.looping)
                    {
                        ReturnToDefaultSequence();
                    }
                    else
                    {
                        currentFrame = currentSequence.startFrame;
                        UpdateSpriteFrame(currentFrame);
                    }
                }
                else
                {
                    UpdateSpriteFrame(currentFrame);
                }
            }
        }
    }

    // 设置当前播放的序列
    public void SetCurrentSequence(string sequenceName, bool isTemporary = false)
    {
        if (!sequenceMap.ContainsKey(sequenceName))
        {
            return;
        }

        currentSequence = sequenceMap[sequenceName];
        currentFrame = currentSequence.startFrame;
        frameTimer = 0f;
        isPlayingBeatAnimation = false;
        isPlayingTempSequence = isTemporary;
        UpdateSpriteFrame(currentFrame);
    }

    // 手动重置动画
    public void ResetAnimation()
    {
        if (currentSequence != null)
        {
            currentFrame = currentSequence.startFrame;
            lastBeatCount = 0;
            frameTimer = 0f;
            isPlayingBeatAnimation = false;
            UpdateSpriteFrame(currentFrame);
        }
    }

    // 返回默认序列
    private void ReturnToDefaultSequence()
    {
        if (!string.IsNullOrEmpty(defaultSequence) && sequenceMap.ContainsKey(defaultSequence))
        {
            SetCurrentSequence(defaultSequence, false);
        }
    }

    void OnDisable()
    {
    }
} 