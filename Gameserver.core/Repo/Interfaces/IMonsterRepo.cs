using System.Collections.Generic;
using System.Threading.Tasks;
using Gameserver.core.Models;

namespace Gameserver.core.Repo.Interfaces
{
    public interface IMonsterRepo
    {
        /// <summary>
        /// Lấy danh sách các vị trí spawn quái theo map, bao gồm thông tin quái
        /// </summary>
        Task<List<Monsterspawn>> GetMonsterSpawnsByMapIdAsync(int mapId);
    }
}
