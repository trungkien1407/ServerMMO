using Gameserver.core.Models;
using Gameserver.core.Repo.Context;
using Gameserver.core.Repo.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Repo
{
    public class NPCRepo : INPCRepo
    {
        private readonly AppDbContext _context;
        public NPCRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<NPC>> GetNPCByMap(int mapId)
        {
            return await _context.NPC.Where(npc => npc.MapID == mapId).ToListAsync();
        }
    }
}
