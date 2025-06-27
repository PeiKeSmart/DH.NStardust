using NewLife.Agent;
using NewLife.Agent.Command;
using Stardust.Managers;

namespace StarAgent.CommandHandler;

public class ListServices : BaseCommandHandler
{
    public ListServices(ServiceBase service) : base(service)
    {
        Cmd = "-ListServices";
        Description = "查看子服务";
        ShortcutKey = 'l';
    }

    public override void Process(String[] args)
    {
        var service = (MyService)Service;
        var manager = service.Provider?.GetService(typeof(ServiceManager)) as ServiceManager;
        
        if (manager == null || manager.Services == null || manager.Services.Length == 0)
        {
            Console.WriteLine("没有找到任何子服务");
            return;
        }

        Console.WriteLine("所有子服务列表：");
        Console.WriteLine("序号\t服务名称\t启用");
        
        for (int i = 0; i < manager.Services.Length; i++)
        {
            var svc = manager.Services[i];
            
            Console.WriteLine("{0}\t{1}\t{2}", 
                i + 1, 
                svc.Name, 
                svc.Enable ? "是" : "否");
        }
    }
}