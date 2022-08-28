/// <summary>
/// 更新步骤接口
/// </summary>
public interface IPatchStep
{
    /// <summary>
    /// 步骤名称
    /// </summary>
    string Name { get; }

    void OnEnter();

    void OnExit();
}