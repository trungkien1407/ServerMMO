using Gameserver.core.Manager;
using Gameserver.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Repo.Interfaces
{
    public interface IItemDataRepo
    {
        Task<List<CachedItemData>> GetItemDataAsync();
     
    }

}
