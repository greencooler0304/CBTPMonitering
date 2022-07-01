#define APPEND_LOG
//#define USE_MUTEX
using CBTPTempMoistMoni.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace CBTPTempMoistMoni.Control
{
    /// <summary>
    /// LogContro1.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LogControl : UserControl, ILogger
    {
        static List<LogControl> instances = new List<LogControl>();
        static string BaseDir = ".";
        public static void SetBaseDir(string dir)
        {
            if (dir != null && 0 < dir.Length && System.IO.Directory.Exists(dir))
            {
                BaseDir = dir;
                foreach (LogControl c in instances)
                {
                    try
                    {
                        c.OnChangedBaseDir();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Print(ex.ToString());
                    }
                }
            }
        }




        public enum DateType {simple, full }
        public DateType dateType = DateType.simple;

        ObservableCollection<string> logs = new ObservableCollection<string>();
        string logfilename = null;
        string logfilePath = null;
#if USE_MUTEX
        Mutex log_mutex = new Mutex();
#else
        bool is_in = false;
#endif
#if APPEND_LOG
#else
        System.IO.StreamWriter log_writer = null;
#endif

        public LogControl()
        {
            instances.Add(this);

            InitializeComponent();

            lsLog.ItemsSource = logs;

            DataContext = new ConfigColors();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
#if APPEND_LOG
#else
            if (log_writer != null)
                log_writer.Close();
#endif
        }

        void OnChangedBaseDir()
        {
            //log_mutex.WaitOne();
            logfilePath = BaseDir + "\\" + logfilename;
            //log_mutex.ReleaseMutex();
        }
        public void setLogFile(string filename)
        {
            string date = "";
            logfilename = filename + DateTime.Now.ToString(".yyyMMdd_HHmmss.") + "log";
            logfilePath = BaseDir + "\\log\\" + logfilename;
#if APPEND_LOG
#else
            try
            {
                log_writer = System.IO.File.AppendText(logfilename);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
                log_writer = null;
            }
#endif
        }


        public void print(object sender, BaseModel.LogEventArg arg)
        {
            string log = (arg.tag == null || arg.tag.Length == 0 ? " " : "[" + arg.tag + "] ") + arg.log;
            print(log, arg.add);
        }

        public void print(string data, bool addList = true)
        {
#if USE_MUTEX
            log_mutex.WaitOne();
#else
            //if (is_in)
            //    return;
            //is_in = true;
#endif
            this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
            {
                string log = "";
                switch (dateType)
                {
                    case DateType.simple:
                        log = DateTime.Now.ToString("HH:mm:ss ") + data;
                        break;
                    case DateType.full:
                        log = DateTime.Now.ToString("yy.MM.dd HH:mm:ss ") + data;
                        break;
                }
                txCurLog.Text = log;
                if (addList)
                {
                    bool gotoEnd = lsLog.SelectedIndex == lsLog.Items.Count - 1;

                    logs.Add(log);
#if APPEND_LOG
                    if (logfilePath != null && logfilename != null)
                    {
                        try
                        {
                            using (System.IO.StreamWriter log_writer = System.IO.File.AppendText(logfilePath))
                            {
                                log_writer.WriteLine(log);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.Print(ex.ToString());
                        }
                    }
#else
                    if (log_writer != null)
                    {
                        log_writer.WriteLine(log);
                        if (logs.Count % 10 == 0)
                            log_writer.Flush();
                    }
#endif
                    if (gotoEnd && lsLog.IsLoaded)
                    {
                        //lsLog.UpdateLayout();
                        lsLog.SelectedIndex = lsLog.Items.Count - 1;
                        //lsLog.ScrollIntoView(lsLog.Items[lsLog.Items.Count - 1]);
                        try
                        {
                            Border border = (Border)VisualTreeHelper.GetChild(lsLog, 0);
                            if (border != null)
                            {
                                ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                                //scrollViewer.ScrollToBottom();
                                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + 3);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.Print(ex.ToString());
                        }
                    }
                }
            });
            //  Async
            //Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
            //{                
            //}

#if USE_MUTEX
            log_mutex.ReleaseMutex();
#else
            //is_in = false;
#endif
        }

        public void printSerial(object sender, BaseModel.LogSerialEventArg arg)
		{
            printSerial(arg.tag, arg.log);
        }

        public void printSerial(string port, string log)
        {
            string filename = port + "_serial.log";
            string filePath = BaseDir + "\\" + filename;

            this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
            {
                string data = DateTime.Now.ToString("yy.MM.dd HH:mm:ss ") + log;

                if (filePath != null && filename != null)
                {
                    try
                    {
                        using (System.IO.StreamWriter log_writer = System.IO.File.AppendText(filePath))
                        {
                            log_writer.WriteLine(data);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Print(ex.ToString());
                    }
                }
            });
        }

        public void printState(object sender, BaseModel.LogStateEventArg arg)
		{
            printState(arg.tag, arg.log);
        }

        public void printState(string dut, string log)
		{
            string filename = dut + "_state.log";
            string filePath = BaseDir + "\\" + filename;

            this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
            {
                string data = DateTime.Now.ToString("yy.MM.dd HH:mm:ss ") + log;

                if (filePath != null && filename != null)
                {
                    try
                    {
                        using (System.IO.StreamWriter log_writer = System.IO.File.AppendText(filePath))
                        {
                            log_writer.WriteLine(data);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Print(ex.ToString());
                    }
                }
            });
        }

        private void LsLog_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            logs.Clear();
            this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
            {
                //lsLog.Items.Clear();
            });
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //log_mutex.WaitOne();

            lsLog.SelectedIndex = lsLog.Items.Count - 1;
            Border border = (Border)VisualTreeHelper.GetChild(lsLog, 0);
            if (border != null)
            {
                ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                scrollViewer.ScrollToBottom();
            }
            
            //log_mutex.ReleaseMutex();
        }


        public void log(string tag, string message)
        {            
            print("[" + tag + "] " + message);
        }

        public void error(string message)
        {
            print("[E] " + message);
        }

        public void error(Exception ex)
        {
            print("[E] " + ex.Source.ToString() + "; " + ex.Message.ToString().Trim());
        }

        public void warn(string message)
        {
            print("[W] " + message);
        }

        public void info(string message)
        {
            print("[I] " + message);
        }

        public void debug(string message)
        {
            print("[D] " + message);
        }

        public void save(string message)
        {
            print("[S] " + message);
        }
    }


    public class ListBoxBehavior
    {
        static readonly Dictionary<ListBox, Capture> Associations =
               new Dictionary<ListBox, Capture>();

        public static bool GetScrollOnNewItem(DependencyObject obj)
        {
            return (bool)obj.GetValue(ScrollOnNewItemProperty);
        }

        public static void SetScrollOnNewItem(DependencyObject obj, bool value)
        {
            obj.SetValue(ScrollOnNewItemProperty, value);
        }

        public static readonly DependencyProperty ScrollOnNewItemProperty =
            DependencyProperty.RegisterAttached(
                "ScrollOnNewItem",
                typeof(bool),
                typeof(ListBoxBehavior),
                new UIPropertyMetadata(false, OnScrollOnNewItemChanged));

        public static void OnScrollOnNewItemChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var listBox = d as ListBox;
            if (listBox == null) return;
            bool oldValue = (bool)e.OldValue, newValue = (bool)e.NewValue;
            if (newValue == oldValue) return;
            if (newValue)
            {
                listBox.Loaded += ListBox_Loaded;
                listBox.Unloaded += ListBox_Unloaded;
                var itemsSourcePropertyDescriptor = TypeDescriptor.GetProperties(listBox)["ItemsSource"];
                itemsSourcePropertyDescriptor.AddValueChanged(listBox, ListBox_ItemsSourceChanged);
            }
            else
            {
                listBox.Loaded -= ListBox_Loaded;
                listBox.Unloaded -= ListBox_Unloaded;
                if (Associations.ContainsKey(listBox))
                    Associations[listBox].Dispose();
                var itemsSourcePropertyDescriptor = TypeDescriptor.GetProperties(listBox)["ItemsSource"];
                itemsSourcePropertyDescriptor.RemoveValueChanged(listBox, ListBox_ItemsSourceChanged);
            }
        }

        private static void ListBox_ItemsSourceChanged(object sender, EventArgs e)
        {
            var listBox = (ListBox)sender;
            if (Associations.ContainsKey(listBox))
                Associations[listBox].Dispose();
            Associations[listBox] = new Capture(listBox);
        }

        static void ListBox_Unloaded(object sender, RoutedEventArgs e)
        {
            var listBox = (ListBox)sender;
            if (Associations.ContainsKey(listBox))
                Associations[listBox].Dispose();
            listBox.Unloaded -= ListBox_Unloaded;
        }

        static void ListBox_Loaded(object sender, RoutedEventArgs e)
        {
            var listBox = (ListBox)sender;
            var incc = listBox.Items as INotifyCollectionChanged;
            if (incc == null) return;
            listBox.Loaded -= ListBox_Loaded;
            Associations[listBox] = new Capture(listBox);
        }

        class Capture : IDisposable
        {
            private readonly ListBox listBox;
            private readonly INotifyCollectionChanged incc;

            public Capture(ListBox listBox)
            {
                this.listBox = listBox;
                incc = listBox.ItemsSource as INotifyCollectionChanged;
                if (incc != null)
                {
                    incc.CollectionChanged += incc_CollectionChanged;
                }
            }

            void incc_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    listBox.ScrollIntoView(e.NewItems[0]);
                    listBox.SelectedItem = e.NewItems[0];
                }
            }

            public void Dispose()
            {
                if (incc != null)
                    incc.CollectionChanged -= incc_CollectionChanged;
            }
        }
    }
}
