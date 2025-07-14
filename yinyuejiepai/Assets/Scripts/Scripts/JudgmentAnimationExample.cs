using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

/// <summary>
/// 判定动画示例脚本，展示如何配置和触发不同判定等级的动画
/// </summary>
public class JudgmentAnimationExample : MonoBehaviour
{
    [TitleGroup("配置引用")]
    [LabelText("判定动画管理器")]
    [Required("请设置判定动画管理器")]
    public JudgmentAnimationManager animationManager;
    
    [LabelText("角色输入管理器")]
    [Required("请设置角色输入管理器")]
    public CharacterInputManager inputManager;
    
    [TitleGroup("显示设置")]
    [LabelText("显示当前状态的文本")]
    public TextMeshProUGUI statusText;
    
    private void Start()
    {
        if (animationManager == null)
        {
            enabled = false;
            return;
        }
        
        if (inputManager == null)
        {
            enabled = false;
            return;
        }
        
        // 订阅角色按键事件
        inputManager.OnCharacterKeyPressed += OnCharacterKeyPressed;
        
        // 更新状态文本
        UpdateStatusText();
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        if (inputManager != null)
        {
            inputManager.OnCharacterKeyPressed -= OnCharacterKeyPressed;
        }
    }
    
    /// <summary>
    /// 角色按键事件处理
    /// </summary>
    private void OnCharacterKeyPressed(string characterName, KeyCode keyCode, float pressTime)
    {
        // 这里可以根据不同的角色或按键触发不同的判定动画
        // 简单示例：根据角色名称的首字母来决定判定等级
        
        if (characterName.StartsWith("P", System.StringComparison.OrdinalIgnoreCase))
        {
            animationManager.PlayPerfectAnimation();
            // 调试信息已移除
        }
        else if (characterName.StartsWith("G", System.StringComparison.OrdinalIgnoreCase))
        {
            animationManager.PlayGoodAnimation();
            // 调试信息已移除
        }
        else
        {
            animationManager.PlayMissAnimation();
            // 调试信息已移除
        }
    }
    
    private void Update()
    {
        // 定期更新状态文本
        if (Time.frameCount % 30 == 0) // 每30帧更新一次
        {
            UpdateStatusText();
        }
    }
    
    // 更新状态文本
    private void UpdateStatusText()
    {
        if (statusText == null || animationManager == null || inputManager == null)
            return;
        
        string info = "判定动画测试:\n";
        info += "使用角色输入管理器中配置的按键触发判定\n\n";
        
        // 显示当前配置的角色按键
        info += "当前角色按键配置:\n";
        foreach (var input in inputManager.characterInputs)
        {
            info += $"{input.characterName}: {input.keyCode}\n";
        }
        
        statusText.text = info;
    }
} 