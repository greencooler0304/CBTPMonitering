using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;

namespace CBTPTempMoistMoni.Control
{
    public abstract class BaseModel : ConfigColors, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChange(string propertyName)
        {
            try
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
            catch (Exception ex)
            {
                PrintLog("BM", "PropertyChange : " + ex.Message);
            }
        }

        public delegate void StartedHandler(object sender, EventArgs arg);
        public event StartedHandler Started;
        protected virtual void doStarted()
        {
            Started?.Invoke(this, new EventArgs());
            OnPropertyChange("IsStart");
            OnPropertyChange("IsStop");
            OnPropertyChange("IsRunable");
            OnPropertyChange("IsRunableOK");
            OnPropertyChange("IsLoading");
        }
        public delegate void FinishedHandler(object sender, EventArgs arg);
        public event FinishedHandler Finshed;
        protected virtual void doFinished()
        {
            Finshed?.Invoke(this, new EventArgs());
            OnPropertyChange("IsStart");
            OnPropertyChange("IsStop");
            OnPropertyChange("IsRunable");
            OnPropertyChange("IsRunableOK");
            OnPropertyChange("IsLoading");
        }
        public class StatuseEventArg : EventArgs
        {
            public enum KIND { Clear, Debug, Info, Warn, Error, Error_Message, Critical }
            public KIND kind;
            public DateTime time;
            public int code;
            public string message;
        }
        public delegate void OnRaiseStatusHandler(object sender, StatuseEventArg arg);
        public event OnRaiseStatusHandler OnRaiseStatus;
        protected virtual void doRaiseStatus(StatuseEventArg.KIND kind, DateTime time, int code, string message)
        {
            OnRaiseStatus?.Invoke(this, new StatuseEventArg() { kind = kind, time = time, code = code, message = message });
            OnPropertyChange("IsStart");
            OnPropertyChange("IsStop");
            OnPropertyChange("IsRunable");
            OnPropertyChange("IsRunableOK");
            OnPropertyChange("IsLoading");
        }
        protected virtual void doRaiseStatus(object sender, StatuseEventArg arg)
        {
            OnRaiseStatus?.Invoke(sender, arg);
            OnPropertyChange("IsStart");
            OnPropertyChange("IsStop");
            OnPropertyChange("IsRunable");
            OnPropertyChange("IsRunableOK");
            OnPropertyChange("IsLoading");
        }
        protected virtual void doLoadingStatus(bool loading)
		{
            OnPropertyChange("IsStart");
            OnPropertyChange("IsStop");
            OnPropertyChange("IsRunable");
            OnPropertyChange("IsRunableOK");
            OnPropertyChange("IsLoading");
        }

        public class LogEventArg : EventArgs
        {
            public string tag;
            public string log;
            public bool add;
        }
        public delegate void OnLogHandler(object sender, LogEventArg arg);
        public event OnLogHandler OnLog;
        public event OnLogHandler OnSaveLog;

        protected virtual void PrintLog(string tag, string log, bool add = true)
        {
            OnLog?.Invoke(this, new LogEventArg() { tag = tag, log = log, add = add });
        }

        protected virtual void SaveLog(string tag, string log, bool add = true)
        {
            OnSaveLog?.Invoke(this, new LogEventArg() { tag = tag, log = log, add = add });
        }

        public class LogSerialEventArg : EventArgs
        {
            public string tag;
            public string log;
        }
        public delegate void OnLogSerialHandler(object sender, LogSerialEventArg arg);
        public event OnLogSerialHandler OnSerialLog;
        protected virtual void PrintSerialLog(string tag, string log)
        {
            OnSerialLog?.Invoke(this, new LogSerialEventArg() { tag = tag, log = log });
        }

        public class LogStateEventArg : EventArgs
        {
            public string tag;
            public string log;
        }
        public delegate void OnLogStateHandler(object sender, LogStateEventArg arg);
        public event OnLogStateHandler OnStateLog;
        protected virtual void PrintStateLog(string tag, string log)
        {
            OnStateLog?.Invoke(this, new LogStateEventArg() { tag = tag, log = log });
        }

