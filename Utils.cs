using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Serialization;

namespace CBTPTempMoistMoni.Common
{
    public class Utils
    {
        // 바이트 배열을 String으로 변환 
        public static string ByteToString(byte[] strByte)
        {
            //string str = Encoding.Default.GetString(strByte);
            //return str;
            StringBuilder sb = new StringBuilder();
            foreach (byte a in strByte)
            {
                if (a < 20)
                {
                    sb.Append("[").Append(a.ToString()).Append("]");
                }
                else
                    sb.Append((char)a);
            }
            return sb.ToString();
        }
        // String을 바이트 배열로 변환 
        public static byte[] StringToByte(string str)
        {
            byte[] StrByte = Encoding.UTF8.GetBytes(str);
            //byte[] StrByte = Encoding.Default.GetBytes(str);
            return StrByte;
            //byte[] ret = new byte[str.Length];
            //for (int i = 0; i < str.Length; i++)
            //    ret[i] = (char)str[i];
            //return ret;
        }
        public static string ByteToStringForLog(byte[] strByte, uint max_len = 20)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte a in strByte)
            {
                if (a < 20)
                {
                    sb.Append("[").Append(a.ToString()).Append("]");
                }
                else
                    sb.Append((char)a);
                if (sb.Length + 3 >= max_len)
                {
                    sb.Append("...");
                    break;
                }
            }
            return sb.ToString();
        }


        public static string GetRelativeFilePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            string ret = Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
            return ret;
        }
        public static string GetRelativeDirPath(string Dir, string folder)
        {
            try
            {
                Dir += "\\dummy";
                Uri pathUri = new Uri(Dir);
                // Folders must end in a slash
                if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    folder += Path.DirectorySeparatorChar;
                }
                Uri folderUri = new Uri(folder);
                string ret = Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
                ret = ret.Substring(0, ret.Length - "dummy".Length);
                int other_drive = ret.IndexOf(':');
                if (ret.Length == 0 || (other_drive < 0 && ret[0].Equals('.') == false))
                    ret = ".\\" + ret;
                return ret;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());  //  system 이 초기화 되기 전에 호출되면 발생
            }
            return ".\\";
        }
    }


    public class Http
    {
        public static string Get(string url)
        {
            string responseText = string.Empty;

            //HttpUtility.UrlEncode
            //encodeURIComponent

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = 2 * 1000; // 30초
            //request.Headers.Add("Authorization", "BASIC SGVsbG8="); // 헤더 추가 방법
            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)request.GetResponse())
                {
                    HttpStatusCode status = resp.StatusCode;
                    Console.WriteLine(status);  // 정상이면 "OK"

                    Stream respStream = resp.GetResponseStream();
                    using (StreamReader sr = new StreamReader(respStream))
                    {
                        responseText = sr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                responseText = ex.Message;
            }

            return responseText;
        }


        public static string request(string url)
        {
            StringBuilder ret = new StringBuilder();

            // Create the http request
            //const string Url = "http://www.microsoft.com";
            var webRequest = WebRequest.Create(url);
            webRequest.Timeout = 3 * 1000;  //  3 second

            try
            {
                // Send the http request and wait for the response
                var responseStream = webRequest.GetResponse().GetResponseStream();

                // Displays the response stream text
                if (responseStream != null)
                {
                    using (var streamReader = new StreamReader(responseStream))
                    {
                        // Return next available character or -1 if there are no characters to be read
                        while (streamReader.Peek() > -1)
                        {
                            ret.Append(streamReader.ReadLine() + Environment.NewLine);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ret.Append(ex.ToString());
            }
            return ret.ToString();
        }

        public static string request(string url, Dictionary<string, string> parameters)
        {
            StringBuilder sb = new StringBuilder();
            if (parameters != null)
            {
                foreach (string key in parameters.Keys)
                {
                    if (0 < sb.Length)
                    {
                        sb.Append("&");
                    }
                    string value = parameters[key];
                    if (value != null)
                    {
                        //  TODO : 특수문자 -> string을 변경.
                        sb.Append(key + "=" + value);
                    }
                }
            }
            return request(url + "?" + sb.ToString());
        }

        public static string reqeustPost()
        {
            StringBuilder ret = new StringBuilder();

            // Create the http request
            var webRequest = WebRequest.Create("https://fcm.googleapis.com/fcm/send");
            webRequest.Timeout = 1;

            try
            {
                // Send the http request and wait for the response
                var responseStream = webRequest.GetResponse().GetResponseStream();

                // Displays the response stream text
                if (responseStream != null)
                {
                    using (var streamReader = new StreamReader(responseStream))
                    {
                        // Return next available character or -1 if there are no characters to be read
                        while (streamReader.Peek() > -1)
                        {
                            ret.Append(streamReader.ReadLine() + Environment.NewLine);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ret.Append(ex.ToString());
            }
            return ret.ToString();
        }
    }


    public class Xml
    {
        public interface ITarget
        {
            void onXmlLoaded();
            void onXmlSaveing();
        }

        public static T load<T>(string path)
        {
            T ret = default(T);
            try
            {
                XmlSerializer mySerializer = new XmlSerializer(typeof(T));
                using (FileStream myFileStream = new FileStream(path, FileMode.Open))
                {
                    try
                    {
                        ret = (T)mySerializer.Deserialize(myFileStream);
                        ITarget target = ret as ITarget;
                        target.onXmlLoaded();
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.Print(e.StackTrace);
                        System.Diagnostics.Debug.Print(e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print(e.StackTrace);
                System.Diagnostics.Debug.Print(e.Message);
            }
            return ret;
        }

        public static void save<T>(String path, ITarget target)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (TextWriter writer = new StreamWriter(path))
                {
                    target.onXmlSaveing();
                    serializer.Serialize(writer, target);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print(e.ToString());
            }
        }
        public static void save(String path, ITarget target)
        {
            try
            {
                if (target != null)
                {
                    XmlSerializer serializer = new XmlSerializer(target.GetType());
                    using (TextWriter writer = new StreamWriter(path))
                    {
                        target.onXmlSaveing();
                        serializer.Serialize(writer, target);
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print(e.ToString());
            }
        }


        public static string SerializeToString<T>(object objectInstance)
        {
            var serializer = new XmlSerializer(typeof(T));
            var sb = new StringBuilder();
            using (TextWriter writer = new StringWriter(sb))
            {
                serializer.Serialize(writer, objectInstance);
            }

            return sb.ToString();
        }
        public static string SerializeToString(object objectInstance)
        {
            var serializer = new XmlSerializer(objectInstance.GetType());
            var sb = new StringBuilder();
            using (TextWriter writer = new StringWriter(sb))
            {
                serializer.Serialize(writer, objectInstance);
            }

            return sb.ToString();
        }

        public static T DeserializeFromString<T>(string objectData)
        {
            return (T)DeserializeFromString(objectData, typeof(T));
        }

        public static object DeserializeFromString(string objectData, Type type)
        {
            var serializer = new XmlSerializer(type);
            object result;

            using (TextReader reader = new StringReader(objectData))
            {
                result = serializer.Deserialize(reader);
            }

            return result;
        }
    }
}
