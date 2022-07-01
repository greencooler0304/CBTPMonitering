#define USE_THREAD

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
    public class TCPServer
    {
        private int port = 6297;
        private Socket listen = null;
        private List<Service> services = new List<Service>();
        private UInt64 id = 0;
        private ILogger Logger = null;
        private Service service = null;


        public void Start(int port, Service service = null)
        {
            this.port = port;
            this.service = service;
            if (this.service != null)
                services.Add(service);
            DoWork();
        }

        public void Stop()
        {
            try
            {
                foreach (Service service in services)
                {
                    service.DoClose();
                }
                listen?.Shutdown(SocketShutdown.Both);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print(e.Message);
                System.Diagnostics.Debug.Print(e.StackTrace);
                Logger?.error(e);
            }
        }

        public void setLogger(ILogger logger)
        {
            Logger = logger;
        }

        void DoWork()
        {
            Logger?.info("DoWork");
            listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listen.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listen.Bind(new IPEndPoint(IPAddress.Any, port));
            listen.Listen(20);
            Logger?.info("Service listen " + IPAddress.Any.ToString() + " : " + port);

            // start listening
            try
            {
                // 클라이언트 접속
                Accept();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print(e.StackTrace);
                Logger?.error(e);
            }
        }

        // 클라이언트 접속 관리
        void Accept()
        {
            try
            {
                listen.BeginAccept((ar =>
                {
                    try
                    {
                        Socket socket = listen.EndAccept(ar);
                        Logger?.info("Accept : " + socket.RemoteEndPoint);

                        // 재귀
                        Accept();

                        if (this.service != null)                                 //  TODO : 꼼수임.
                        {
                            if (this.service.socket == null ||
                                this.service.socket.Connected == false)    //  connection을 한개만 유지
                            {
                                this.service.socket = socket;
                                this.service.server = this;
                                ReceiveFrom(this.service);
                            }
                            else
                            {
                                Logger?.warn("Already connected.");
                            }
                        }
                        else
                        {
                            //Service service = new Service("service_" + (id++));
                            //service.socket = socket;
                            //service.server = this;
                            //services.Add(service);
                            //ReceiveFrom(service);
                        }

                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.Print(e.StackTrace);
                        Logger?.error(e);
                    }
                }), listen);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print(e.StackTrace);
                Logger?.error(e);
            }
        }



        // 클라 패킷 수신
        public void ReceiveFrom(Service service)
        {
            try
            {
#if USE_THREAD
                service.tempBuffer = new byte[Service.BUFFER_SIZE];
                service.socket.BeginReceive(service.tempBuffer, 0, service.tempBuffer.Length, 0, (ar) =>
#else
                conn.makeReceiveBuffer();
                conn.socket.BeginReceive(conn.recBuffer, conn.recLength, conn.recBuffer.Length - conn.recLength, 0, (ar) =>
#endif
                {
                    try
                    {
                        int bytesRead = service.socket.EndReceive(ar);
                        if (bytesRead < 1)
                        {
                            Logger?.info(String.Format("{0} diconnected by remote.", service.name));
                            throw new SocketException();
                        }
                        //Logger?.info("OnReceive : " + conn.socket.RemoteEndPoint + " : " + bytesRead);

                        Service _service = ar.AsyncState as Service;
#if USE_THREAD
                        _service.AddQueue(bytesRead);
#else
                        _conn.recLength += bytesRead;
                        _conn.doReceive();
#endif
                        //  재귀. receive next data.
                        ReceiveFrom(service);
                    }
                    catch (ObjectDisposedException e)
                    {
                        System.Diagnostics.Debug.Print(e.StackTrace);
                        Logger?.info(e.Message);
                    }
                    catch (SocketException e)
                    {
                        System.Diagnostics.Debug.Print(e.StackTrace);
                        //Logger?.info(String.Format("{0} {1} closed the connection", conn.name, conn.socket?.RemoteEndPoint));
                        Logger?.info(String.Format("{0} closed the connection  : {1}", service.name, e.Message));
                        service.DoClose();
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.Print(e.StackTrace);
                        Logger?.error(e);
                    }
                }, service);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print(e.StackTrace);
                Logger?.error(e);
            }
        }

        public void SendTo(Service service, byte[] data)
        {
            if (data != null)
            {
                SendTo(service, data, 0, data.Length);
            }
        }

        public int SendTo(Service service, byte[] data, int start, int length)
        {
            try
            {
                if (data != null && 0 < length && (start + length) <= data.Length)
                {
                    object[] param = new object[] { service, length, data };
                    service.socket.BeginSend(data, start, length, 0,
                        (ar) =>
                        {
                            object[] _param = ar.AsyncState as object[];
                            Service _service = _param[0] as Service;
                            // Complete sending the data to the remote device.
                            int bytesSent = _service.socket.EndSend(ar);
#if DEBUG
                            //Logger?.info("OnSend : " + conn.socket.RemoteEndPoint + ". " + _param[1] + ". " + Util.toStringForDebug(_param[2] as byte[], 10));
                            //Logger?.info("OnSend : " + conn.socket.RemoteEndPoint + ". " + _param[1]);
#endif
                        }, param);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print(e.StackTrace);
                Logger?.error(e);
            }
            return 0;
        }

        public void RemoveMe(Service service)
        {
            try
            {
                services.Remove(service);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print(e.StackTrace);
                Logger?.error(e);
            }
        }
    }



    public class Service
    {
        public static int BUFFER_SIZE = 1024 * 1024;

        //public delegate int doing(Connection conn, byte[] data, int length);
        //public delegate void doclose(Connection conn);

        public string name;
        //private doing _doing = null;
        //private doclose _doclose = null;
        public TCPServer server = null;

#if USE_THREAD
        public ConcurrentQueue<byte[]> queue = new ConcurrentQueue<byte[]>();
        public byte[] tempBuffer;
        private ManagedThread thread = null;
#endif

        public Socket socket;
        public byte[] recBuffer = new byte[0];
        //private int sndLength = 0;
        protected ILogger Logger = null;

        public Service(string name)//, doing doing, doclose doclose)
        {
            this.name = name;
            //this._doing = doing;
            //this._doclose = doclose;
        }
        ~Service()
        {
        }

        public void SetLogger(ILogger logger)
        {
            Logger = logger;
        }

        public void start()
        {
#if USE_THREAD
            this.thread = new ManagedThread("TH_" + name, OnWorking);
            this.thread.Start();
            Logger?.info("start Worker");
#endif
        }
        public void stop()
        {
#if USE_THREAD
            Logger?.info("stop Worker");
            thread?.Stop();
#endif
        }

        public void Close()
        {
            try
            {
#if USE_THREAD
                thread.Stop();
#endif
                Logger?.info("Service manager : close socket.");
                socket?.Shutdown(SocketShutdown.Both);
                socket?.Close();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print(e.StackTrace);
                Logger?.error(e);
            }
        }


#if USE_THREAD
        public bool OnWorking(UInt64 calledCount)
        {
            LoadAndDoReceive();
            return true;
        }

        public void AddQueue(int size)
        {
            Array.Resize(ref tempBuffer, size);
            queue.Enqueue(tempBuffer);
            //Logger?.info(name + " enqueue " + tempBuffer.Length);
        }

        public void LoadAndDoReceive()
        {
            byte[] temp = null;
            for (int i = 0; queue.IsEmpty == false && i < 20; i++)
            {
                //Logger?.info("[ServiceManager] queue : " + queue.Count);
                if (queue.TryDequeue(out temp))
                {
                    byte[] abytes = new byte[recBuffer.Length + temp.Length];
                    Array.Copy(recBuffer, 0, abytes, 0, recBuffer.Length);
                    Array.Copy(temp, 0, abytes, recBuffer.Length, temp.Length);
                    recBuffer = abytes;
                }
            }
            //Logger?.info("[ServiceManager] end " + recLength + " queue:" + queue.Count);

            if (recBuffer.Length > 0)
                DoReceive();
        }
#endif

        public virtual void OnClose()
        {
            Close();
        }

        public void DoClose()
        {
            try
            {
#if USE_THREAD
                while (queue.IsEmpty == false)  //  remain data
                    Thread.Sleep(10);
#endif
                OnClose();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print(e.StackTrace);
                Logger?.error(e);
            }
        }

        public virtual int OnReceive(byte[] recBuffer)
        {
            return recBuffer.Length;
        }

        public void DoReceive()
        {
            int spend = OnReceive(recBuffer);
            byte[] abytes = new byte[recBuffer.Length - spend];
            Array.Copy(recBuffer, spend, abytes, 0, abytes.Length);
            recBuffer = abytes;
        }

        public int DoSend(byte[] data)
        {
            int? ret = server?.SendTo(this, data, 0, data.Length);
            return ret != null ? (int)ret : 0;
        }
    }
}
