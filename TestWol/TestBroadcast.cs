using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestWol
{
    [TestClass]
    public class TestBroadcast
    {
        [TestMethod]
        public void Slash24()
        {
            Assert.AreEqual(
                                      new IPAddress(new byte[] { 10, 0, 0, 255 }),
                wol.NetMisc.Broadcast(new IPAddress(new byte[] { 10, 0, 0,   0 }), 24));
        }
        [TestMethod]
        public void Slash25()
        {
            Assert.AreEqual(
                                      new IPAddress(new byte[] { 10, 0, 0, 127 }),
                wol.NetMisc.Broadcast(new IPAddress(new byte[] { 10, 0, 0, 0 }), 25));
        }
        [TestMethod]
        public void Slash23()
        {
            Assert.AreEqual(
                                      new IPAddress(new byte[] { 10, 0, 1, 255 }),
                wol.NetMisc.Broadcast(new IPAddress(new byte[] { 10, 0, 0, 0 }), 23));
        }
        [TestMethod]
        public void Slash16()
        {
            Assert.AreEqual(
                                      new IPAddress(new byte[] { 10, 0, 255, 255 }),
                wol.NetMisc.Broadcast(new IPAddress(new byte[] { 10, 0, 0, 0 }), 16));
        }
    }
}
