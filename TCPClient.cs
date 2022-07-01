#define USE_THREAD
#define USE_SERVICE_HAS_BUFFER

using CBTPTempMoistMoni.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CBTPTempMoistMoni.Server
{
    public class TCPClient
    {
        public enum State { None, Connecting, Connected, Starting, Started, Running, Stoping, Stoped, Closing, Closed }
        public static State state { get; set; }

        string name = "";
        string address = "127.0.0.1";
        int port = 4100;
        Socket socket = null;
        ILogger Logger = null;


        public TCPClient(string name = "anonymous")
        {
            this.name = name;
        }

        public void setLogger(ILogger logger)
        {
            Logger = logger;
        }



        #region Connection .....

        public const int BUFFER_SIZE = 10 * 1024 * 1024;
        byte[] recBuffer = new byte[0];
        int parseErrorCount = 0;

#if USE_THREAD
        ConcurrentQueue<byte[]> queue = new ConcurrentQueue<byte[]>();
        byte[] tempBuffer = null;
        ManagedThread thread = null;
#endif

        void startWorker()
        {
            state = State.Starting;

#if USE_THREAD
            thread = new ManagedThread("TH_" + name, onWorking);
            thread.Start();
#endif
            state = State.Started;
        }


#if USE_THREAD
        bool onWorking(UInt64 calledCount)
        {
            state = State.Running;

            byte[] temp = null;
            for (int i = 0; queue.IsEmpty == false && i < 20; i++)
            {
                if (queue.TryDequeue(out temp))
                {
                    byte[] abytes = new byte[recBuffer.Length + temp.Length];
                    Array.Copy(recBuffer, 0, abytes, 0, recBuffer.Length);
                    Array.Copy(temp, 0, abytes, recBuffer.Length, temp.Length);
                    recBuffer = abytes;
                }
            }

            if (recBuffer.Length > 0)
            {
                Logger.info("OnReceive : " + Utils.ByteToStringForLog(recBuffer, 20));

                int spend = OnReceive(recBuffer);
                byte[] tbytes = new byte[recBuffer.Length - spend];
                Array.Copy(recBuffer, spend, tbytes, 0, tbytes.Length);
                recBuffer = tbytes;
            }

            return true;
        }

        void addQueue(int size)
        {
            Array.Resize(ref tempBuffer, size);
            queue.Enqueue(tempBuffer);
            //Logger.info("[-] enqueue " + tempBuffer.Length);
        }
#endif

        #endregion




        public void connect(string serverAddress, int serverPort)
        {
            address = serverAddress;
            port = serverPort;

            try
            {
                state = State.Connecting;
                if (serverAddress.ToLower().Equals("localhost"))
                {
                    serverAddress = "127.0.0.1";
                }
                IPHostEntry entry = Dns.GetHostEntry(serverAddress);
                int index = 0;
                for (int i = 0; i < entry.AddressList.Length; ++i)
                {
                    IPAddress ip = entry.AddressList[i];
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        index = i;
                        break;
                    }
                }

                // 클라 접속 시 서버에 접속
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.BeginConnect(new IPEndPoint(entry.AddressList[index], serverPort), (ar2) =>
                {
                    try
                    {
                        state = State.Connected;

                        socket.EndConnect(ar2);
                        Logger.info("connected : " + socket.RemoteEndPoint);

                        ReceiveFrom();

                        startWorker();

                        OnConnected(true, null);
                    }
                    catch (SocketException e)
                    {
                        state = State.Closed;
                        Logger.error(e);
                        OnConnected(false, null);
                    }

                }, socket);
            }
            catch (Exception e)
            {
                state = State.Closed;
                Logger.error(e);
                OnConnected(false, null);
            }
        }

        public void close()
        {
            state = State.Closing;
            try
            {
                socket?.Shutdown(SocketShutdown.Both);
                socket?.Close();
                socket = null;

            }
            catch (Exception e)
            {
                Logger.error(e);
            }
            OnDisconnected();
#if USE_THREAD
            try
            {
                int count = 0;
                while (queue.IsEmpty == false && count < 100)  //  remain data
                {
                    Thread.Sleep(10);
                    ++count;
                }

                thread?.Stop();
                thread = null;
            }
            catch (Exception e)
            {
                Logger.error(e);
            }
#endif
            state = State.Closed;
        }


        // 클라 패킷 수신
        void ReceiveFrom()
        {
            try
            {
                state = State.Starting;
#if USE_THREAD
                tempBuffer = new byte[BUFFER_SIZE];
                socket.BeginReceive(tempBuffer, 0, tempBuffer.Length, 0, (ar) =>
#else
                TCPClient.makeReceiveBuffer();
                TCPClient.socket.BeginReceive(conn.recBuffer, conn.recLength, conn.recBuffer.Length - conn.recLength, 0, (ar) =>
#endif
                {
                    try
                    {
                        int bytesRead = socket.EndReceive(ar);
                        if (bytesRead < 1)
                        {
                            throw new SocketException();
                        }
                        //Logger.info("OnReceive : " + socket.RemoteEndPoint + " : " + bytesRead);

#if USE_THREAD
                        addQueue(bytesRead);
#else
                        TCPClient.recLength += bytesRead;
                        TCPClient.doReceive();
#endif
                        //  receive next data.
                        ReceiveFrom();
                    }
                    catch (ObjectDisposedException e)
                    {
                        Logger.info(e.Message);
                    }
                    catch (SocketException)
                    {
                        Logger.info(String.Format("Client {0} closed the connection", socket?.RemoteEndPoint));
                        if (state != State.Closing)
                            close();
                    }
                    catch (Exception e)
                    {
                        Logger.error(e);
                    }
                }, null);
            }
            catch (Exception e)
            {
                Logger.error(e);
            }
        }


        public int SendTo(byte[] data, int start, int length)
        {
            try
            {
                if (data != null && 0 < length && (start + length) <= data.Length)
                {
                    object[] param = new object[] { length };
                    socket.BeginSend(data, start, length, 0,
                        (ar) =>
                        {
                            object[] _param = ar.AsyncState as object[];

                            // Complete sending the data to the remote device.
                            int bytesSent = socket.EndSend(ar);
                            //Logger.info("OnSend : " + socket.RemoteEndPoint + " : " + _param[0]);
                        }, param);
                }
            }
            catch (Exception e)
            {
                Logger.error(e);
            }
            return 0;
        }

        public int SendTo(byte[] buffer)
        {
            return SendTo(buffer, 0, buffer.Length);
        }
        public int SendTo(string data)
        {            
            Logger.info("Try Send : " + (data.Length < 20 ? data : data.Substring(0, 17) + "..."));
            return SendTo(Utils.StringToByte(data));
        }




        protected virtual void OnConnected(bool fine, Exception ex)
        {

        }
        protected virtual void OnDisconnected()
        {

        }

        protected virtual int OnReceive(byte[] recBuffer)
        {
            return recBuffer.Length;
        }
    }
}
