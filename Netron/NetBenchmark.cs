using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ServerFramework.NET;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
namespace Netron
{
    public partial class NetBenchmark : Form
    {
        Server server;
        List<byte> data = new List<byte>();
        int length = 100000;
        Stopwatch swserver = new Stopwatch();
        public NetBenchmark()
        {
            InitializeComponent();
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            server = new Server(1337, new List<char> { (char)0xFF });
            server.OnMessageReceived += new ClientEventHandler(server_OnMessageReceived);
            server.OnClientDisconnect += new ClientEventHandler(server_OnClientDisconnect);
            server.StartAsync();

        }

        void server_OnClientDisconnect(object sender, ClientEventArgs e)
        {
            MessageBox.Show("Done!");
            swserver.Stop();
        }

        void server_OnMessageReceived(object sender, ClientEventArgs e)
        {
            Client c = e.Client;
            foreach(byte b in c.Message)
                data.Add(b);
            if (data.Count >= length)
                server.CloseConnection(c);
        }

        private void button2_Click(object sender, EventArgs e)
        {

            swserver.Start();
            TcpClient client = new TcpClient(textBox1.Text, 1337);
            byte[] data = new byte[length];
            for (int x = 0; x < length; x++) data[x] = (byte)(new Random()).Next(120);
            NetworkStream stream = client.GetStream();
            stream.Write(data, 0, data.Length);
            stream.WriteByte((byte)0xFF);
            MessageBox.Show("Done!2");
            swserver.Stop();
            Console.WriteLine(swserver.ElapsedMilliseconds);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
