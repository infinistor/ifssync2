/*
* Copyright (c) 2021 PSPACE, inc. KSAN Development Team ksan@pspace.co.kr
* KSAN is a suite of free software: you can redistribute it and/or modify it under the terms of
* the GNU General Public License as published by the Free Software Foundation, either version 
* 3 of the License.  See LICENSE for details
*
* 본 프로그램 및 관련 소스코드, 문서 등 모든 자료는 있는 그대로 제공이 됩니다.
* KSAN 프로젝트의 개발자 및 개발사는 이 프로그램을 사용한 결과에 따른 어떠한 책임도 지지 않습니다.
* KSAN 개발팀은 사전 공지, 허락, 동의 없이 KSAN 개발에 관련된 모든 결과물에 대한 LICENSE 방식을 변경 할 권리가 있습니다.
*/
using CefSharp.Wpf;
using IfsSync2UI;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IfsSync2Data
{
    #region FileIcon
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SHFileInfo
    {
        public IntPtr hIcon;

        public int iIcon;

        public uint dwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    };
    public enum IconSize : short
    {
        Small,
        Large
    }
    public enum ItemType : short
    {
        Folder,
        File
    }
    public enum ItemState : short
    {
        Undefined,
        Open,
        Close
    }
    #endregion

    static class Utility
    {
        public static void ErrorMessageBox(string Msg, string Title)
        {
            MessageBox.Show(Msg, Title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }

        public static bool GetVolumeSize(string URL, out long Total, out long Used)
        {
            try
            {
                string url = URL + MainData.CURL_GET_S3_VOLUME_SIZE;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = MainData.CURL_STR_GET_METHOD;
                request.ContentType = MainData.CURL_STR_CONTENTTYPE;
                request.Timeout = MainData.CURL_TIMEOUT_DELAY;
                ServerCertificateValidationCallback();

                // Response 처리
                using (WebResponse resp = request.GetResponse())
                {
                    long.TryParse(resp.Headers[MainData.CURL_GET_S3_VOLUME_TOTAL_SIZE], out Total);
                    long.TryParse(resp.Headers[MainData.CURL_GET_S3_VOLUME_USED_SIZE], out Used);
                }
            }
            catch {
                Total = Used = 0;
                return false;
            }
            return true;
        }

        public static void ServerCertificateValidationCallback()
        {
            if (ServicePointManager.ServerCertificateValidationCallback == null)
            {
                ServicePointManager.ServerCertificateValidationCallback +=
                delegate ( Object obj, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors ) { return true; };
            }
        }

        public static bool FileNameSpecialCharactersErrorCheck(string FileName)
        {
            char[] SpecialCharacters = {'\\', '/', ':', '*', '?', '"', '<', '>', '|' };

            foreach (char Special in SpecialCharacters) if (FileName.IndexOf(Special) >= 0) return true;
            return false;
        }

        public static bool ExtensionCharactersErrorCheck(string FileName)
        {
            char[] SpecialCharacters = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

            foreach (char Special in SpecialCharacters) if (FileName.IndexOf(Special) >= 0) return true;
            return false;
        }

        /************************* Get Global User **********************************/

        private static readonly string STR_IP = "ip";
        private static readonly string STR_HOSTNAME = "hostname";
        private static readonly string STR_PORT = "port";
        private static readonly string STR_MAC = "mac";
        private static readonly string STR_OS = "os";
        private static readonly string STR_GROUP = "group";
        private static readonly string STR_PCNAME = "pcName";


        private static readonly int PORT_NUMBER = 58443;

        private static readonly string STR_ERR_MSG = "err_msg";
        private static readonly string STR_RET = "ret";

        private static readonly string STR_USERID = "userid";
        private static readonly string STR_S3PROXY = "s3proxy";
        private static readonly string STR_ACCESS_KEY = "access_key";
        private static readonly string STR_ACCESS_SECRET = "access_secret";
        private static readonly string STR_TENANT_KEY = "tenant";
        //private static readonly string STR_TENANT_SECRET = "tenant_secret";

        private static string GetMacAddress()
        {
            return NetworkInterface.GetAllNetworkInterfaces()[0].GetPhysicalAddress().ToString();
        }
        private static string GetIPAddress()
        {
            string IP = string.Empty;
            IPAddress[] Host = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (var Item in Host)
            {
                if (Item.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    IP = Item.ToString();
                    break;
                }
            }
            return IP;
        }
        public static UserData GetGlobalUser(string _URL, string PcName)
        {
            try
            {
                string URL;
                if (_URL.EndsWith("/")) URL = _URL + MainData.WATCHER_SERVICE_GET_USER;
                else URL = _URL + "/" + MainData.WATCHER_SERVICE_GET_USER;

                OperatingSystem os = Environment.OSVersion;
                string data = string.Format("{{" +
                                "\"{0}\":\"{1}\", " +
                                "\"{2}\":\"{3}\", " +
                                "\"{4}\":\"{5}\", " +
                                "\"{6}\":\"{7}\", " +
                                "\"{8}\":\"{9}\", " +
                                "\"{10}\":\"{11}\"," +
                                "\"{12}\":\"{13}\"}}",
                                STR_IP, GetIPAddress(),
                                STR_HOSTNAME, Dns.GetHostName(),
                                STR_PORT, PORT_NUMBER,
                                STR_MAC, GetMacAddress(),
                                STR_OS, os.Platform.ToString().ToUpper().Substring(0, 3),
                                STR_GROUP, "0",
                                STR_PCNAME, PcName);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
                request.Method = MainData.CURL_STR_POST_METHOD;
                request.ContentType = MainData.CURL_STR_CONTENTTYPE;
                request.Timeout = MainData.CURL_TIMEOUT_DELAY;
                //Error evasion
                ServerCertificateValidationCallback();
                // POST할 데이타를 Request Stream에 쓴다
                byte[] bytes = Encoding.ASCII.GetBytes(data);
                request.ContentLength = bytes.Length; // 바이트수 지정

                using (Stream reqStream = request.GetRequestStream())
                {
                    reqStream.Write(bytes, 0, bytes.Length);
                }

                // Response 처리
                string responseText = string.Empty;
                using (WebResponse resp = request.GetResponse())
                {
                    Stream respStream = resp.GetResponseStream();
                    using (StreamReader sr = new StreamReader(respStream)) { responseText = sr.ReadToEnd(); };
                };

                JObject UserObj = JObject.Parse(responseText);


                if (!int.TryParse(UserObj[STR_RET].ToString(), out int ret))
                {
                    throw new Exception(STR_RET + " is Not int");
                }
                if (ret != 0)
                {
                    string ErrorMsg = UserObj[STR_ERR_MSG].ToString();
                    throw new Exception(ErrorMsg);
                }

                //Get User Data
                UserData User = new UserData()
                {
                    URL = UserObj[STR_S3PROXY].ToString(),
                    UserName = UserObj[STR_USERID].ToString(),
                    AccessKey = UserObj[STR_ACCESS_KEY].ToString(),
                    AccessSecret = UserObj[STR_ACCESS_SECRET].ToString(),
                    StorageName = UserObj[STR_TENANT_KEY].ToString(),
                    Debug = false
                };

                return User;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        public static UserData GetGlobalUser(string _URL, string IP, string HostName, string MAC, string PcName)
        {
            try
            {
                string URL;
                if (_URL.EndsWith("/")) URL = _URL + MainData.WATCHER_SERVICE_GET_USER;
                else URL = _URL + "/" + MainData.WATCHER_SERVICE_GET_USER;

                OperatingSystem os = Environment.OSVersion;
                string data = string.Format("{{" +
                                "\"{0}\":\"{1}\", " +
                                "\"{2}\":\"{3}\", " +
                                "\"{4}\":\"{5}\", " +
                                "\"{6}\":\"{7}\", " +
                                "\"{8}\":\"{9}\", " +
                                "\"{10}\":\"{11}\"," +
                                "\"{12}\":\"{13}\"}}",
                                STR_IP, IP,
                                STR_HOSTNAME, HostName,
                                STR_PORT, PORT_NUMBER,
                                STR_MAC, MAC,
                                STR_OS, os.Platform.ToString().ToUpper().Substring(0, 3),
                                STR_GROUP, "0",
                                STR_PCNAME, PcName);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
                request.Method = MainData.CURL_STR_POST_METHOD;
                request.ContentType = MainData.CURL_STR_CONTENTTYPE;
                request.Timeout = MainData.CURL_TIMEOUT_DELAY;
                //Error evasion
                ServerCertificateValidationCallback();
                // POST할 데이타를 Request Stream에 쓴다
                byte[] bytes = Encoding.ASCII.GetBytes(data);
                request.ContentLength = bytes.Length; // 바이트수 지정

                using (Stream reqStream = request.GetRequestStream())
                {
                    reqStream.Write(bytes, 0, bytes.Length);
                }

                // Response 처리
                string responseText = string.Empty;
                using (WebResponse resp = request.GetResponse())
                {
                    Stream respStream = resp.GetResponseStream();
                    using (StreamReader sr = new StreamReader(respStream)) { responseText = sr.ReadToEnd(); };
                };

                JObject UserObj = JObject.Parse(responseText);


                if (!int.TryParse(UserObj[STR_RET].ToString(), out int ret))
                {
                    throw new Exception(STR_RET + " is Not int");
                }
                if (ret != 0)
                {
                    string ErrorMsg = UserObj[STR_ERR_MSG].ToString();
                    throw new Exception(ErrorMsg);
                }

                //Get User Data
                UserData User = new UserData()
                {
                    URL = UserObj[STR_S3PROXY].ToString(),
                    UserName = UserObj[STR_USERID].ToString(),
                    AccessKey = UserObj[STR_ACCESS_KEY].ToString(),
                    AccessSecret = UserObj[STR_ACCESS_SECRET].ToString(),
                    StorageName = UserObj[STR_TENANT_KEY].ToString(),
                    Debug = false
                };

                return User;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        /*****************************Get Icon***************************************/
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFileInfo psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        public const uint SHGFI_ICON = 0x000000100;
        public const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        public const uint SHGFI_OPENICON = 0x000000002;
        public const uint SHGFI_SMALLICON = 0x000000001;
        public const uint SHGFI_LARGEICON = 0x000000000;
        public const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        public const uint FILE_ATTRIBUTE_FILE = 0x00000100;

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        public static ImageSource IconToImageSource(this Icon icon)
        {
            ImageSource wpfBitmap = null;
            try
            {
                Bitmap bitmap = icon.ToBitmap();
                IntPtr hBitmap = bitmap.GetHbitmap();

                wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                if (!DeleteObject(hBitmap))
                {
                    throw new Win32Exception();
                }
            }
            catch { }

            return wpfBitmap;
        }

        public static ImageSource GetIconImageSource(string path)
        {
            Icon defaultIcon = Utility.GetIcon(path, IconSize.Small, ItemState.Close);
            return Utility.IconToImageSource(defaultIcon);
        }

        public static Icon GetIcon(string path, IconSize size, ItemState state)
        {
            var flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES;

            if (Equals(state, ItemState.Open))       { flags += SHGFI_OPENICON;  }
            if (object.Equals(size, IconSize.Small)) { flags += SHGFI_SMALLICON; }
            else                                     { flags += SHGFI_LARGEICON; }

            var shfi = new SHFileInfo();
            var res = SHGetFileInfo(path, FILE_ATTRIBUTE_DIRECTORY, out shfi, (uint)Marshal.SizeOf(shfi), flags);
            if (Equals(res, IntPtr.Zero)) throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
            try
            {
                Icon.FromHandle(shfi.hIcon);
                return (Icon)Icon.FromHandle(shfi.hIcon).Clone();
            }
            catch
            {
                throw;
            }
            finally
            {
                DestroyIcon(shfi.hIcon);
            }
        }
    }
}
