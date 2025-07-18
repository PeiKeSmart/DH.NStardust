﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Remoting;
using NewLife.Remoting.Extensions;
using NewLife.Serialization;
using Stardust.Data.Deployment;
using Stardust.Data.Nodes;
using Stardust.Models;
using Stardust.Server.Services;
using XCode.Membership;
using TokenService = Stardust.Server.Services.TokenService;

namespace Stardust.Server.Controllers;

/// <summary>发布中心服务</summary>
[ApiFilter]
[Route("[controller]/[action]")]
public class DeployController : BaseController
{
    private Node _node;
    private String _clientId;
    private readonly NodeService _nodeService;
    private readonly DeployService _deployService;
    private readonly TokenService _tokenService;
    private readonly StarServerSetting _setting;

    public DeployController(DeployService deployService, NodeService nodeService, TokenService tokenService, StarServerSetting setting, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _deployService = deployService;
        _nodeService = nodeService;
        _tokenService = tokenService;
        _setting = setting;
    }

    #region 令牌验证
    protected override Boolean OnAuthorize(String token)
    {
        ManageProvider.UserHost = UserHost;

        var (jwt, node, ex) = _nodeService.DecodeToken(token, _setting.TokenSecret);
        _node = node;
        _clientId = jwt.Id;
        if (ex != null) throw ex;

        return node != null;
    }

    protected override void OnWriteError(String action, String message) => WriteHistory(0, action, false, message);
    #endregion

    /// <summary>获取分配到本节点的应用服务信息</summary>
    /// <returns></returns>
    public DeployInfo[] GetAll(Int32 deployId, String deployName, String appName)
    {
        var list = AppDeployNode.FindAllByNodeId(_node.ID);

        var rs = new List<DeployInfo>();
        foreach (var item in list)
        {
            // 不返回未启用的发布集，如果需要在客户端删除，则通过指令下发来实现
            if (!item.Enable) continue;

            // 过滤需要的应用部署
            if (deployId > 0 && item.Id != deployId) continue;

            var app = item.Deploy;
            if (app == null || !app.Enable) continue;

            if (!deployName.IsNullOrEmpty())
            {
                if (!item.DeployName.IsNullOrEmpty() && item.DeployName != deployName ||
                    item.DeployName.IsNullOrEmpty() && app.Name != deployName) continue;
            }
            if (!appName.IsNullOrEmpty() && app.AppName != appName) continue;

            var inf = _deployService.BuildDeployInfo(item, _node);
            if (inf == null) continue;

            rs.Add(inf);
            WriteHistory(app.Id, nameof(GetAll), true, inf.ToJson());
        }

        return rs.ToArray();
    }

    /// <summary>上传本节点的所有应用服务信息</summary>
    /// <param name="services"></param>
    /// <returns></returns>
    [HttpPost]
    public Int32 Upload([FromBody] ServiceInfo[] services)
    {
        if (services == null || services.Length == 0) return 0;

        // 本节点所有发布
        var list = AppDeployNode.FindAllByNodeId(_node.ID);

        var rs = 0;
        foreach (var svc in services)
        {
            if (svc.Name.IsNullOrEmpty()) continue;

            AppDeploy app = null;

            // 发布节点可能有自定义名字
            var dn = list.FirstOrDefault(e => e.DeployName.EqualIgnoreCase(svc.Name));
            app = dn?.Deploy;

            app ??= AppDeploy.FindByName(svc.Name);
            app ??= new AppDeploy { Name = svc.Name/*, Enable = svc.Enable*/ };

            //// 仅可用应用
            //if (app.Enable)
            {
                if (app.FileName.IsNullOrEmpty()) app.FileName = svc.FileName;
                if (app.Arguments.IsNullOrEmpty()) app.Arguments = svc.Arguments;
                if (app.WorkingDirectory.IsNullOrEmpty()) app.WorkingDirectory = svc.WorkingDirectory;
                if (app.UserName.IsNullOrEmpty()) app.UserName = svc.UserName;
                if (app.Environments.IsNullOrEmpty()) app.Environments = svc.Environments;
                if (app.Mode < 0) app.Mode = svc.Mode;

                app.MaxMemory = svc.MaxMemory;
            }

            // 先保存，可能有插入，需要取得应用发布Id
            var rs2 = app.Save();

            if (rs2 > 0) WriteHistory(app.Id, nameof(Upload), true, svc.ToJson());

            rs += rs2;

            dn ??= list.FirstOrDefault(e => e.DeployId == app.Id);
            if (dn == null)
                dn = new AppDeployNode { DeployId = app.Id, NodeId = _node.ID, Enable = svc.Enable };
            else
                list.Remove(dn);

            if (svc.Enable && app.Enable) dn.Enable = true;
            dn.LastUpload = DateTime.Now;

            dn.Save();
        }

        // 没有匹配的应用，本次禁用
        foreach (var item in list)
        {
            item.Enable = false;
            item.LastUpload = DateTime.Now;
            item.Update();
        }

        return rs;
    }

    /// <summary>应用心跳。上报应用信息</summary>
    /// <param name="inf"></param>
    /// <returns></returns>
    [HttpPost]
    public Int32 Ping([FromBody] AppInfo inf) => _deployService.Ping(_node, inf, UserHost);

    /// <summary>获取分配到本节点的应用发布任务</summary>
    public BuildTask GetBuildTask(Int32 deployId, String deployName, String appName)
    {
        return null;
    }

    /// <summary>更新编译任务</summary>
    /// <param name="result"></param>
    [HttpPost]
    public Int32 UpdateBuildTask(BuildResult result)
    {
        var ver = AppDeployVersion.FindById(result.Id)
            ?? throw new ArgumentNullException(nameof(result));

        if (!result.CommitId.IsNullOrEmpty()) ver.CommitId = result.CommitId;
        if (!result.CommitLog.IsNullOrEmpty()) ver.CommitLog = result.CommitLog;
        if (result.CommitTime.Year > 2000) ver.CommitTime = result.CommitTime;
        if (!result.Progress.IsNullOrEmpty()) ver.Progress = result.Progress;

        return ver.Update();
    }

    /// <summary>上传编译任务结果文件</summary>
    [HttpPost]
    public String UploadBuildFile(Int32 taskId, [FromForm] IFormFile file)
    {
        var ver = AppDeployVersion.FindById(taskId);
        if (ver == null) throw new ArgumentNullException(nameof(taskId));

        //// 仅支持部分包和标准包
        //if (info.Mode > DeployModes.Standard) throw new ApiException(400, "仅支持部分包和标准包！");

        //// 仅支持覆盖文件
        //if (info.Overwrite.IsNullOrEmpty()) throw new ApiException(400, "请指定覆盖文件！");

        // 保存应用发布信息
        var rs = 0;
        //var rs = _deployService.UploadBuildTask(_node, info, UserHost);
        //WriteHistory(info.AppId, nameof(UploadBuildTask), rs > 0, info.ToJson());

        return rs > 0 ? "ok" : "fail";
    }

    #region 辅助
    private void WriteHistory(Int32 appId, String action, Boolean success, String remark) => _deployService.WriteHistory(appId, _node?.ID ?? 0, action, success, remark, UserHost);
    #endregion
}