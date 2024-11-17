using Microsoft.Extensions.DependencyInjection;

namespace OnairConv1.Helpers
{
    public static class AddExtension
    {
        public static IServiceCollection AddOnairConv1(this IServiceCollection services)
        {
            //NextService: services.AddSingleton<$ClassName$>();
            return services;
        }
    }
}