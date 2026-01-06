using Microsoft.Extensions.DependencyInjection;
using MRVA.Reports.Data.Services;

namespace MRVA.Reports.Data.Extensions;

public static class ServiceCollectionExtension
{

    public static IServiceCollection AddReportData(this IServiceCollection services)
    {
        services.AddSingleton<WeatherStore>();
        
        return services;
    }
    
}