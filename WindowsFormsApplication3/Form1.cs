using System;
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
using Oraycn.MPlayer;
using Oraycn.MCapture;
using System.Drawing;

namespace WindowsFormsApplication3
{
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class Form1 : Form
    {
        Socket sckText,sckVoice,sckFile,sckVideo;
        //soundCapture sc;
        EndPoint epLocal, epRemote;
        EndPoint epVoiceLocal, epVoiceRemote;
        EndPoint epFileLocal, epFileRemote;
        EndPoint epVideoLocal, epVideoRemote;
        soundCapture vc = new soundCapture();
        private IAudioPlayer audioPlayer;
        private IMicrophoneCapturer microphoneCapturer;
        private ICameraCapturer cameraCapturer;
        bool flag = true;

        public Form1()
        {
            
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            //set text socket and voice socket
            sckText = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sckText.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            sckVoice = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sckVoice.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            sckVideo = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sckVideo.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            
            sckFile = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sckFile.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

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
            //capture image from camera device
            this.cameraCapturer = CapturerFactory.CreateCameraCapturer(0, new Size(int.Parse("320"), int.Parse("240")), 20);
            this.cameraCapturer.ImageCaptured += new ESBasic.CbGeneric<Bitmap>(ImageCaptured);
            this.cameraCapturer.Start();

            //At the same time, capture voice from microphone device
            this.microphoneCapturer = CapturerFactory.CreateMicrophoneCapturer(int.Parse("0"));
            this.microphoneCapturer.AudioCaptured += new ESBasic.CbGeneric<byte[]>(microphoneCapturer_AudioCaptured);
            this.microphoneCapturer.Start();

            flag = false;
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

                sckText.Send(msg);
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


            epLocal = new IPEndPoint(IPAddress.Parse(textBox1.Text), Convert.ToInt32("80"));
        
            sckText.Bind(epLocal);
        
            Console.WriteLine("bind local ip successfully...");

            epRemote = new IPEndPoint(IPAddress.Parse(textBox2.Text), Convert.ToInt32("80"));
            
            sckText.Connect(epRemote);
            Console.WriteLine("connect remote ip successfully...");
            byte[] buffer = new byte[1500];
            sckText.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref epRemote, new AsyncCallback(MessageCallBack), buffer);
            Thread voCon = new Thread(VoiceChat);
            voCon.IsBackground = true;
            voCon.Start();
            Thread viCon = new Thread(VideoChat);
            viCon.IsBackground = true;
            viCon.Start();
            //change ui state
            Thread fileTrans = new Thread(listenFile);
            fileTrans.IsBackground = true;
            fileTrans.Start();
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
                FileStream file = fileInfo.OpenRead();
                if (fileInfo.Length > 4194304)
                {
                    MessageBox.Show("所选择的文件不能超过4M");
                }
                else
                {
                    //file upload logic here
                    button3.Text = name;
                    StartSendFile(file);

                }
            }
           
        }


        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                //vc.StartRecord("C:\\Users\\zbyclar\\Desktop\\test1.wav");
                this.microphoneCapturer = CapturerFactory.CreateMicrophoneCapturer(int.Parse("0"));
                this.microphoneCapturer.Start();
                this.microphoneCapturer.AudioCaptured += new ESBasic.CbGeneric<byte[]>(microphoneCapturer_AudioCaptured);
                flag = false;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void listenFile()
        {
            epFileLocal = new IPEndPoint(IPAddress.Parse(textBox1.Text), Convert.ToInt32("100"));
            epFileRemote = new IPEndPoint(IPAddress.Parse(textBox2.Text), Convert.ToInt32("100"));
            sckFile.Bind(epFileLocal);
            sckFile.Connect(epFileRemote);
            while (true)
            {
                
                if (sckFile.Poll(5000, SelectMode.SelectRead))
                { 

                    string sendFileName = System.Text.Encoding.Unicode.GetString(ReceiveVarData(sckFile));
                    string bagSize = System.Text.Encoding.Unicode.GetString(ReceiveVarData(sckFile));
                    int bagCount = int.Parse(System.Text.Encoding.Unicode.GetString(ReceiveVarData(sckFile)));
                    string bagLast = System.Text.Encoding.Unicode.GetString(ReceiveVarData(sckFile));
                    FileStream myFile = new FileStream(sendFileName, FileMode.Create, FileAccess.Write);
                    int sendedCount = 0;
                    while (true)
                    {
                        byte[] data = ReceiveVarData(sckFile);
                        if (data.Length == 0)
                            break;
                        else
                        {
                            sendedCount++;
                            myFile.Write(data, 0, data.Length);
                            Console.Write("keep writing data into the file!");
                        }

                    }
                    //byte[] fileRecv = new byte[4194304];
                    myFile.Close();
                    MessageBox.Show("文件接收完毕!");
                }
            }
        }

        /*
        private void ReceiveFile(IAsyncResult iar)
        {
            int recSize = sckFile.EndReceiveFrom(iar, ref epFileRemote);
            if (recSize > 0)
            {
                byte[] receivedByte = new byte[4194304];
                receivedByte = (byte[])iar.AsyncState;
            }
        }
        */
        private void microphoneCapturer_AudioCaptured(byte[] audioData)
        {
            sckVoice.Send(audioData);
            //Console.WriteLine("Keep sending voice data");
        }

        private void ImageCaptured(Bitmap img)
        {
            byte[] imgByt = bitmapToByte(img);
            Console.WriteLine(imgByt.Length);
            sckVideo.SendTo(imgByt,epVideoRemote);
            Console.WriteLine("Keep sending image data");
        }

        private void VoiceChat()
        {
            epVoiceLocal = new IPEndPoint(IPAddress.Parse(textBox1.Text), Convert.ToInt32("90"));
            epVoiceRemote = new IPEndPoint(IPAddress.Parse(textBox2.Text), Convert.ToInt32("90"));
            sckVoice.Bind(epVoiceLocal);
            sckVoice.Connect(epVoiceRemote);
            /*
            vc.sckSetter(sckVoice);
            vc.intPtr = this.Handle;
            vc.CreatePalyDevice();
            vc.CreateSecondaryBuffer();
            */
            this.audioPlayer = PlayerFactory.CreateAudioPlayer(int.Parse("0"), 16000, 1, 16, 2);
            byte[] bytData = new byte[999999];
            while (true)
            {
                
                if (sckVoice.Poll(5000, SelectMode.SelectRead))
                {
                    if (flag == true)
                    {
                        //vc.StartRecord("C:\\Users\\zbyclar\\Desktop\\test2.wav");
                        this.microphoneCapturer = CapturerFactory.CreateMicrophoneCapturer(int.Parse("0"));
                        this.microphoneCapturer.Start();
                        this.microphoneCapturer.AudioCaptured += new ESBasic.CbGeneric<byte[]>(microphoneCapturer_AudioCaptured);
                        flag = false;
                    }
                    //Console.WriteLine("There are voices !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    sckVoice.BeginReceiveFrom(bytData, 0, bytData.Length, SocketFlags.None, ref epVoiceRemote, new AsyncCallback(ReceiveData), null);
                    
                }
            }

        }

        private void VideoChat()
        {
            epVideoLocal = new IPEndPoint(IPAddress.Parse(textBox1.Text), Convert.ToInt32("110"));
            epVideoRemote = new IPEndPoint(IPAddress.Parse(textBox2.Text), Convert.ToInt32("110"));
            sckVideo.Bind(epVideoLocal);
            sckVideo.Connect(epVideoRemote);
            
           
            while (true)
            {
                
                if (sckVideo.Poll(5000, SelectMode.SelectRead))
                {
                    if (flag == true)
                    {
                        //capture image from camera device
                        this.cameraCapturer = CapturerFactory.CreateCameraCapturer(0, new Size(int.Parse("320"), int.Parse("240")), 20);
                        this.cameraCapturer.ImageCaptured += new ESBasic.CbGeneric<Bitmap>(ImageCaptured);
                        this.cameraCapturer.Start();

                        //At the same time, capture voice from microphone device
                        this.microphoneCapturer = CapturerFactory.CreateMicrophoneCapturer(int.Parse("0"));
                        this.microphoneCapturer.AudioCaptured += new ESBasic.CbGeneric<byte[]>(microphoneCapturer_AudioCaptured);
                        this.microphoneCapturer.Start();

                        flag = false;
                    }
                    Console.WriteLine("There are both images and voice !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    
                    MemoryStream ms = new MemoryStream();
                    int size = 0;

                    while(size< 230454)
                    {
                        byte[] img = new byte[230454];
                        int r = sckVideo.Receive(img, img.Length, SocketFlags.None);
                        size = size + r;
                        Console.WriteLine("This time, there are " + r + " datagrams" +"\n"+"And in total "+size+" have been received");
                        ms.Write(img, 0, r);
                    }
                    if(size == 230454)
                        Form1_ImageCaptured(StreamToBitmap(ms));
                    
                }
            }

        }
        /*
        private void ReceiveImage(IAsyncResult iar)
        {
            int intRecv = 0;
            try
            {
                intRecv = sckVideo.EndReceiveFrom(iar, ref epVideoRemote);
            }
            catch
            {
                throw new Exception();
            }
            if(intRecv > 0)
            {
                byte[] byteReceivedImage = new byte[intRecv];
                Bitmap imageBitmap = byteToBitmap(byteReceivedImage);
                Form1_ImageCaptured(imageBitmap);
            }
        }
        */
        private void Form1_ImageCaptured(Bitmap img)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new ESBasic.CbGeneric<Bitmap>(this.Form1_ImageCaptured), img);
            }
            else
            {


                Bitmap old = (Bitmap)this.panel3.BackgroundImage;
                this.panel3.Dispose();
                this.panel3 = new System.Windows.Forms.PictureBox();
                this.panel3.BackgroundImage = img;
                if (old != null)
                {
                    //立即释放不再使用的视频帧
                    
                    old.Dispose();
                      
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
                //Buffer.BlockCopy(bytReceivedData, 0, bytReceivedData, 0, intRecv);
                //vc.getVoiceData(intRecv, bytReceivedData

                //Console.WriteLine("There are voices !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                this.audioPlayer.Play(bytReceivedData);
            }
        }

        private void bgdListen()
        {

            GetLocalIP();
            UDPListener listener = new UDPListener();
            listener.StartListener();

        }

        private void button5_Click(object sender, EventArgs e)
        {
            sckVideo.Disconnect(true);
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
                int size = sckText.EndReceiveFrom(aResult, ref epRemote);

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
                sckText.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref epRemote, new AsyncCallback(MessageCallBack), buffer);
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.ToString());
            }
        }

        public static byte[] ReceiveVarData(Socket s)
        {  
            int total = 0;  
            int recv;  
            byte[] datasize = new byte[4];  
            recv = s.Receive(datasize, 0, 4, SocketFlags.None);  
            int size = BitConverter.ToInt32(datasize, 0);  
            int dataleft = size;  
            byte[] data = new byte[size];  
            while (total<size)  
            {  
                recv = s.Receive(data, total, dataleft, SocketFlags.None);  
                if (recv == 0)  
                {  
                    data = null;  
                    break;  
                }  
                total += recv;  
                dataleft -= recv;  
            }  
            return data;  
        }  

        private byte[] bitmapToByte(Bitmap btmp)
        {
            MemoryStream ms = new MemoryStream();
            btmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            byte[] bytes = ms.GetBuffer();
            ms.Close();
            return bytes;
        }

        private Bitmap StreamToBitmap(MemoryStream mem)
        {
            Console.WriteLine("The receiving buffer length is "+mem.Length);

            Bitmap bm = (Bitmap)Image.FromStream(mem);
            mem.Close();
            return bm;
        }

        private void StartSendFile(FileStream file)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            int packetSize = 10240;
            int packetCount = (int)(file.Length / (long)packetSize);
            int lastDataPacket = (int)(file.Length - (long)(packetSize * packetCount));
            byte[] fileNameByte = enc.GetBytes(file.Name.ToString());
            byte[] packetSizeByte = enc.GetBytes(packetSize.ToString());
            byte[] packetCountByte = enc.GetBytes(packetCount.ToString());
            byte[] lastDataPacketByte = enc.GetBytes(lastDataPacket.ToString());
            sckFile.Send(fileNameByte);
            Console.Write("write file name");
            sckFile.Send(packetSizeByte);
            Console.Write("set packet Size");
            sckFile.Send(packetCountByte);
            Console.Write("set packet number");
            
            byte[] data = new byte[packetSize];
            for(int i = 0; i < packetCount; i++)
            {
                file.Read(data, 0, data.Length);
                sckFile.Send(data);
            }
            if(lastDataPacket != 0)
            {
                data = new byte[lastDataPacket];
                file.Read(data, 0, data.Length);
                sckFile.Send(data);
            }
        }

    }
}
