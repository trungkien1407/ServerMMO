using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Dto
{
    public class ServerSettings
    {
        public string? Host { get; set; }
        public int Port { get; set; }
        public int ExpRate { get; set; }   // <- mới thêm
        public int GoldRate { get; set; }  // <- ví dụ thêm nữa
    }
}
