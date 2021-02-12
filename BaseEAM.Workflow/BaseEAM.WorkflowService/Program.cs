using System.Configuration;
using Topshelf;

namespace BaseEAM.WorkflowService
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<WorkflowServiceHost>(s =>
                {
                    s.ConstructUsing(name => new WorkflowServiceHost());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription(ConfigurationManager.AppSettings["ServiceName"]);
                x.SetDisplayName(ConfigurationManager.AppSettings["ServiceName"]);
                x.SetServiceName(ConfigurationManager.AppSettings["ServiceName"]);
            });
        }
    }
}
