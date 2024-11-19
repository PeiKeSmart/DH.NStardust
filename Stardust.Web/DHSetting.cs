using System.ComponentModel;

using NewLife.Configuration;
using NewLife.Security;

namespace Stardust.Web;

/// <summary>配置</summary>
[Config("DHSetting")]
public class DHSetting : Config<DHSetting>
{
    /// <summary>接口密钥</summary>
    [Description("接口密钥")]
    public String ApiPwd { get; set; } = Rand.NextString(16);
}
