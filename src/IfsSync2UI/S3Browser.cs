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
using CefSharp;
using CefSharp.Wpf;
using IfsSync2Data;
using log4net;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace IfsSync2UI
{
    /// <summary>
    /// Browser.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class S3Browser : Window
    {
        private static readonly string CLASS_NAME = "S3Browser";

        private readonly ChromiumWebBrowser browser;

        private readonly string URL;
        private readonly string AccessKey;
        private readonly string SecretKey;
        private readonly string Source;

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public S3Browser(string _Source, string _URL, string _AccessKey, string _SecretKey)
        {
            URL = _URL;
            AccessKey = _AccessKey;
            SecretKey = _SecretKey;

            InitializeComponent();
            Source = string.Format("https://{0}", _Source);
            Console.WriteLine(Source);
            browser = new ChromiumWebBrowser(Source);

            browserContainer.Content = browser;
            browser.LoadingStateChanged += Browser_LoadingStateChanged;
            browser.KeyDown += Browser_KeyDown;
        }

        private void Browser_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.F5)
            {
                browser.Reload();
            }
        }


        private void Browser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            const string FUNCTION_NAME = "Browser_LoadingStateChanged";
            if (!e.IsLoading)
            {
                try
                {
                    browser.ExecuteScriptAsync(string.Format("document.getElementById('{0}').value = '{1}'", "s3AuthURL", URL));
                    browser.ExecuteScriptAsync(string.Format("document.getElementById('{0}').value = '{1}'", "s3AccessKey", AccessKey));
                    browser.ExecuteScriptAsync(string.Format("document.getElementById('{0}').value = '{1}'", "s3AccessSecret", SecretKey));
                    browser.ExecuteScriptAsync("submitFormS3();");
                }
                catch (Exception ex)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", ex.Message);
                    Dispatcher.Invoke(delegate { Utility.ErrorMessageBox("S3 File Manager URL정보가 올바르지 않습니다.", Title); Close(); });
                    
                }
            }
        }

    }
}
