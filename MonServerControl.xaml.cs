using CBTPTempMoistMoni.Common;
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
    /// MonServerControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MonServerControl : UserControl
    {
        public MonServerControl()
        {
            InitializeComponent();
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            MonServer server = this.DataContext as MonServer;
            if (server != null)
            {
                server.OnLog += Li_OnLog;
            }
        }

        private void Li_OnLog(object sender, BaseModel.LogEventArg arg)
        {
            lsLog.print(sender, arg);
        }
    }


    public class MonServer : BaseModel
    {
        public string MainServer { get; set; } = "127.0.0.1";
        public string Pos { get; set; } = "PS_1_1";

        private string _Status = "Idle";
        [XmlIgnore] public string Status 
        {
            get { return _Status; } 
            set
            {
                if (_Status != value)
                {
                    _Status = value;
                    OnPropertyChange("Status");
                }
            }
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
            base.start();
        }
        public override void stop(bool byError = false)
        {
            base.stop(byError);
        }

        public void upload(string pos, float temp, float moisture, float co2ppm, float lightLx)
        {
            Status = "Update Data";
            string data = String.Format("Pos={0}&Temp={1:F2}&Mois={2:F2}&Co2={3:F2}&Light={4:F2}", 
                pos, temp, moisture, co2ppm, lightLx);
            string http_url = "http://" + MainServer + "/app/smartfarm/set_monitor.asp?admin=RaonSMF&" + data;
            PrintLog("I", data);
            System.Diagnostics.Debug.Print(http_url);

            Http.request(http_url);
        }
    }
}
