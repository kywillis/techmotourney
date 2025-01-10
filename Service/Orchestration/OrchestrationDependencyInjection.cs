using Microsoft.Extensions.DependencyInjection;
using TecmoTourney.Orchestration.Interfaces;

namespace TecmoTourney.Orchestration
{
    public static class OrchestrationDependencyInjection
    {
        public static void AddOrchestrationServices(this IServiceCollection services)
        {
            services.AddScoped<IPlayerOrchestration, PlayerOrchestration>();
            services.AddScoped<IGameResultOrchestration, GameResultOrchestration>();
            services.AddScoped<ITournamentsOrchestration, TournamentsOrchestration>();
            services.AddScoped<IGameTeamOrchestration, GameTeamsOrchestration>();
        }
    }
}