        public class LoadEventArg : EventArgs
		{
            public int port;
            public string log;
            public bool add;
		}

        #region Runtime

        protected List<BaseModel> children = new List<BaseModel>();
        protected List<BaseModel> activeChildren
        {
            get
            {
                List<BaseModel> ret = new List<BaseModel>();
                foreach (BaseModel model in children)
                    if (model.Activated)
                        ret.Add(model);
                return ret;
            }
        }
        [XmlIgnore] public bool Activated { get; set; } = true;
        protected bool thread_run = false;
        protected bool loading = false;
        protected bool starting = false;
        protected bool started = false;
        protected bool stopping = false;
        protected bool stopping_by_error = false;
        [XmlIgnore] public bool IsLoading { get { return loading == false; } }
        [XmlIgnore] public bool IsStart { get { return loading == false && started; } }
        [XmlIgnore] public bool IsStop { get { return loading == false && started == false; } }

        [XmlIgnore] public bool IsRunable { get { return runable(); } }
        [XmlIgnore] public string IsRunableOK { get { return runable() ? "OK" : "_"; } }
        [XmlIgnore] public Color IsRunableColor { get { return IsRunable ? ColorSubTitleBack : ColorEditErrorBack; } }
        [XmlIgnore] public Color IsRunningColor
        {
            get
            {
                if (starting)
                {
                    return Colors.DarkBlue;         //  시작 중
                }
                else if (started)                   //  시작 됨.
                {
                    if (stopping)
                        return Colors.LightGray;    //  정지 중
                    else
                        return Colors.LightPink;    //  진행 중
                }
                else
                    return Colors.LightBlue;        //  종료 됨.
            }
        }
        [XmlIgnore] public string IsRunningText
        {
            get
            {
                if (starting)
                {
                    return "시작중";               //  시작 중
                }
                else if (started)                  //  시작 됨.
                {
                    if (stopping)
                        return "종료중";          //  정지 중
                    else
                        return "종료";            //  진행 중
                }
                else
                    return "시작";                //  종료 됨.
            }
        }

        public void UpdateProerty_for_runtime(bool do_child = true)
        {
            if (do_child)
            {
                foreach (BaseModel model in children)
                    model.UpdateProerty_for_runtime();
            }

            OnPropertyChange("IsStart");
            OnPropertyChange("IsStop");
            OnPropertyChange("IsRunable");
            OnPropertyChange("IsRunableOK");
            OnPropertyChange("IsRunableColor");
            OnPropertyChange("IsRunningColor");
            OnPropertyChange("IsRunningText");
        }

        #endregion  //  Runtime



        public virtual void clearChild()
        {
            children.Clear();
        }

        protected virtual int addChild(BaseModel model)
        {
            model.Started += Model_Started;
            model.Finshed += Model_Finshed;
            model.OnRaiseStatus += Model_StatusChanged;

            children.Add(model);

            return children.Count;
        }


        public int getFirstActiveChildIndex()
        {
            return getNextActiveChildIndex(-1);
        }
        public int getLastActiveChildIndex()
        {
            return getPrevActiveChildIndex(children.Count);
        }
        public int getPrevActiveChildIndex(int old)
        {
            int ret = old - 1;
            while (-1 < ret && ret < children.Count)
            {
                if (children[ret].Activated)
                    break;
                ret--;
            }
            return ret;
        }
        public int getNextActiveChildIndex(int old)
        {
            int ret = old + 1;
            while (-1 < ret && ret < children.Count)
            {
                if (children[ret].Activated)
                    break;
                ret++;
            }
            return ret;
        }

        public virtual void onLoading(bool loading)
		{
            this.loading = loading;
            doLoadingStatus(loading);

            foreach (BaseModel model in children)
			{
                model.onLoading(loading);
			}
        }

        public virtual void openSerial()
		{
            foreach (BaseModel model in children)
			{
#if !SIMULATE
                Thread.Sleep(1000);
#endif
                model.openSerial();
            }
        }

