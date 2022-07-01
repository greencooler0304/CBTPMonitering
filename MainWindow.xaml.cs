using CBTPTempMoistMoni.Common;
using CBTPTempMoistMoni.Control;
using CBTPTempMoistMoni.Simulate;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CBTPTempMoistMoni
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static CBTPTempMoist cbtpTempMoist;
        static SimulateWindow simulateWindow;

        public MainWindow()
        {
            InitializeComponent();
            loadModule();
        }

        void loadModule()
        {
            cbtpTempMoist = Xml.load<CBTPTempMoist>("./config.xml");
            if (cbtpTempMoist == null)
            {
                cbtpTempMoist = new CBTPTempMoist();
                SaveModule();

                cbtpTempMoist.onXmlLoaded();
            }
        }
        void SaveModule()
        {
            Xml.save("./config.xml", cbtpTempMoist);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.start();

            cbtpTempMoist.init();
            this.DataContext = cbtpTempMoist;
#if debug
            simulateWindow = new SimulateWindow();
            simulateWindow.collector = raonCollector;
            simulateWindow.Left = this.Left + this.Width;
            simulateWindow.Top = this.Top;
            simulateWindow.Height = this.Height;
            simulateWindow.Show();
#endif
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.DataContext = null;
            cbtpTempMoist.terminate();
            SaveModule();

            Logger.finish();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            cbtpTempMoist.stop();
#if debug
            simulateWindow.Close();
#endif
        }
    }




    public class CBTPTempMoist : BaseModel, Xml.ITarget
    {
        public Project project { get; set; } = new Project();
        //public SensorCO2 co2 { get; set; } = new SensorCO2();
        //public SensorLight light { get; set; } = new SensorLight();
        //public SensorMoisture moisture { get; set; } = new SensorMoisture();
        public SensorTemp temperature { get; set; } = new SensorTemp();
        public MonServer monServer { get; set; } = new MonServer();

        public int uploadTimeMs { get; set; } = 1000;

        private System.Timers.Timer timer = null;

        public CBTPTempMoist()
        {
        }


        public void onXmlLoaded()
        {            
        }

        public void onXmlSaveing()
        {            
        }

        public override void init()
        {
            clearChild();

            addChild(project);
            //addChild(co2);
            //addChild(light);
            //addChild(moisture);
            addChild(temperature);
            //addChild(monServer);

            base.init();
        }

        public override void terminate()
        {
            base.terminate();

            clearChild();
        }


        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //monServer.upload(
            //    monServer.Pos,
            //    temperature.Temp,
            //    moisture.Moisture,
            //    0,
            //    0
            //    //co2.Co2ppm,
            //    //light.LightLx
            //    );
        }
        private void startTimer()
        {
            timer = new System.Timers.Timer(uploadTimeMs);
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = true;
            timer.Enabled = true;
        }
        private void stopTime()
        {
            timer?.Stop();
            timer = null;
        }



        protected override void Model_Started(object sender, EventArgs arg)
        {
            if (sender == project)
            {
                //co2.start();
                //light.start();
                //moisture.start();
                temperature.start();
                //monServer.start();

                startTimer();
            }

            base.Model_Started(sender, arg);
        }
        protected override void Model_Finshed(object sender, EventArgs arg)
        {
            if (sender == project)
            {
                stopTime();

                //co2.stop();
                //light.stop();
                //moisture.stop();
                temperature.stop();
                //monServer.stop();
            }
            base.Model_Finshed(sender, arg);
        }
        protected override void Model_StatusChanged(object sender, StatuseEventArg arg)
        {
            base.Model_StatusChanged(sender, arg);
        }
    }
}
