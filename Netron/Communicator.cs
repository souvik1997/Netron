using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
namespace Netron
{
    enum TronInstruction
    {
        AddToGrid, RemoveFromGrid
    }
    enum TronCommunicatorStatus
    {
        Master, Slave
    }
    static class Communicator
    {
        public TronCommunicatorStatus tcs;
        string[] ipaddrs;
        public void Broadcast(TronType te, TronInstruction instr)
        {
            if (tcs == TronCommunicatorStatus.Slave)
            {
                TcpClient client = new TcpClient(ipaddrs[0], 1337);
                byte[] data = te.
            }

        }
    }
}