        public virtual void closeSerial()
		{
            foreach (BaseModel model in children)
			{
#if !SIMULATE
                Thread.Sleep(1000);
#endif
                model.thread_run = false;
                model.closeSerial();
			}
		}

        public virtual void init()
        {
            thread_run = true;
            foreach (BaseModel model in children)
                model.init();
        }
        public virtual void terminate()
        {
            foreach (BaseModel model in children)
                model.terminate();
        }

        public virtual bool runable()
        {
            bool ret = true;
            foreach (BaseModel model in activeChildren)
                ret = ret && model.runable();
            return ret;
        }

        public virtual void start()
        {
            starting = true;

            if (activeChildren.Count < 1)
            {
                starting = false;
                started = true;
                doStarted();
            }
            else
            {
                foreach (BaseModel model in activeChildren)   //  Model_Started() 에서 확인
                    model.start();
            }
            UpdateProerty_for_runtime();
        }
        public virtual void stop(bool byError = false)
        {
            //if (stopping)
            //{
            //    //throw new Exception("Already started Stop.");
            //    System.Diagnostics.Debug.Print("Already started Stop.");
            //}
            //else
            {
                stopping = true;
                stopping_by_error = byError;
                if (activeChildren.Count < 1)
                {
                    started = false;
                    stopping = false;
                    //stopping_by_error = false;
                    doFinished();
                }
                else
                {
                    foreach (BaseModel model in activeChildren)   //  Model_Finshed() 에서 확인
                        model.stop(byError);               
                }
            }
            UpdateProerty_for_runtime();
        }
        public virtual void reset()
        {
            stopping = true;
            stopping_by_error = true;
            if (activeChildren.Count < 1)
			{
                started = false;
                stopping = false;
                doFinished();
			}
            foreach (BaseModel model in activeChildren)
                model.reset();
            UpdateProerty_for_runtime();
        }
        public virtual void ForcedStopForError()
        {
            started = false;
            stopping = false;
            doFinished();
        }

        public virtual void ToggleStart()
        {
            if (starting)
            {
                //  시작 중
            }
            else if (started)
            {
                if (stopping)
                    ;   //  정지 중
                else
                    stop(false);    //  진행 중
            }
            else
                start();//  종료 됨.
            UpdateProerty_for_runtime();
        }

        public virtual void StopOnStoping()
        {
            //if (stopping && started)
            //{
            //}
            foreach (BaseModel model in activeChildren)
                model.StopOnStoping();
        }

#if SIMULATE
        protected bool runTimeSerialDebug = true;
#else
        protected bool runTimeSerialDebug = false;
#endif
        public virtual void runTimeSerialDebugMode(bool debug)
        {
            runTimeSerialDebug = debug;
            foreach (BaseModel model in children)
                model.runTimeSerialDebugMode(debug);
        }

#if SIMULATE
        protected bool runTimeStateDebug = true;
#else
        protected bool runTimeStateDebug = false;
#endif
        public virtual void runTimeStateDebugMode(bool debug)
        {
            runTimeStateDebug = debug;
            foreach (BaseModel model in children)
                model.runTimeStateDebugMode(debug);
        }




        protected virtual void Model_Started(object sender, EventArgs arg)
        {
            bool running = true;
            foreach (BaseModel model in activeChildren)
            {
                 running = running && model.IsStart;
            }
            if (running)
            {
                starting = false;
                started = true;
                doStarted();
            }
        }
        protected virtual void Model_Finshed(object sender, EventArgs arg)
        {
            bool running = false;
            foreach (BaseModel model in activeChildren)
            {
//#if DEBUG
//                if (model is HTDut)
//                    PrintLog((model as HTDut).Dut_1_Name, "starget = " + model.started + "--------------------------");
//#endif
                running = running || model.IsStart;
            }
            if (running == false)
            {
                started = false;
                stopping = false;
                stopping_by_error = false;
                doFinished();
            }
        }

        protected virtual void Model_StatusChanged(object sender, StatuseEventArg arg)
        {
            doRaiseStatus(sender, arg);
        }

        SerialTransfer transfer = new SerialTransfer(); 
    }
}
