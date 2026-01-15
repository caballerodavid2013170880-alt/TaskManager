// 13 ene: se especifica en un archivo a parte la especificación para no saturar el "Program.cs" (es decir, es un método de extensión, segmentando las llamadas que se hacen desde el program.cs)
using TaskManager.Interfaces.Tasks;

namespace TaskManager.Utilities.Configurations
{
    public static class ServiceConfiguration
    {

        public static void AddServices(this IServiceCollection services)
        {
            services.AddScoped<ITaskService, TaskService>();
        }
    }
}