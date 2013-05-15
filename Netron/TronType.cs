using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Netron
{
    interface TronType
    {
        public int GetID();
        public int GetState();
        public byte[] Serialize();
    }
    
}
