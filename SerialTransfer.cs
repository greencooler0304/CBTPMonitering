//using CBTPTempMoistMoni.Debug;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CBTPTempMoistMoni.Control
{
    public class SerialTransfer
    {
        public enum ProtocolKind { Terminal, StxEtx, PcLink }

        public class DataReceivedEventArgs : EventArgs
        {
            public string data;
        }
        public delegate void DataReceivedHandler(object sender, DataReceivedEventArgs arg);
        public event DataReceivedHandler DataReceived;
        void DoDataReceived(string data)
        {
            DataReceived?.Invoke(this, new DataReceivedEventArgs() { data = data });
        }
        public class ErrorReceivedEventArgs : EventArgs
        {
            public string error;
        }
        public delegate void ErrorReceivedHandler(object sender, ErrorReceivedEventArgs arg);
        public event ErrorReceivedHandler ErrorReceived;
        void DoErrorReceived(string error)
        {
            ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs() { error = error });
        }

        public class ConnectEventArgs : EventArgs
        {
            public bool isConnected;
        }
        public delegate void ConnectStateHandler(object sender, ConnectEventArgs arg);
        public event ConnectStateHandler ConnectState;
        void DoConnect(bool isConnected)
        {
            ConnectState?.Invoke(this, new ConnectEventArgs() { isConnected = isConnected });

            if (isConnected)
                Send("Command"); 
        }



#if SIMULATE
        protected VirtualSerial serialPort;
#else
        protected SerialPort serialPort;
#endif
        protected Protocol protocol;

        public int baudRate { get; set; }
        public int dataBit { get; set; }
        public StopBits StopBits { get; set; }
        public Parity parity { get; set; }
        public ProtocolKind protocolKind { get; set; }

        public SerialTransfer()
        {
#if SIMULATE
            serialPort = new VirtualSerial();
#else
            serialPort = new SerialPort();
#endif
            baudRate = 115200;
            dataBit = 8;
            StopBits = StopBits.One;
            parity = Parity.None;

            serialPort.ErrorReceived += SerialPort_ErrorReceived;
            serialPort.DataReceived += SerialPort_DataReceived;
        }

        bool started = false;
        public bool isRunnable()
        {
            return started && serialPort.IsOpen;
        }

        public void Start(String comport)
        {
            //protocolKind = ptcKind;
            protocol = new PcLinkProtocol();

            serialPort.PortName = comport;
            serialPort.BaudRate = baudRate;
            serialPort.DataBits = dataBit;
            serialPort.StopBits = StopBits;
            serialPort.Parity = parity;


            // 30초 마다 Serial Open 체크
            started = true;
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 2);
            timer.Tick += delegate
            {
                if (started && serialPort.IsOpen == false)
                {
                    serialPort.Open();
                }
                DoConnect(serialPort.IsOpen);
            };
            timer.Start();
        }
        public void Stop()
        {
            serialPort.Close();

            started = false;
        }


        // 바이트 배열을 String으로 변환 
        private string ByteToString(byte[] strByte)
        {
            string str = Encoding.Default.GetString(strByte);
            return str;
        }
        // String을 바이트 배열로 변환 
        private byte[] StringToByte(string str)
        {
            //byte[] StrByte = Encoding.UTF8.GetBytes(str); 
            byte[] StrByte = Encoding.Default.GetBytes(str);
            return StrByte;
        }

        public void Send(string data)
        {
            // 온도PV에서 습도SP(00006)까지 D-Register를 읽는 명령어(고정)
            string strCommand = "01RSD,06,0001";

            //Send(StringToByte(data));
            Send(StringToByte(strCommand));

        }
        public void Send(byte[] data)
        {
            byte[] text = protocol.make(data, true);
            serialPort.Write(text, 0, text.Length);
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] buffer = new byte[serialPort.BytesToRead];
            serialPort.Read(buffer, 0, buffer.Length);

            foreach (string aline in protocol.parse(buffer))
            {
                //if (tcbLogging.SelectedIndex == 0)
                //{
                //    try
                //    {
                //        using (StreamWriter w = File.AppendText(ttxLogPath.Text))
                //        {
                //            w.Write(aline);
                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        System.Diagnostics.Debug.Print(ex.StackTrace);
                //    }
                //}
                //AppendText(this.texBoxLog, aline);
                //outFilterWindow(aline);

                DoDataReceived(aline);

                //outForDebug("parsed", aline);
                //System.Diagnostics.Debug.Print("--------------------------------------------------------");
            }
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            DoErrorReceived(serialPort.ReadExisting());
        }
    }
}
