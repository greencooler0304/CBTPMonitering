using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CBTPTempMoistMoni.Common
{
    class ManagedThread
    {
        public delegate bool doWork(UInt64 called_count);
        public delegate bool doWorkParam(UInt64 called_count, Object param);

        private Thread _thread = null;
        private string _name = null;
        private UInt64 _roofs = 0;
        private doWork _doWork = null;
        private doWorkParam _doWorkParam = null;
        private bool running = true;
        private int _sleep = 10;


        #region Manager

        private static ConcurrentBag<ManagedThread> _threads = new ConcurrentBag<ManagedThread>();

        private static void AddThis(ManagedThread _this)
        {
            _threads.Add(_this);
            Logger.info("[ThreadManager] add : " + _this._name);
        }

        private static void RemoveThis(ManagedThread _this)
        {
            if (_threads != null)
            {
                if (_threads.TryTake(out _this))
                {
                    Logger.info("[ThreadManager] remove : " + _this._name);
                }
                else
                {
                    Logger.info("[ThreadManager] don't remove : " + _this._name);
                }
            }
        }

        #endregion



        public ManagedThread(String name, doWork start)
        {
            _name = name;
            _doWork = start;
            _thread = new Thread(new ThreadStart(this.RunThread));
            AddThis(this);
        }
        public ManagedThread(String name, doWorkParam start)
        {
            _name = name;
            _doWorkParam = start;
            _thread = new Thread(new ParameterizedThreadStart(this.RunThread));
            AddThis(this);
        }
        public ManagedThread(String name, doWork start, int sleep)
        {
            _name = name;
            _doWork = start;
            _thread = new Thread(new ThreadStart(this.RunThread));
            AddThis(this);
            _sleep = 0 < sleep && sleep < 100000 ? sleep : _sleep;
        }


        ~ManagedThread()
        {
        }

        // Thread methods / properties
        public void Start() { _thread.Start(); }
        public void Join() { _thread.Join(); }
        public bool IsAlive { get { return _thread.IsAlive; } }
        public void Stop()
        {
            Logger.info("[ThreadManager] Stop : " + _name);
            running = false;
            Join();
            Logger.info("[ThreadManager] Stopped : " + _name);
        }

        public Thread GetThread()
        {
            return _thread;
        }

        // Override in base class
        public void RunThread()
        {
            if (_doWork != null)
            {
                Logger.info("[ThreadManager] start : " + _name);
                while (running)
                {
                    if (_doWork(_roofs) == false)
                    {
                        break;
                    }
                    _roofs++;
                    Thread.Sleep(_sleep);
                }
                Logger.info("[ThreadManager] end : " + _name);
            }
            RemoveThis(this);
        }
        public void RunThread(Object param)
        {
            if (_doWorkParam != null)
            {
                Logger.info("[ThreadManager] start : " + _name);
                while (running)
                {
                    if (_doWorkParam(_roofs, param) == false)
                    {
                        break;
                    }
                    _roofs++;
                    Thread.Sleep(_sleep);
                }
                Logger.info("[ThreadManager] end : " + _name);
            }
            RemoveThis(this);
        }
    }
}
