using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.DataAccess.Interfaces
{
    public interface IGameTeamDAO
    {
        Task<IEnumerable<GameTeamDAOModel>> GetAll();
    }
}
