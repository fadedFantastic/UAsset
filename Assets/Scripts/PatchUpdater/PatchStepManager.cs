using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 热更步骤管理器
/// </summary>
public static class PatchStepManager
{
    private static readonly List<IPatchStep> s_PatchSteps = new List<IPatchStep>();
    private static IPatchStep s_CurStep;

    /// <summary>
    /// 启动
    /// </summary>
    public static void Run(string entryStep)
    {
        s_CurStep = GetStep(entryStep);
        if (s_CurStep != null)
        {
            s_CurStep.OnEnter();
        }
        else
        {
            Debug.LogError($"Not found entry step: {entryStep}");
        }
    }

    /// <summary>
    /// 切换步骤
    /// </summary>
    public static void Transition(string stepName)
    {
        if (string.IsNullOrEmpty(stepName))
        {
            throw new ArgumentNullException();
        }

        var step = GetStep(stepName);
        if (step == null)
        {
            Debug.LogError($"Not found step {stepName}");
            return;
        }

        Debug.Log($"PatchStep change {s_CurStep.Name} to {step.Name}");
        s_CurStep.OnExit();
        s_CurStep = step;
        s_CurStep.OnEnter();
    }

    /// <summary>
    /// 添加步骤
    /// </summary>
    /// <param name="step"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void AddStep(IPatchStep step)
    {
        if (step == null)
        {
            throw new ArgumentNullException();
        }

        if (!s_PatchSteps.Contains(step))
        {
            s_PatchSteps.Add(step);   
        }
        else
        {
            Debug.LogError($"Step {step.Name} already existed");
        }
    }

    /// <summary>
    /// 获取步骤
    /// </summary>
    /// <param name="stepName">步骤名</param>
    /// <returns></returns>
    private static IPatchStep GetStep(string stepName)
    {
        foreach (var step in s_PatchSteps)
        {
            if (step.Name == stepName)
            {
                return step;
            }
        }
        return null;
    }
}