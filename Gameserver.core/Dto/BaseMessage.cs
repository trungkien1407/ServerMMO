using Newtonsoft.Json.Linq;
using System;

namespace Gameserver.core.Dto
{
    public class BaseMessage
    {
        public string Action { get; set; }
        public JObject Data { get; set; }
    }
}