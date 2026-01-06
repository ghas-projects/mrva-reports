using MRVA.Reports.Data.Helpers;

namespace MRVA.Reports.Data.Services;

public class WeatherStore
{

    private readonly List<WeatherForecast> _forecasts;
    
    public WeatherStore()
    {
        _forecasts = ResourceHelper.GetResource<List<WeatherForecast>>("weather.json") ?? [];
    }
    
    public List<WeatherForecast> GetForecastList()
    {
        return _forecasts;
    }
    
}