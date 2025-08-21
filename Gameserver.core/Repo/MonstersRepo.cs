using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameserver.core.Models;
using Gameserver.core.Repo.Context;
using Gameserver.core.Repo.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Gameserver.core.Repo
{
    public class MonsterRepo : IMonsterRepo
    {
        private readonly AppDbContext _context;

        public MonsterRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Monsterspawn>> GetMonsterSpawnsByMapIdAsync(int mapId)
        {
            return await _context.Monsterspawns
                .Include(spawn => spawn.Monster) // Load thông tin quái
                .Where(spawn => spawn.MapID == mapId)
                .ToListAsync();
        }
    }
}
