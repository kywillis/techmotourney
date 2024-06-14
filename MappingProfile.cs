using AutoMapper;
using TecmoTourney.Models;
using TecmoTourney.DataAccess.Models;

namespace TecmoTourney
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<TournamentDAOModel, TournamentModel>();
        }
    }
}
