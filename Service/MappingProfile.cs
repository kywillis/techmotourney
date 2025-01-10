using AutoMapper;
using TecmoTourney.Models;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.Models.Requests;
using Newtonsoft.Json;

namespace TecmoTourney
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<GameTeamDAOModel, GameTeamModel>();
            CreateMap<CreatePlayerRequestModel, PlayerDAOModel>();
            CreateMap<TournamentDAOModel, TournamentModel>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ((TournamentStatus)src.StatusId).ToString()))
                .ForMember(dest => dest.TournamentBracket, opt => opt.MapFrom(src => JsonConvert.DeserializeObject<TournamentBracketModel>(src.BracketData)));
            CreateMap<TournamentModel, TournamentDAOModel>()
                .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => (int)src.Status))
                .ForMember(dest => dest.BracketData, opt => opt.MapFrom(src => JsonConvert.SerializeObject(src.TournamentBracket)));
            CreateMap<PlayerDAOModel, PlayerModel>().ReverseMap();

            CreateMap<SaveGameResultRequestModel, GameResultDAOModel>()
             .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => (int)src.Status))
             .ForMember(dest => dest.GameTypeId, opt => opt.MapFrom(src => (int)src.GameType))
             .MapGameResultStatsModelToDAO();

            CreateMap<GameResultDAOModel, GameResultModel>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ((GameStatus)src.StatusId).ToString()))
                .ForMember(dest => dest.GameType, opt => opt.MapFrom(src => ((GameType)src.GameTypeId).ToString()))
                .MapDAOToGameResultStatsModel();

            CreateMap<GameResultModel, GameResultDAOModel>()
                .ForMember(dest => dest.TournamentId, opt => opt.MapFrom(src => src.TournamentId))
                .ForMember(dest => dest.Player1Id, opt => opt.MapFrom(src => src.Player1.PlayerId))
                .ForMember(dest => dest.Player2Id, opt => opt.MapFrom(src => src.Player2.PlayerId))
                .ForMember(dest => dest.Player1Score, opt => opt.MapFrom(src => src.Player1.Score))
                .ForMember(dest => dest.Player2Score, opt => opt.MapFrom(src => src.Player2.Score))
                .ForMember(dest => dest.Player1PassingYards, opt => opt.MapFrom(src => src.Player1.PassingYards))
                .ForMember(dest => dest.Player2PassingYards, opt => opt.MapFrom(src => src.Player2.PassingYards))
                .ForMember(dest => dest.Player1RushingYards, opt => opt.MapFrom(src => src.Player1.RushingYards))
                .ForMember(dest => dest.Player2RushingYards, opt => opt.MapFrom(src => src.Player2.RushingYards))
                .ForMember(dest => dest.Player1GameTeamID, opt => opt.MapFrom(src => (int?)src.Player1.GameTeamId))
                .ForMember(dest => dest.Player2GameTeamID, opt => opt.MapFrom(src => (int?)src.Player2.GameTeamId))
                .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => (int)src.Status))
                .ForMember(dest => dest.GameTypeId, opt => opt.MapFrom(src => (int)src.GameType));
        }
    }
}
