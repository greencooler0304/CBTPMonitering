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
using System.Windows.Shapes;

namespace CBTPTempMoistMoni.Simulate
{
    /// <summary>
    /// SimulateWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SimulateWindow : Window
    {
        internal CBTPTempMoist collector = null;

        public SimulateWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = new Simulator(collector);
        }
    }



    class Simulator
    {
        public Sensor sensorTemp { get; set; } = new Sensor("Temperature");
        public Sensor sensorMois { get; set; } = new Sensor("Moisture");
        //public Sensor sensorCo2 { get; set; } = new Sensor("Co2");
        //public Sensor sensorLight { get; set; } = new Sensor("Light");

        public Simulator(CBTPTempMoist collector = null)
        {
            if (collector != null)
            {
                //sim.sensorTemp.IP = "127.0.0.1";
                //sensorTemp.Port = collector.temperature.Port;
                //sensorMois.Port = collector.moisture.Port;
                //sensorCo2.Port = collector.co2.Port;
                //sensorLight.Port = collector.light.Port;
            }
        }
    }

}
