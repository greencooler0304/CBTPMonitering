using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBTPTempMoistMoni.Control
{

    // 22.06 - 충북TP 온습도 Chamber(위지스) 기기의 TEMI1000 컨트롤러 PCLINK 프로토콜 
    public class PcLinkProtocol : Protocol
    {
        StringBuilder rx_sb = null;
        byte last_char = (byte)0;
        byte rx_bcc = (byte)0;

        StringBuilder rx_error = new StringBuilder();

        public override bool safeRemainOut(int spent_msec)
        {
            return false;
        }

        public override string ToString()
        {
            return "STX P.";
        }

        public string parse(byte achar, bool bSumUse)
        {
            string ret = null;

            switch (achar)
            {
                case (byte)0x02:
                    if (rx_error.Length > 0)
                    {
                        ret = "NoParsing : " + rx_error.ToString();
                        rx_error.Clear();
                    }
                    rx_sb = new StringBuilder();
                    last_char = achar;
                    break;

                case (byte)0x0d:
                    if (rx_sb != null)
                    {
                        last_char = achar;
                    }
                    break; 

                case (byte)0x0A:
                    if (rx_sb != null & last_char == 0x0D)
                    {
                        string recvData = rx_sb.ToString();
                        string[] parsingDatas = recvData.Split(',');

                        int SumIndex = parsingDatas.Length;

                        if (bSumUse)
                        {
                            byte[] arrSumBytes = Encoding.UTF8.GetBytes(parsingDatas[SumIndex - 1].Substring(4, 2));

                            byte SumData; 

                            for(int i = 0; i < 2; i++)
                            {
                                byte byTemp; 
                                
                                if (arrSumBytes[i] >= 65)       byTemp = (byte)(arrSumBytes[i] - 0x37);
                                else                            byTemp = (byte)(arrSumBytes[i] - 0x30);

                                if(i == 0)                      byTemp = (byte)(byTemp << 4);

                                arrSumBytes[i] = byTemp;
                            }

                            SumData = (byte)(arrSumBytes[0] + arrSumBytes[1]);

                            for(int i = 0; i < rx_sb.Length - 2; i++)
                            {
                                rx_bcc += (byte)rx_sb[i];
                            }

                            if (rx_bcc != SumData)
                            {
                                rx_sb.Append(" - parity error. " + ((int)SumData).ToString("X2") + " != " + rx_bcc.ToString("X2"));
                            }
                        }

                        //OK, NG 체크 
                        if(parsingDatas[1] == "OK")
                        {
                            parsingDatas[SumIndex - 1] = parsingDatas[SumIndex - 1].Substring(0, 4);

                            foreach (string strData in parsingDatas)
                            {
                                ret += strData;
                                ret += ',';
                            }

                            // ',' 제거
                            ret = ret.Substring(0, ret.Length - 1);
                        }
                        else if (parsingDatas[1] == "NG")
                        {
                            rx_sb.Append(" - MSG NG : ERROR CODE => " + parsingDatas[2]);
                            ret = rx_sb.ToString();
                        }

                        rx_sb = null;
                        rx_bcc = (byte)0;
                    }
                    break;

                default:
                    if (rx_sb != null)
                    {
                        last_char = achar;
                        rx_sb.Append((char)last_char);
                    }
                    else
                    {
                        rx_error.Append(" ").Append(((int)achar).ToString("X2")).Append("[" + achar + "]");
                    }
                    break;
            }

            return ret;
        }

        public override string[] parse(byte[] text, bool newLine)
        {
            List<string> ret = null;
            string apacket = null;

            if (text != null && text.Length > 0)
            {
                foreach (byte achar in text)
                {
                    apacket = parse(achar, true);

                    if (apacket != null)
                    {
                        if (ret == null)
                            ret = new List<string>();

                        if (newLine)
                            ret.Add(Environment.NewLine);

                        ret.Add(apacket);
                    }
                }        
            }
            return ret == null ? new string[0] : ret.ToArray();
        }

        public override string remain()
        {
            return "";
        }

        public override byte[] make(byte[] text)
        {
            List<byte> sb = new List<byte>();

            return sb.ToArray();
        }

        public override byte[] make(byte[] text, bool bSumUse)
        {
            List<byte> sb = new List<byte>();
            int sum = (byte)0;

            sb.Add(0x02);  //  STX

            // CheckSum은 STX 이후 부터 CR 전까지 더한 값의 하위 바이트 값을 
            // 2 byte Char 형태로 표현
            foreach (byte a in text)
            {
                sum += a;
                sb.Add(a);
            }

            if(bSumUse)
            {
                //상위비트 
                byte upperBit = (byte)(sum & 0xf0);

                upperBit = (byte)(upperBit >> 4); 

                if(upperBit <=10)    upperBit = (byte)(upperBit + 0x30); 
                else                 upperBit = (byte)(upperBit + 0x37);

                sb.Add((byte)upperBit);

                //하위비트 
                byte lowerBit = (byte)(sum & 0x0f);

                if (lowerBit <= 10) lowerBit = (byte)(lowerBit + 0x30);
                else                lowerBit = (byte)(lowerBit + 0x37);

                sb.Add((byte)lowerBit);
            }

            sb.Add((byte)0x0D); //  CR
            sb.Add((byte)0x0A); //  LF

            return sb.ToArray();
        }
    }
}
