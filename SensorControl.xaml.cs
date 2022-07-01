using CBTPTempMoistMoni.Control;
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

namespace CBTPTempMoistMoni.Simulate
{
    /// <summary>
    /// ServerControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SensorControl : UserControl
    {
        public SensorControl()
        {
            InitializeComponent();

            SetMinMaxValue(0.1f, 20f, 0.1f, "{0:F1}");
        }

        public void SetMinMaxValue(float min, float max, float gap, string format = "{0:F2}")
        {
            cbValue.Items.Clear();
            for (float i = min; i < max; i += gap)
                cbValue.Items.Add(string.Format(format, i));
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            (this.DataContext as Sensor)?.start();
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            (this.DataContext as Sensor)?.stop();
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Sensor sensor = this.DataContext as Sensor;
            if (sensor != null)
            {
                sensor.OnLog += Li_OnLog;
                sensor.client.setLogger(lsLog);
            }
            //System.Diagnostics.Debug.Print(li.ToString());
        }

        private void Li_OnLog(object sender, BaseModel.LogEventArg arg)
        {
            lsLog.print(sender, arg);
        }

        private void cbValue_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
            }
        }
    }

    public class Sensor : BaseModel
    {
        public class _TcpClient : TCPClient
        {
            Sensor parent = null;
            public _TcpClient(Sensor parent, string name) : base(name)
            {
                this.parent = parent;
            }
            protected override void OnConnected(bool fine, Exception ex)
            {
                base.OnConnected(fine, ex);
                parent.startTimer();
            }
            protected override void OnDisconnected()
            {
                parent.stopTime();
                base.OnDisconnected();
            }
            protected override int OnReceive(byte[] recBuffer)
            {
                return base.OnReceive(recBuffer);
            }
        }
        [XmlIgnore] public _TcpClient client = null;

        public string _Name = "None";
        public String Name
        {
            get { return _Name; }
            set
            {
                _Name = value;
                OnPropertyChange("name");
            }
        }

        string _ip = "127.0.0.1";
        public string IP { 
            get { return _ip; }
            set
            {
                if (_ip != value)
                {
                    _ip = value;
                    OnPropertyChange("ip");
                }
            }
        }
        int _port = 4000;
        public int Port {
            get { return _port; }
            set { 
                if (_port != value)
                {
                    _port = value;
                    OnPropertyChange("Port");
                }
            } 
        }
        float _Value = 0;
        public float Value
        {
            get { return _Value; }
            set { 
                _Value = value; 
                OnPropertyChange("Value"); 
            }
        }

        private System.Timers.Timer timer = null;


        public Sensor(String name)
        {
            this.Name = name;
            client = new _TcpClient(this, name);
        }


        public override void init()
        {
            base.init();
        }

        public override void start()
        {
            client.connect(IP, Port);
            base.start();
        }
        public override void stop(bool byError = false)
        {
            client.close();
            base.stop(byError);
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            client.SendTo(Value.ToString());
        }
        private void startTimer()
        {
            timer = new System.Timers.Timer(2000);
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = true;
            timer.Enabled = true;
        }
        private void stopTime()
        {
            timer?.Stop();
            timer = null;
        }

    }
}
