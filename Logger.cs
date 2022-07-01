using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CBTPTempMoistMoni.Common
{
    public interface ILogger
    {
        void log(string tag, string message);
        public void error(string message);
        public void error(Exception e);
        public void warn(string message);
        public void info(string message);
        public void debug(string message);
        public void save(string message);
    }

    public class FileLogger : ILogger
    {
        public void debug(string message)
        {
            throw new NotImplementedException();
        }

        public void error(string message)
        {
            throw new NotImplementedException();
        }

        public void error(Exception e)
        {
            throw new NotImplementedException();
        }

        public void info(string message)
        {
            throw new NotImplementedException();
        }

        public void log(string tag, string message)
        {
            throw new NotImplementedException();
        }

        public void warn(string message)
        {
            throw new NotImplementedException();
        }
        public void save(string message)
        {
            throw new NotImplementedException();
        }

    }

    public static class Logger
    {
        //static string logfile = AppDomain.CurrentDomain.BaseDirectory + "log_" + DateTime.Now.ToLongDateString() + ".txt";
        static string logfile = "./log/log_" + DateTime.Now.ToLongDateString() + ".txt";
        static string savelogfile = "./log/Data_" + DateTime.Now.Year + ".txt";

        public static string ERROR = "E";
        public static string WARN = "W";
        public static string INFO = "I";
        public static string DEBUG = "D";
        public static string SAVE = "S";

        static ManagedThread workerThread = null;
        static ManagedThread workerThread2 = null;

        static ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
        static ConcurrentQueue<string> queue2 = new ConcurrentQueue<string>();


        #region Log writer thread 
        static bool DoWork(UInt64 called_count)
        {
            if (queue.IsEmpty == false)
            {
                string log;

                StreamWriter sw = new StreamWriter(logfile, true);

                string sPattern = "[S]";

                while (queue.TryDequeue(out log))
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(log, sPattern))
                    {
                        Console.WriteLine(log);
                        sw.WriteLine(log);
                    }
                }
                sw.Close();
            }
            return true;
        }

        static bool DoSave(UInt64 called_count)
        {
            if (queue2.IsEmpty == false)
            {
                string log;

                StreamWriter sw = new StreamWriter(savelogfile, true);

                string sPattern = "[S]";

                while (queue2.TryDequeue(out log))
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(log, sPattern))
                    {
                        Console.WriteLine(log);
                        sw.WriteLine(log);
                    }
                }
                sw.Close();
            }
            return true;
        }

        public static void start()
        {
            string sDirName = "./log";
            Directory.CreateDirectory(sDirName);

            if (workerThread == null)
            {
                workerThread = new ManagedThread("Logger", DoWork);
                workerThread.Start();
            }

            if (workerThread2 == null)
            {
                workerThread2 = new ManagedThread("Logger", DoSave);
                workerThread2.Start();
            }
        }

        public static void finish()
        {
            if (workerThread != null)
            {
                workerThread.Stop();
                workerThread.Join();

                workerThread = null;
            }

            if (workerThread2 != null)
            {
                workerThread2.Stop();
                workerThread2.Join();

                workerThread2 = null;
            }
        }

        #endregion

        public static void log(string tag, string message)
        {
            string log = DateTime.Now.ToString() + " [" + tag + "] : " + message;
            if (workerThread != null)
            {
                queue.Enqueue(log);
                log = "." + log;
            }
            else
            {
                StreamWriter sw = new StreamWriter(logfile, true);
                sw.WriteLine(log);
                sw.Flush();
                sw.Close();
            }

            System.Diagnostics.Debug.Print(log);
        }

        public static void log2(string tag, string message)
        {
            string log = DateTime.Now.ToString() + " [" + tag + "] : " + message;
            if (workerThread2 != null)
            {
                queue2.Enqueue(log);
                log = "." + log;
            }
            else
            {
                StreamWriter sw = new StreamWriter(logfile, true);
                sw.WriteLine(log);
                sw.Flush();
                sw.Close();
            }

            System.Diagnostics.Debug.Print(log);
        }


        public static void error(string message)
        {
            log(ERROR, message);
        }
        public static void error(Exception e)
        {
            log(ERROR, e.Message);
            System.Diagnostics.Debug.Print(e.StackTrace);
        }

        public static void warn(string message)
        {
            log(WARN, message);
        }

        public static void info(string message)
        {
            log(INFO, message);
        }

        public static void debug(string message)
        {
            log(DEBUG, message);
        }

        public static void save(string message)
        {
            log2(SAVE, message);
        }

        public static void WriteErrorLog(Exception ex)
        {
            log(ERROR, ex.Source.ToString() + "; " + ex.Message.ToString().Trim());
        }

        public static void WriteErrorLog(string message)
        {
            log(ERROR, message);
        }
    }
}
