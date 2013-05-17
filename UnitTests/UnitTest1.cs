using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netron;
using System.Drawing;
namespace UnitTests
{
    [TestClass]
    public class UnitTest1
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
            Assert.IsTrue(gr.Get(2, 3).GetTronType() == TronType.Wall);
        }
        [TestMethod]
        public void TestPlayerTurning()
        {
            var gr = new Netron.Grid(3, 3);
            var player = new Netron.Player();
            player.PutSelfInGrid(gr, 2, 2);
            player.Direction = TronBase.DirectionType.East;
            player.Act();
            Assert.IsTrue(player.Direction == TronBase.DirectionType.South);
        }
        [TestMethod]
        public void TestPacketCreation()
        {
            var gr = new Grid(32, 32);
            var player = new Player();
            var comm = new Communicator(gr);
            player.Color = Color.Aqua;
            player.PutSelfInGrid(gr, 5, 5);
            var instr = TronInstruction.AddToGrid;
            Console.WriteLine(comm.GeneratePacket(player, instr, 7, 8));
            Assert.IsTrue(true);
        }
    }
}
