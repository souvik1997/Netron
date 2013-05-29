using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netron;

namespace UnitTests
{
    [TestClass]
    public class UnitTests
    {
        public UnitTests()
        {
            Program.Log = new Log();
        }

        [TestMethod]
        public void TestPlayerMovement()
        {
            var gr = new Grid(32, 32);
            var player = new Player(0);
            player.PutSelfInGrid(gr, 2, 3);
            player.Direction = TronBase.DirectionType.East;
            player.Act();
            Assert.IsTrue(player.XPos == 3);
        }

        [TestMethod]
        public void TestAdditionToGrid()
        {
            var gr = new Grid(32, 32);
            var player = new Player(0);
            player.PutSelfInGrid(gr, 2, 3);
            Assert.IsTrue(player.XPos == 2);
        }

        [TestMethod]
        public void TestWallDropping()
        {
            var gr = new Grid(32, 32);
            var player = new Player(0);
            player.PutSelfInGrid(gr, 2, 3);
            player.Direction = TronBase.DirectionType.East;
            player.Act();
            Assert.IsTrue(gr.Get(2, 3).GetTronType() == TronType.Wall);
        }

        [TestMethod]
        public void TestPacketCreationAndDecryptionAndDeserialization()
        {
            var player = new Player(0);
            var gr = new Grid(5, 5);
            var comm = new Communicator(gr);
            player.PutSelfInGrid(gr, 2, 2);
            player.Direction = TronBase.DirectionType.South;
            string data = comm.GeneratePacket(player, TronInstruction.AddToGrid, 2, 2);
            Player newplayer = Player.Deserialize(data.Split(Communicator.Separator)[4]);
            Assert.IsTrue(newplayer.Equals(player));
        }
    }
}