using Microsoft.Extensions.DependencyInjection;
using TecmoTourney.DataAccess.Interfaces;

namespace TecmoTourney.DataAccess
{
    public static class DataAccessDependencyInjection
    {
        public static void AddDataAccessServices(this IServiceCollection services)
        {
            services.AddScoped<IGameResultDAO, GameResultDAO>();
            services.AddScoped<ITournamentsDAO, TournamentsDAO>();
            services.AddScoped<IPlayerDAO, PlayerDAO>();
        }
    }
}
