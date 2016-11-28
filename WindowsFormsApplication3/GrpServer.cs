using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApplication3
{
    class GrpServer
    {
        Socket connection = null;
        Thread threadwatch = null;
        Socket socketwatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        Dictionary<string, Socket> dic = new Dictionary<string, Socket> { };
        Dictionary<string, int> ipPortDic = new Dictionary<string, int> { };
        Dictionary<string, string> ipipDic = new Dictionary<string, string> { };
        IPAddress serverAddress = IPAddress.Parse(GlobalVariable.localIp.Trim());

        public GrpServer()
        {
            IPEndPoint point = new IPEndPoint(serverAddress, int.Parse("11000"));
            socketwatch.Bind(point);
            socketwatch.Listen(20);
            threadwatch = new Thread(watchconnecting);
            threadwatch.IsBackground = true;
            threadwatch.Start();
        }

        public void watchconnecting()
        {
            while (true)
            {
                try
                {
                    connection = socketwatch.Accept();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    break;
                }

                IPAddress clientIP = (connection.RemoteEndPoint as IPEndPoint).Address;
                int clientPort = (connection.RemoteEndPoint as IPEndPoint).Port;
                string clientipPort = connection.RemoteEndPoint.ToString();

                string sendmsg = "Successfully connect to \r\n" +"Local IP: "+ clientIP +", Local Port: " +clientPort.ToString();
                byte[] arrSendMsg = Encoding.UTF8.GetBytes(sendmsg);
                connection.Send(arrSendMsg);

                Console.WriteLine(sendmsg);

                dic.Add(clientIP.ToString(), connection);
                ipPortDic.Add(clientIP.ToString(), clientPort);
                ipipDic.Add(clientIP.ToString(), null);
                ParameterizedThreadStart pts = new ParameterizedThreadStart(recv);
                Thread thread = new Thread(pts);
                thread.IsBackground = true;
                thread.Start(connection);

            }
        }

        private void recv(object socketclientpara)
        {
            Socket socketServer = socketclientpara as Socket;

            while (true)
            {
                byte[] arrServerRecMsg = new byte[1024 * 1024];

                try
                {
                    int length = socketServer.Receive(arrServerRecMsg);
                    string strSRecMsg = Encoding.UTF8.GetString(arrServerRecMsg, 0, length);
                    Console.WriteLine(strSRecMsg);
                    if (strSRecMsg != null)
                    {
                        string senderip = (socketServer.RemoteEndPoint as IPEndPoint).Address.ToString();
                        string receiverip = strSRecMsg;
                        if (strSRecMsg == "no connection request")
                        {
                            this.ipipDic[senderip] = null;
                        }else
                        {
                            this.ipipDic[senderip] = receiverip;
                            
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                    break;
                }
            }
        }

        private void send(Socket client, Dictionary<string,int> msg)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(msg.ToString());
            client.Send(bytes);

        }


    }
}
