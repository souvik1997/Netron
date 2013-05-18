using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
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
            var player = new Netron.Player(0);
            player.PutSelfInGrid(gr, 2, 3);
            player.Direction = TronBase.DirectionType.East;
            player.Act();
            Assert.IsTrue(player.XPos == 3);
        }
        [TestMethod]
        public void TestAdditionToGrid()
        {
            var gr = new Netron.Grid(32, 32);
            var player = new Netron.Player(0);
            player.PutSelfInGrid(gr, 2, 3);
            Assert.IsTrue(player.XPos == 2);
        }
        [TestMethod]
        public void TestWallDropping()
        {
            var gr = new Netron.Grid(32, 32);
            var player = new Netron.Player(0);
            player.PutSelfInGrid(gr, 2, 3);
            player.Direction = TronBase.DirectionType.East;
            player.Act();
            Assert.IsTrue(gr.Get(2,3).GetTronType() == TronType.Wall);
        }
        [TestMethod]
        public void TestPlayerTurning()
        {
            var gr = new Netron.Grid(3, 3);
            var player = new Netron.Player(0);
            player.PutSelfInGrid(gr, 2, 2);
            player.Direction = TronBase.DirectionType.East;
            player.Act();
            Assert.IsTrue(player.Direction == TronBase.DirectionType.South);
        }
        [TestMethod]
        public void TestPacketCreationAndDecryptionAndDeserialization()
        {
            var player = new Player(0);
            var gr = new Grid(5, 5);
            var comm = new Communicator(gr);
            player.PutSelfInGrid(gr, 2, 2);
            player.Direction = TronBase.DirectionType.South;
            var data = comm.GeneratePacket(player, TronInstruction.AddToGrid, 2, 2);
            var newplayer = Player.Deserialize(data.Split(Communicator.Separator)[4]);
            Assert.IsTrue(newplayer.Equals(player));
        }
    }
}
