using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netron;
namespace UnitTests
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void TestPlayerMovement()
        {
            var gr = new Netron.Grid(32, 32);
            var player = new Netron.Player();
            player.PutSelfInGrid(gr, 2, 3);
            player.Direction = TronBase.DirectionType.East;
            player.Act();
            Assert.IsTrue(player.XPos == 3);
        }
        [TestMethod]
        public void TestAdditionToGrid()
        {
            var gr = new Netron.Grid(32, 32);
            var player = new Netron.Player();
            player.PutSelfInGrid(gr, 2, 3);
            Assert.IsTrue(player.XPos == 2);
        }
        [TestMethod]
        public void TestWallDropping()
        {
            var gr = new Netron.Grid(32, 32);
            var player = new Netron.Player();
            player.PutSelfInGrid(gr, 2, 3);
            player.Direction = TronBase.DirectionType.East;
            player.Act();
            Assert.IsTrue(gr.Get(2,3).GetTronType() == TronType.Wall);
        }
        
    }
}
