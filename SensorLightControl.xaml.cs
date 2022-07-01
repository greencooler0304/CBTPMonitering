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

namespace CBTPTempMoistMoni.Control
{
    /// <summary>
    /// SensorLightControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SensorLightControl : UserControl
    {
        public SensorLightControl()
        {
            InitializeComponent();
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SensorLight sensor = this.DataContext as SensorLight;
            if (sensor != null)
            {
                sensor.server.setLogger(lsLog);
                sensor.service.SetLogger(lsLog);
            }
        }
    }




    public class SensorLight : BaseModel
    {
        [XmlIgnore] public TCPServer server = null;
        public class _Service : Service
        {
            SensorLight parent;
            public _Service(SensorLight parent, string name) : base(name)
            {
                this.parent = parent;
            }

            public override int OnReceive(byte[] recBuffer)
            {
                Logger?.info("Received : " + Utils.ByteToStringForLog(recBuffer));

                float value = 0;
                if (float.TryParse(Utils.ByteToString(recBuffer), out value))
                    parent.LightLx = value;

                DoSend(recBuffer);
                return recBuffer.Length;
            }

        }
        [XmlIgnore] public _Service service = null;

        string _ip = "0.0.0.0";
        public string SensorIP
        {
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
        int _port = 4004;
        public int Port
        {
            get { return _port; }
            set
            {
                if (_port != value)
                {
                    _port = value;
                    OnPropertyChange("Port");
                }
            }
        }
        float _lightLx = 0.0f;
        [XmlIgnore]
        public float LightLx
        {
            get { return _lightLx; }
            set
            {
                if (_lightLx != value)
                {
                    _lightLx = value;
                    OnPropertyChange("LightLx");
                }
            }
        }


        public SensorLight()
        {
            service = new _Service(this, "Light");
            server = new TCPServer();
        }

        public override void init()
        {
            base.init();
        }
        public override void terminate()
        {
            base.terminate();
        }

        public override void start()
        {
            service.start();
            server.Start(Port, service);

            base.start();
        }
        public override void stop(bool byError = false)
        {
            server.Stop();
            server.RemoveMe(service);
            service.stop();

            base.stop(byError);
        }
    }
}
