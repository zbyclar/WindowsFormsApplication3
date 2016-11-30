﻿using System;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Security.Policy;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Net.NetworkInformation;

namespace WindowsFormsApplication3
{
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class Form1 : Form
    {
        

        Socket sck,sckVoice;
        soundCapture sc;
        EndPoint epLocal, epRemote;
        EndPoint epVoiceLocal, epVoiceRemote;
        soundCapture vc = new soundCapture();
        public Form1()
        {

            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            
            //set text socket and voice socket
            sck = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sck.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            sckVoice = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sckVoice.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            //get local IP address, will be replaced by real-machine ip address in the future
            GlobalVariable.localIp = GetLocalIP();
            textBox1.Text = GlobalVariable.localIp;
            textBox2.Text = GlobalVariable.localIp;

            //make webrowser navigate to BaiduMap.html
            DirectoryInfo dir = new DirectoryInfo(System.Windows.Forms.Application.StartupPath);
            string str = dir.Parent.Parent.FullName.ToString();
            str.Replace("\\", "/");
            str = str + "\\BaiduMap.html";
            Console.WriteLine(str);
            
            //Open a new thread to listen to the Internet Broadcast
            /*
            ThreadStart threadStart = new ThreadStart(bgdListen);
            listenerThread = new Thread(threadStart);
            listenerThread.Start();
            */

        }

        private void bgdListen()
        {

            GetLocalIP();
            UDPListener listener = new UDPListener();
            listener.StartListener();


        }

        private String GetLocalIP()
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            Console.WriteLine("=======================" + Dns.GetHostName() + "===============================");

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }

            }

            return "127.0.0.1";
        }

        private void MessageCallBack(IAsyncResult aResult)
        {
            try
            {
                int size = sck.EndReceiveFrom(aResult, ref epRemote);

                //there must be an epRemote to get message from
                if (size > 0)
                {
                    byte[] receivedData = new byte[1464];
                    receivedData = (byte[])aResult.AsyncState;
                    ASCIIEncoding eEncoding = new ASCIIEncoding();
                    string receivedMessage = eEncoding.GetString(receivedData);
                    listBox1.Items.Add("Friends: " + receivedMessage);
                }

                byte[] buffer = new byte[1500];
                sck.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref epRemote, new AsyncCallback(MessageCallBack), buffer);

            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.ToString());
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            webBrowser1.ObjectForScripting = this;
            TreeNode t1 = treeView1.Nodes.Add("GPS 联系人");
            TreeNode t2 = new TreeNode("116.307414,39.910573");
            GlobalVariable.gpsAllLocation += t2.ToString().Substring(10) + "|";
            TreeNode t3 = new TreeNode("116.292466,39.98403");
            GlobalVariable.gpsAllLocation += t3.ToString().Substring(10) + "|";
            TreeNode t4 = new TreeNode("116.433896,39.975184");
            GlobalVariable.gpsAllLocation += t4.ToString().Substring(10) + "|";
            t1.Nodes.Add(t2);
            t1.Nodes.Add(t3);
            t1.Nodes.Add(t4);
            
            
            Console.WriteLine(GlobalVariable.gpsAllLocation);




        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            vc.stoprec();
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (webBrowser1.Document != null)
            {
                int length = GlobalVariable.gpsAllLocation.Split('|').Length;

                String[] gpsTotalLocation = new String[2 * length];
                for (int i = 0; i < length; i++)
                {
                    if (GlobalVariable.gpsAllLocation.Split('|')[i] != "")
                    {
                        Console.WriteLine(GlobalVariable.gpsAllLocation.Split('|')[i]);
                        Console.WriteLine(GlobalVariable.gpsAllLocation.Split('|')[i].Split(',')[0]);
                        Console.WriteLine(GlobalVariable.gpsAllLocation.Split('|')[i].Split(',')[1]);
                        gpsTotalLocation[2 * i] = GlobalVariable.gpsAllLocation.Split('|')[i].Split(',')[0];
                        gpsTotalLocation[2 * i + 1] = GlobalVariable.gpsAllLocation.Split('|')[i].Split(',')[1];
                    }

                }
                var gpsJson = JsonConvert.SerializeObject(gpsTotalLocation);
                gpsJson.Trim(' ');
                Console.WriteLine(webBrowser1.Document.GetElementById("allGpsContainer").GetAttribute("value"));
                webBrowser1.Document.GetElementById("allGpsContainer").SetAttribute("value", gpsJson);
                Console.WriteLine(webBrowser1.Document.GetElementById("allGpsContainer").GetAttribute("value"));
                HtmlElement btnAdd = webBrowser1.Document.GetElementById("valueshot");
                btnAdd.InvokeMember("click");
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (treeView1.SelectedNode.Nodes.Count == 0)
            {
                GlobalVariable.gpsPointedLocation = treeView1.SelectedNode.ToString().Substring(10);
                Console.WriteLine(treeView1.SelectedNode.ToString().Substring(10));
                if (webBrowser1.Document != null)
                {
                    webBrowser1.Document.GetElementById("PointedGpsContainer").SetAttribute("value", GlobalVariable.gpsPointedLocation);
                    HtmlElement btnShot = webBrowser1.Document.GetElementById("centreScreen");
                    btnShot.InvokeMember("click");

                }
            }

        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                byte[] msg = new byte[1500];
                msg = enc.GetBytes(textBox6.Text);

                sck.Send(msg);
                listBox1.Items.Add("You: " + textBox6.Text);
                textBox6.Clear();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click_1(object sender, EventArgs e)
        {

            //Send broadcast message through udp protocol, to indicate the specified ip address to communicate.
            /*
            sckBrc = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress broadcast = IPAddress.Parse("192.168.1.255");
            byte[] sendbuf = Encoding.ASCII.GetBytes("192.168.1.60");
            IPEndPoint ep = new IPEndPoint(broadcast, 11001);
            sckBrc.SendTo(sendbuf, ep);
            Console.WriteLine("Message sent to the broadcast address");
            */
            //Set up connection between source ip and destination ip, need to be further amend.

            epLocal = new IPEndPoint(IPAddress.Parse(textBox1.Text), Convert.ToInt32("80"));
        
            sck.Bind(epLocal);
        
            Console.WriteLine("bind local ip successfully...");

            epRemote = new IPEndPoint(IPAddress.Parse(textBox2.Text), Convert.ToInt32("80"));
            
            sck.Connect(epRemote);
            Console.WriteLine("connect remote ip successfully...");
            byte[] buffer = new byte[1500];
            sck.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref epRemote, new AsyncCallback(MessageCallBack), buffer);
            Thread voCon = new Thread(VoiceChat);
            voCon.IsBackground = true;
            voCon.Start();
            //change ui state

            button4.Enabled = false;
            button4.Text = "Connected";
            Submit.Enabled = true;
            textBox6.Focus();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //Initialize
            OpenFileDialog fileDialog = new OpenFileDialog();

            //Justify whether the user choose the file correctly
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                //get the file name, which is the extension of the full path
                string name = Path.GetFileName(fileDialog.FileName);

                //Justify the file size, which should be smaller than 20K
                FileInfo fileInfo = new FileInfo(fileDialog.FileName);
                if (fileInfo.Length > 20480)
                {
                    MessageBox.Show("所选择的文件不能超过20K");
                }
                else
                {
                    //file upload logic here
                    button3.Text = name;

                }
            }
           
        }

        private void listenFile()
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                vc.StartRecord("C:\\Users\\zbyclar\\Desktop\\test1.wav");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }

            //C:\\Users\\zbyclar\\Desktop\\test1.wav
        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {

        }
        
        private void StartReceiveFile()
        {
            //port is 2005 for file transferring
            IPEndPoint epReceiver = new IPEndPoint(IPAddress.Parse(textBox1.Text), int.Parse("2005"));
            IPEndPoint epSender = new IPEndPoint(IPAddress.Parse(textBox2.Text), int.Parse("2005"));

            Socket fileTrans = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            fileTrans.Bind(epReceiver);
            fileTrans.Connect(epSender);
            fileTrans.Listen(10);
        }

        private void VoiceChat()
        {
            epVoiceLocal = new IPEndPoint(IPAddress.Parse(textBox1.Text), Convert.ToInt32("90"));
            epVoiceRemote = new IPEndPoint(IPAddress.Parse(textBox2.Text), Convert.ToInt32("90"));
            sckVoice.Bind(epVoiceLocal);
            sckVoice.Connect(epVoiceRemote);
            vc.sckSetter(sckVoice);
            vc.intPtr = this.Handle;
            vc.CreatePalyDevice();
            vc.CreateSecondaryBuffer();
            byte[] bytData = new byte[999999];
            while (true)
            {
                Console.WriteLine("Keep Listening to port 90...");
                if (sckVoice.Poll(5000, SelectMode.SelectRead))
                {
                    sckVoice.BeginReceiveFrom(bytData, 0, bytData.Length, SocketFlags.None, ref epVoiceRemote, new AsyncCallback(ReceiveData), null);
                    
                }
            }

        }

        private void ReceiveData(IAsyncResult iar)
        {
            int intRecv = 0;
            try
            {
                intRecv = sckVoice.EndReceiveFrom(iar, ref epVoiceRemote);
            }
            catch
            {
                throw new Exception();
            }
            if(intRecv > 0)
            {
                byte[] bytReceivedData = new byte[intRecv];
                Buffer.BlockCopy(bytReceivedData, 0, bytReceivedData, 0, intRecv);
                vc.getVoiceData(intRecv, bytReceivedData);
            }
        }
        
    }
}
