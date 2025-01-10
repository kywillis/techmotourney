using AutoMapper;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;

namespace TecmoTourney
{
    public static class MappingExtensions
    {
        public static IMappingExpression<TSource, GameResultDAOModel> MapGameResultStatsModelToDAO<TSource>(
            this IMappingExpression<TSource, GameResultDAOModel> mapping)
            where TSource : SaveGameResultRequestModel
        {
            return mapping
                .ForMember(dest => dest.Player1Id, opt => opt.MapFrom(src => src.Player1.PlayerId))
                .ForMember(dest => dest.Player2Id, opt => opt.MapFrom(src => src.Player2.PlayerId))
                .ForMember(dest => dest.Player1Score, opt => opt.MapFrom(src => src.Player1.Score))
                .ForMember(dest => dest.Player2Score, opt => opt.MapFrom(src => src.Player2.Score))
                .ForMember(dest => dest.Player1PassingYards, opt => opt.MapFrom(src => src.Player1.PassingYards))
                .ForMember(dest => dest.Player2PassingYards, opt => opt.MapFrom(src => src.Player2.PassingYards))
                .ForMember(dest => dest.Player1RushingYards, opt => opt.MapFrom(src => src.Player1.RushingYards))
                .ForMember(dest => dest.Player2RushingYards, opt => opt.MapFrom(src => src.Player2.RushingYards))
                .ForMember(dest => dest.Player1GameTeamID, opt => opt.MapFrom(src => src.Player1.GameTeamId))
                .ForMember(dest => dest.Player2GameTeamID, opt => opt.MapFrom(src => src.Player2.GameTeamId));
        }

        public static IMappingExpression<GameResultDAOModel, TDestination> MapDAOToGameResultStatsModel<TDestination>(
            this IMappingExpression<GameResultDAOModel, TDestination> mapping)
            where TDestination : GameResultModel
        {
            return mapping
                .ForMember(dest => dest.Player1, opt => opt.MapFrom(src => new GameResultStatsModel
                {
                    BracketGameId = src.BracketGameId,
                    PlayerId = src.Player1Id,
                    Score = src.Player1Score,
                    PassingYards = src.Player1PassingYards,
                    RushingYards = src.Player1RushingYards,
                    GameTeamId = src.Player1GameTeamID ?? 0
                }))
                .ForMember(dest => dest.Player2, opt => opt.MapFrom(src => new GameResultStatsModel
                {
                    BracketGameId = src.BracketGameId,
                    PlayerId = src.Player2Id,
                    Score = src.Player2Score,
                    PassingYards = src.Player2PassingYards,
                    RushingYards = src.Player2RushingYards,
                    GameTeamId = src.Player2GameTeamID ?? 0
                }));
        }
    }
}
