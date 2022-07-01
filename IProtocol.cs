using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBTPTempMoistMoni.Control
{
    //interface IProtocol
    //{
    //    bool safeRemainOut(int spent_msec);

    //    string remain();
    //    string[] parse(byte[] text, bool newLine = true);
    //    byte[] make(byte[] text);
    //}

    public abstract class Protocol// : IProtocol
    {
        public delegate void OnSend(Protocol sender, byte[] buffer, int offset, int count);    
        protected OnSend send = null;

        public abstract byte[] make(byte[] text);

        public abstract byte[] make(byte[] text, bool bSumUse);

        public abstract string[] parse(byte[] text, bool newLine = true);

        public abstract string remain();

        public abstract bool safeRemainOut(int spent_msec);
    }
}
