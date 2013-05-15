using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
namespace Netron
{
    abstract class Remoteable
    {
        TcpClient cl = new TcpClient();
    }
}
