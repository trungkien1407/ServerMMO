using Gameserver.core.Dto;
using Gameserver.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Repo
{
    public interface ICharacterRepo
    {
      
      //  Task<Characters> GetById(int id);
        Task<Characters?> GetCharactersByUserID(int userid);
        Task<Characters> Create(Characters character);        
        Task<bool> Update(Characters character);        
        Task<bool> CheckName(string charname);
        Task LoadCharacterDataIntoSession(Session session,int Class);

    }
}
