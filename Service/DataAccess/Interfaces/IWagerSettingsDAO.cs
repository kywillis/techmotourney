using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.DataAccess.Interfaces
{
    public interface IWagerSettingsDAO
    {
        Task<WagerSettingsDAOModel> GetAsync();
        Task<bool> UpdateAsync(WagerSettingsDAOModel settings);
    }
}
