using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ServerFramework.NET;
using System.Net.Sockets;
using System.Diagnostics;
namespace Netron
{
    public partial class NetBenchmark : Form
    {
        Server _server;
        readonly List<byte> _data = new List<byte>();
        private const int Length = 100000;
        readonly Stopwatch _swserver = new Stopwatch();
        public NetBenchmark()
        {
            InitializeComponent();
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            _server = new Server(1337, new List<char> { (char)0xFF });
            _server.OnMessageReceived += server_OnMessageReceived;
            _server.OnClientDisconnect += server_OnClientDisconnect;
            _server.StartAsync();

        }

        void server_OnClientDisconnect(object sender, ClientEventArgs e)
        {
            MessageBox.Show("Done!");
            _swserver.Stop();
        }

        void server_OnMessageReceived(object sender, ClientEventArgs e)
        {
            Client c = e.Client;
            foreach(byte b in c.Message)
                _data.Add(b);
            if (_data.Count >= Length)
                _server.CloseConnection(c);
        }

        private void button2_Click(object sender, EventArgs e)
        {

            _swserver.Start();
            TcpClient client = new TcpClient(textBox1.Text, 1337);
            byte[] data = new byte[Length];
            for (int x = 0; x < Length; x++) data[x] = (byte)(new Random()).Next(120);
            NetworkStream stream = client.GetStream();
            stream.Write(data, 0, data.Length);
            stream.WriteByte(0xFF);
            MessageBox.Show("Done!2");
            _swserver.Stop();
            
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
