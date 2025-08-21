using Gameserver.core.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Repo.Context
{
    public class SessionContext
    {
        private readonly SessionManager _sessionManager;

        public SessionContext(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public int? GetUserId(Guid clientId)
        {
            return _sessionManager.GetUserIdByClientId(clientId);
        }


        public bool IsAuthenticated(Guid clientId)
        {
            return GetUserId(clientId) != null;
        }
    }

}
