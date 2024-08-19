﻿using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Cube;
using NewLife.Cube.ViewModels;
using NewLife.Web;
using Stardust.Data;
using XCode.Membership;

namespace Stardust.Web.Areas.Registry.Controllers;

[RegistryArea]
[Menu(0, false)]
public class AppServiceController : EntityController<AppService>
{
    static AppServiceController()
    {
        ListFields.RemoveField("ServiceId", "OriginAddress");

        {
            var df = ListFields.GetField("ServiceName") as ListField;
            df.DisplayName = "{ServiceName}";
            df.Url = "/Registry/Service?name={ServiceName}";
        }
        {
            var df = ListFields.GetField("NodeName") as ListField;
            df.Header = "节点";
            df.DisplayName = "{NodeName}";
            df.Url = "/Nodes/Node?Id={NodeId}";
            df.Target = "_blank";
        }
        {
            var df = ListFields.GetField("Client") as ListField;
            df.Url = "/Registry/AppService?appId={AppId}&client={Client}";
        }
    }

    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        base.OnActionExecuting(filterContext);

        var appId = GetRequest("appId").ToInt(-1);
        if (appId > 0)
        {
            PageSetting.NavView = "_App_Nav";
            PageSetting.EnableNavbar = false;
        }
    }

    protected override FieldCollection OnGetFields(ViewKinds kind, Object model)
    {
        var fields = base.OnGetFields(kind, model);

        if (kind == ViewKinds.List)
        {
            var appId = GetRequest("appId").ToInt(-1);
            if (appId > 0) fields.RemoveField("AppName");
        }

        return fields;
    }

    protected override IEnumerable<AppService> Search(Pager p)
    {
        PageSetting.EnableAdd = false;
        //PageSetting.EnableNavbar = false;

        var appId = p["appId"].ToInt(-1);
        var client = p["client"];
        var serviceId = p["serviceId"].ToInt(-1);
        var enable = p["enable"]?.ToBoolean();

        return AppService.Search(appId, serviceId, client, enable, p["Q"], p);
    }
}