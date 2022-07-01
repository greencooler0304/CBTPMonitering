using CBTPTempMoistMoni.Common;
using CBTPTempMoistMoni.Server;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;
using System.IO.Ports;
using System.Threading;
using System.Windows.Threading;

namespace CBTPTempMoistMoni.Control
{
    /// <summary>
    /// SensorTempControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SensorTempControl : UserControl
    {
        public SensorTempControl()
        {
            InitializeComponent();
        }



        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SensorTemp sensor = this.DataContext as SensorTemp;

            if (sensor != null)
            {
                lsLog.setLogFile("Save");

                sensor.OnLog += Li_OnLog;
            }

        }

        private void Li_OnLog(object sender, BaseModel.LogEventArg arg)
        {
            lsLog.print(sender, arg);
        }
    }

    public class SensorTemp : BaseModel
    {
        [XmlAttribute] public string TemptureValue { get; set; }
        [XmlAttribute] public string MoistureValue { get; set; }


        [XmlIgnore] public TCPServer server = null;
        public class _Service : Service
        {
            SensorTemp parent;
            public _Service(SensorTemp parent, string name) : base(name)
            {
                this.parent = parent;
            }

            public override int OnReceive(byte[] recBuffer)
            {
                Logger?.info("Received : " + Utils.ByteToStringForLog(recBuffer));

                float value = 0;

                if (float.TryParse(Utils.ByteToString(recBuffer), out value))
                    parent.Temp = value;

                DoSend(recBuffer);

                return recBuffer.Length;
            }

        }
        [XmlIgnore] public _Service service = null;

        float _temp = 0.0f;
        [XmlIgnore] public float Temp
        {
            get { return _temp; }
            set
            {
                if (_temp != value)
                {
                    _temp = value;
                    OnPropertyChange("Temp");
                }
            }
        }

        float _moisture = 0.0f; 


        [XmlIgnore]
        public float Moisture
        {
            get { return _moisture; }
            set
            {
                if (_moisture != value)
                {
                    _moisture = value;
                    OnPropertyChange("Moisture");
                }
            }
        }

        public float upateValuesFromMSG(byte[] byArray)
        {
            int iIndex = 0;
            int iValue = 0;
            foreach (byte achar in byArray)
            {
                byte byTemp = 0x00;

                if (achar >= 65) byTemp = (byte)(achar - 0x37);
                else byTemp = (byte)(achar - 0x30);

                switch (iIndex)
                {
                    case 0:
                        iValue += byTemp << 12;
                        break;
                    case 1:
                        iValue += byTemp << 8;
                        break;
                    case 2:
                        iValue += byTemp << 4;
                        break;
                    case 3:
                        iValue += byTemp;
                        break;
                    default:
                        break;
                }

                iIndex++;
            }

            float fValue = (float)(iValue * 0.1);

            return fValue; 
        }

        protected SerialPort serialPort;
        protected PcLinkProtocol PcLinkTrans = new PcLinkProtocol();

        public SensorTemp()
        {
            service = new _Service(this, "Temp");
            //server = new TCPServer();

            serialPort = new SerialPort();
        }

        int getRunSec()
        {
            return (int)(DateTime.Now - run_time).TotalSeconds;
        }

        private string _Name = "TEMI";

        public string Name { get { return _Name; } set { _Name = value; } }

        bool startedCheckPort = false;

        DateTime run_time;
        DateTime start_time;
        DateTime start_time_step;
        long run_last_sec = 0;
        long step_last_sec = 0;
        long read_last_sec = 0;

        public override void init()
        {
            // 위지스 온습도 champer의 콘솔은 포트가 고정임 
            serialPort.PortName = "COM2";
            serialPort.BaudRate = 115200;
            serialPort.DataBits = 8;
            serialPort.StopBits = StopBits.One;
            serialPort.Parity = Parity.None;

            base.init();

            startedCheckPort = true;
            run_time = DateTime.Now;
            thread_run = true;
            (new Thread(() =>
            {
                DateTime start_t = DateTime.Now;
                long old_log_time = 0;
                while (thread_run)
                {
                    long _time = (long)DateTime.Now.Subtract(start_t).TotalSeconds;
                    int run_sec = getRunSec();
                    if (run_last_sec != run_sec)
                    {
                        run_last_sec = run_sec;
                        if (startedCheckPort)
                        {
                            if (serialPort.IsOpen)
                            {
                                //OnPropertyChange("IsRunable");
                            }
                            else
                            {
#if MANUAL_OPEN_POW
#else
                                try
                                {
                                    serialPort.Open();
                                    Logger.save("Port Opened");
                                }
                                catch (Exception ex)
                                {
                                    if (old_log_time == 0 || old_log_time + 5 < _time)
                                    {
                                        old_log_time = _time == 0 ? 1 : _time;
                                        PrintLog(Name, ex.Message);
                                    }
                                    else
                                        PrintLog(Name, ex.Message, false);

                                    string sMessage = "Stop - " + ex.Message;
                                    Logger.save(sMessage);
                                }
#endif
                            }
                            UpdateProerty_for_runtime(false);
                        }
                    }

                    try
                    {
                        doing();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Print(ex.ToString());
                    }

#if debug
                    Thread.Sleep(500);
#else
                    Thread.Sleep(10 * 1000);   
#endif
                }
            })).Start();
        }

        DateTime read_temp_time_for_debug;
        long read_temp_count_for_debug = 0;

        enum State
        {
            None, Ready, SendReceive, CheckError, Break, Stop, Finshed, Error
        }
        State state = State.None;

        enum RunState { Start, Find, Send, Delay, Recevie, DelayRetry, Complete }
        RunState runState = RunState.Start;
        enum RunReult { Continue, Completed, Error }

        public enum ParseReult { Succ, Fail, Contine }
        public enum CheckResult { Succ, Fail, Retry, RecvError }


        void doing()
        {
            //if (_Name.Equals("Device2"))
            //    System.Diagnostics.Debug.Print("");

            RunReult ret = RunReult.Continue;
            State curState = state;

            switch (curState)
            {
                case State.None:
                    break;
                case State.Ready:
                    start_time = DateTime.Now;
                    PrintLog(Name, "Start");
                    Logger.save("Start");

                    ret = RunReult.Completed;
                    break;
                case State.SendReceive:
                    ret = Run_SendReceivJop("Send MSG", false, MakePacket_GetTempAndMoisture, ChekcAnswerMSG, GetCunrIndex_Normal, NG_ERROR_CODE);
                    break;
                case State.Break:
                    PrintLog(Name, "Break");
                    ret = RunReult.Completed;
                    break;
                case State.Stop:
                    PrintLog(Name, "Stop");
                    Logger.save("Stop - state stop");
                    ret = RunReult.Completed;
                    break;
                case State.Finshed:
                    PrintLog(Name, "Finished.");
                    Logger.save("Finished");

                    ret = RunReult.Completed;
                    base.stop(false);
                    break;
                case State.Error:
                    doRaiseStatus(StatuseEventArg.KIND.Critical, DateTime.Now, -1, "치명적 오류가 발생. 종료시도");
                    break;
            }

            switch (ret)
            {
                case RunReult.Continue:
                    break;
                case RunReult.Completed:
                    switch (curState)
                    {
                        case State.Ready:
                            state = State.SendReceive;
                            break;
                        case State.Finshed:
                            state = State.None;
                            break;
                    }
                    runState = RunState.Start;  //  시작
                    break;
                case RunReult.Error:
                    state = State.Error;
                    break;
            }
        }

        DateTime last_start_delay_time;
        byte[] do_send_packet_buffer = null;
        int do_child_index = 0;
        int do_retry_count = 0;
        string do_send_packet = "";

        public delegate int getCurrentIndex(int old);

        public delegate byte[] MakePacketHandler(int address, out bool isDelay);
        public delegate CheckResult CheckAnswerHandler(int addr, string data);

        int iErrorIndex = 0;

        string strErrCode = null; 

        public class ErrorCode
        {
            public int code;
            public string desc;
        }

        byte[] MakePacket_GetTempAndMoisture(int addr, out bool isDelay)
        {
            isDelay = true;

            //string strCommand = "01RSD,06,0001";
            string strCommand = "01RSD,OK,011E,01F4,0B1C,1124,03D9,02BC";

            //string strCommand = "01AMI";

            byte[] StrByte = Encoding.Default.GetBytes(strCommand);

            byte[] do_send_packet = PcLinkTrans.make(StrByte, true);  
            return do_send_packet;
        }

        CheckResult ChekcAnswerMSG(int addr, string data)
        {
            // 수신 데이터 FORMAT : 0x02 + 01RSD,OK,01F4, 012C19 + CR + LF 
            byte[] arrByteDatas = Encoding.UTF8.GetBytes(data);
            string ParsingResult = null;
            string[] ParsingDatas = null;
            string strResult = null;
            string strTempValue, strMoistureValue;

            foreach (byte achar in arrByteDatas)
            {
                ParsingResult = PcLinkTrans.parse(achar, true); 
            }

            if(ParsingResult != null)
            {
                ParsingDatas = ParsingResult.Split(',');
                
                if(ParsingDatas != null && ParsingDatas.Length > 6)
                {
                    strResult = ParsingDatas[1];
                }
            }
            else
            {
                Logger.save("Error - Data Error");
                return CheckResult.Retry;
            }

            if(strResult.Equals("OK"))
            {
                // 2th : 현재온도 
                // 3th : 설정온도
                strTempValue = ParsingDatas[2];
                Temp = upateValuesFromMSG(Utils.StringToByte(strTempValue));

                // 6th : 현재습도
                // 7th : 설정습도 
                strMoistureValue = ParsingDatas[6];
                Moisture = upateValuesFromMSG(Utils.StringToByte(strMoistureValue));

                string sLogData = Temp.ToString() + ",," + Moisture.ToString() + ",,";  
                Logger.save(sLogData);

                return CheckResult.Succ;
            }
            else if (strResult.Equals("NG"))
            {
                strErrCode = ParsingDatas[2];

                switch (strErrCode)
                {
                    case "01":
                        iErrorIndex = 0;
                        break;
                    case "02":
                        iErrorIndex = 1;
                        break;
                    case "04":
                        iErrorIndex = 2;
                        break;
                    case "08":
                        iErrorIndex = 3;
                        break;
                    case "11":
                        iErrorIndex = 4;
                        break;
                    case "12":
                        iErrorIndex = 5;
                        break;
                    case "00":
                        iErrorIndex = 6;
                        break;
                }

                string sMessage = "Error - NG Code : " + strErrCode;
                Logger.save(sMessage);

                return CheckResult.Fail;
            }
            else 
                return CheckResult.Retry;
        }

        int GetCunrIndex_Normal(int old_addr)
        {
            return old_addr + 1;
        }
        int getStepSec()
        {
            return (int)(DateTime.Now - start_time_step).TotalSeconds;
        }

        ErrorCode[] NG_ERROR_CODE = new ErrorCode[]
        {
            new ErrorCode() { code = 0x01, desc = "존재하지 않는 커맨드 지정" },
            new ErrorCode() { code = 0x02, desc = "존재하지 않는 D-Register 지정" },
            new ErrorCode() { code = 0x04, desc = "데이터 설정 Error - 유효한 데이터 이외의 문자 사용" },
            new ErrorCode() { code = 0x08, desc = "잘못된 Format 구성 - 유효한 데이터 이외의 문자 사용" },
            new ErrorCode() { code = 0x0B, desc = "CheckSum Error" },
            new ErrorCode() { code = 0x0C, desc = "Monitoring Command Error - 지정한 Command가 없음" },
            new ErrorCode() { code = 0x00, desc = "기타 Error" },
        };



        RunReult Run_SendReceivJop(string tag, bool logmode, MakePacketHandler makefunc, CheckAnswerHandler checkAnswer,
            getCurrentIndex getIndex, ErrorCode[] errors)
        {
            RunReult ret = RunReult.Continue;
            switch (runState)
            {
                case RunState.Start:
                    start_time_step = DateTime.Now;
                    do_child_index = -1;
                    //runState = RunState.Find;
                    runState = RunState.Send;
                    receiveBuilder.Clear();
                    break;

                //case RunState.Find:
                //    //if (_Name.Equals("HSB") && state == State.SetPower)
                //    //    System.Diagnostics.Debug.Print("");
                //    do_child_index = getIndex(do_child_index);  //   oder_inc ? 0 : chidren.Count - 1;
                //    if (do_child_index < 0)
                //        ;   //  Wait
                //    else if (children.Count <= do_child_index)
                //        ret = RunReult.Completed;   //  Complete
                //    else
                //        runState = RunState.Send;
                //    break;

                case RunState.Send:
                    {
                        //System.Diagnostics.Debug.Print("");
                        bool isDelay;
                        serialPort.ReadExisting();  //clear
                        receiveBuilder.Clear();

                        do_send_packet_buffer = makefunc(do_child_index, out isDelay);

                        PrintLog("", "S:" + Utils.ByteToString(do_send_packet_buffer));//, false);

                        serialPort.Write(do_send_packet_buffer, 0, do_send_packet_buffer.Length);

                        if (logmode) PrintLog(Name, String.Format(tag, do_child_index));

                        do_retry_count = 0;

                        if (isDelay)
                        {
                            runState = RunState.Delay;
                            last_start_delay_time = DateTime.Now;
                        }

                        runState = RunState.Recevie;
                    }
                    break;

                case RunState.Delay:
                    if (1 * 5000 < DateTime.Now.Subtract(last_start_delay_time).TotalMilliseconds)
                    {
                        Logger.save("NoResponse");
                        runState = RunState.Send;
                    }
                    break;

                case RunState.DelayRetry:
                    if (step_last_sec + 1 < getStepSec())
                    {
                        bool isDelay;
                        do_retry_count++;
                        PrintLog(Name, String.Format(tag, do_child_index) + " retry=" + do_retry_count);
                        do_send_packet_buffer = makefunc(do_child_index, out isDelay);

                        PrintLog(Name, "DS:" + Utils.ByteToString(do_send_packet_buffer), false);

                        serialPort.Write(do_send_packet_buffer, 0, do_send_packet_buffer.Length);

                        //runState = isDelay ? RunState.Delay : RunState.Recevie;
                        runState = RunState.Recevie;
                    }
                    break;

                case RunState.Recevie:
                    string data = "";
                    byte[] buffer = new byte[serialPort.BytesToRead];
                    serialPort.Read(buffer, 0, buffer.Length);

                    if (0 < buffer.Length)
                    {
                        PrintLog("", "R:" + Utils.ByteToString(buffer));

                        switch (parseReceiveData(buffer, ref data))
                        {
                            case ParseReult.Succ:   //  잘된 packet
                                switch (checkAnswer(do_child_index, data))
                                {
                                    case CheckResult.Succ:      //  정상 응답
                                                                //runState = RunState.Find;
                                        runState = RunState.Send;

                                        break;
                                    case CheckResult.Fail:      //  문제가 발생함. 
                                        doRaiseStatus(StatuseEventArg.KIND.Error, DateTime.Now, errors[iErrorIndex].code, String.Format(errors[iErrorIndex].desc, do_child_index));
                                        ret = RunReult.Error;
                                        break;
                                    case CheckResult.Retry:     //  다시 시도
                                        start_time_step = DateTime.Now; //  TODO : 위치 좋지 않음.
                                        step_last_sec = getStepSec();
                                        runState = RunState.DelayRetry;
                                        //delay_last_sec = getStepSec();
                                        break;
                                    case CheckResult.RecvError: //  오류를 받음.
                                        doRaiseStatus(StatuseEventArg.KIND.Error, DateTime.Now, errors[iErrorIndex].code, String.Format(errors[iErrorIndex].desc, do_child_index));
                                        ret = RunReult.Error;
                                        break;
                                }
                                break;

                            case ParseReult.Fail:       //  잘못된 packet -- 여기서는 protocol이 딱히 없어 Fail은 없음.
                                doRaiseStatus(StatuseEventArg.KIND.Warn, DateTime.Now, errors[iErrorIndex].code, String.Format(errors[iErrorIndex].desc, do_child_index));  // + "번째 센서인식 명령에 응답이 정확하지 않습니다.");
                                break;

                            case ParseReult.Contine:    //  parsing 중
                                int cur_step_sec = getStepSec();
                                if (cur_step_sec < 6)
                                {
                                    if (step_last_sec != cur_step_sec)
                                    {
                                        step_last_sec = cur_step_sec;
                                        if (step_last_sec % 2 == 0)
                                        {
                                            do_retry_count++;

                                            PrintLog(Name, String.Format(tag, do_child_index) + " retry=" + do_retry_count);
                                            PrintLog(Name, "RS:" + Utils.ByteToString(do_send_packet_buffer));

                                            serialPort.Write(do_send_packet_buffer, 0, do_send_packet_buffer.Length);
                                        }
                                    }
                                }
                                else
                                {
                                    doRaiseStatus(StatuseEventArg.KIND.Error, DateTime.Now, errors[3].code, String.Format(errors[3].desc, do_child_index));  // + "번째 센서인식 명령에 응답하지 않습니다.");
                                    ret = RunReult.Error;
                                }
                                break;
                        }
                    }
                    else
                    {
                        runState = RunState.Delay;
                    }
                    break;

                case RunState.Complete:
                    break;
            }
            return ret;
        }


        StringBuilder receiveBuilder = new StringBuilder();
        bool bCRValue = false; 

        ParseReult parseReceiveData(byte[] buffer, ref string data)
        {
            ParseReult ret = ParseReult.Contine;

            foreach (byte ab in buffer)
            {
                switch (ab)
                {
                    case 0x0D:
                        bCRValue = true;
                        receiveBuilder.Append((char)ab);
                        break;

                    case 0x0A:
                        if (bCRValue)
                        {
                            receiveBuilder.Append((char)ab);
                            data = receiveBuilder.ToString();
                            receiveBuilder.Clear();
                            bCRValue = false; 

                            ret = ParseReult.Succ;

                        }
                        break;
                    default:
                        receiveBuilder.Append((char)ab);
                        bCRValue = false; 
                        break;
                }
            }
            return ret;
        }



        public override void terminate()
        {
            thread_run = false;
            startedCheckPort = false;
            serialPort.Close();

            base.terminate();
        }

        public override void start()
        {
            //base.start(); 

            if (serialPort.IsOpen)
            {
#if SIMULATE
                serialPort.setErrRange(ErrVolRange, ErrCurRange);
#endif
                //foreach (APower ch in children)
                //{
                //    ch.buildSec();
                //}
                state = State.Ready;

                Temp = 0.0F;
                Moisture = 0.0F; 

                base.start();
            }
            else
            {
                PrintLog(Name, "Port Open 되지 않았습니다.");
                doRaiseStatus(StatuseEventArg.KIND.Error, DateTime.Now, -1, "Port Open 되지 않았습니다.");
            }
        }
        public override void stop(bool byError = false)
        {
            //server.Stop();
            //server.RemoveMe(service);
            //service.stop();

            if (byError)
                state = State.Break;
            else
                state = State.Stop;


            base.stop(byError);
        }
    }
}
