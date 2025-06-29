using Stardust.Managers;
using Stardust.Models;

namespace DH.NStardust.Models;

/// <summary>应用服务集合信息</summary>
public class ServicesInfo
{
    /// <summary>
    /// 应用服务集合
    /// </summary>
    public ServiceInfo[]? Services { get; set; }

    /// <summary>正在运行的应用服务信息</summary>
    public ProcessInfo[]? RunningServices { get; set; }
}
