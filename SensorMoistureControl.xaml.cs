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
    /// SensorMoistureControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SensorMoistureControl : UserControl
    {
        public SensorMoistureControl()
        {
            InitializeComponent();
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SensorMoisture sensor = this.DataContext as SensorMoisture;
            if (sensor != null)
            {
                sensor.server.setLogger(lsLog);
                sensor.service.SetLogger(lsLog);
            }
        }
    }



    public class SensorMoisture : BaseModel
    {
        [XmlIgnore] public TCPServer server = null;
        public class _Service : Service
        {
            SensorMoisture parent;
            public _Service(SensorMoisture parent, string name) : base(name)
            {
                this.parent = parent;
            }

            public override int OnReceive(byte[] recBuffer)
            {
                Logger?.info("Received : " + Utils.ByteToStringForLog(recBuffer));

                float value = 0;
                if (float.TryParse(Utils.ByteToString(recBuffer), out value))
                    parent.Moisture = value;

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
        int _port = 4002;
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
        float _moisture = 0.0f;
        [XmlIgnore] public float Moisture
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


        public SensorMoisture()
        {
            service = new _Service(this, "Moisture");
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
