using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Netron
{
    interface FiniteLifetimeable
    {
        public void DecreaseLifetime();
        public void TryToDie();
    }
}
