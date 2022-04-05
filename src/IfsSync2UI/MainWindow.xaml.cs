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
using log4net;
using log4net.Config;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using IfsSync2Data;
using System.Collections.ObjectModel;
using System.Windows.Shapes;
using System.Windows.Media;
using Amazon.S3;
using System.Windows.Input;
using System.Timers;
using System.Windows.Controls.Primitives;
using System.Threading;
using CefSharp.Wpf;
using CefSharp;
using Amazon;

[assembly: XmlConfigurator(ConfigFile = "IfsSync2UILogConfig.xml", Watch = true)]

namespace IfsSync2UI
{
    public partial class MainWindow : Window
    {
        private static readonly string CLASS_NAME = "MainWindow";
        private const double MaxAngle = 360;
        private const string NULL_STORAGE_NAME = "N/A";

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        /**********************************************************************/
        private readonly ObservableCollection<JobDetailData> JobDetailList = new ObservableCollection<JobDetailData>();

        private StorageData MainStorage = null;
        private readonly StorageUI MainStorageUI = null;

        private readonly List<StorageData> StorageList = new List<StorageData>();
        private readonly List<StorageUI> StorageUIList = new List<StorageUI>();

        private readonly WatcherConfig WatcherConfigs;

        private S3Browser browser = null;
        /***************************** SQL ************************************/
        private readonly JobDataSqlManager JobSQL;
        private readonly UserDataSqlManager UserSQL;
        /**********************************************************************/
        private readonly System.Timers.Timer UpdateJobListTimer;
        private readonly System.Timers.Timer UpdateStorageListTimer;

        private const int JOB_TIMER_DELAY = 5000;
        private const int STORAGE_TIMER_DELAY = 60000;
        /**************************Toggle Button*******************************/
        ToggleButton SeletedBtn = null;
        Mutex mutex = null;
        private int GlobalCount = 0;

        public MainWindow()
        {
            DuplicateExecution(MainData.MUTEX_NAME_UI);

            MainUtility.DeleteOldLogs(MainData.GetLogFolder("IfsSync2UI"));
            log.Info("Main Start");
            WatcherConfigs = new WatcherConfig(true);
            JobSQL = new JobDataSqlManager(WatcherConfigs.RootPath);
            UserSQL = new UserDataSqlManager(WatcherConfigs.RootPath);

            InitializeComponent();
            MainStorageUI = new StorageUI(Storage_1, L_StorageName_1, L_URL_1, P_Graph_1, L_SizeRate_1, L_Total_1, L_Used_1, L_Free_1, T_StorageName, T_S3FileManagerURL);
            StorageUIList.Add(new StorageUI(Storage_2, L_StorageName_2, L_URL_2, P_Graph_2, L_SizeRate_2, L_Total_2, L_Used_2, L_Free_2, T_EditStorage_2, T_EditS3FileManagerURL_2, B_Edit_2, T_EditURL_2, T_EditAccessKey_2, T_EditAccessSecret_2, T_EditUserName_2, T_EmptyStorage_2, G_Quota_2));
            StorageUIList.Add(new StorageUI(Storage_3, L_StorageName_3, L_URL_3, P_Graph_3, L_SizeRate_3, L_Total_3, L_Used_3, L_Free_3, T_EditStorage_3, T_EditS3FileManagerURL_3, B_Edit_3, T_EditURL_3, T_EditAccessKey_3, T_EditAccessSecret_3, T_EditUserName_3, T_EmptyStorage_3, G_Quota_3));
            StorageUIList.Add(new StorageUI(Storage_4, L_StorageName_4, L_URL_4, P_Graph_4, L_SizeRate_4, L_Total_4, L_Used_4, L_Free_4, T_EditStorage_4, T_EditS3FileManagerURL_4, B_Edit_4, T_EditURL_4, T_EditAccessKey_4, T_EditAccessSecret_4, T_EditUserName_4, T_EmptyStorage_4, G_Quota_4));
            L_JobList.ItemsSource = JobDetailList;

            TabInit();
            StorageListUpdate();
            JobListUpdate();

            UpdateJobListTimer = new System.Timers.Timer { Interval = JOB_TIMER_DELAY };
            UpdateJobListTimer.Elapsed += UpdateJobList;
            UpdateJobListTimer.Start();

            UpdateStorageListTimer = new System.Timers.Timer { Interval = STORAGE_TIMER_DELAY };
            UpdateStorageListTimer.Elapsed += UpdateStorageList;
            UpdateStorageListTimer.Start();

            Title += " V" + MainData.GetVersion();
        }
        private void DuplicateExecution(string MutexName)
        {
            try
            {
                mutex = new Mutex(false, MutexName);
            }
            catch (Exception)
            {
                Application.Current.Shutdown();
            }
            if(!mutex.WaitOne(0, false)) Application.Current.Shutdown();

        }
        private void UpdateJobList(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(delegate {
                JobListUpdate();
            });
        }
        public void UpdateStorageList(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(delegate {
                StorageListUpdate();
            });
        }
        private void TabInit()
        {
            const string FUNCTION_NAME = "TabInit";
            //Set Job
            List<JobData> JobList;
            try
            {
                JobList = JobSQL.GetJobDatas(Environment.UserName);
            }
            catch{ return; }

            if (JobList.Count == 0)
            {
                //Instant Backup
                JobData Instant = new JobData()
                {
                    JobName = MainData.INSTANT_BACKUP_NAME,
                    HostName = Environment.UserName,
                    StrPolicy = JobData.PolicyNameList.Now.ToString()
                };
                JobSQL.PutJobData(Instant);
                Instant.ID = JobSQL.GetJobDataID(Instant.HostName, Instant.JobName);
                JobList.Add(Instant);
            }
            foreach (var Job in JobList)
            {
                try
                {
                    MainTab.Dispatcher.Invoke(delegate
                    {
                        TabItem item = new TabItem();

                        JobTab tabItemContent = new JobTab(item, Job, WatcherConfigs.RootPath);
                        item.Content = tabItemContent;

                        item.Header = Job.JobName;
                        MainTab.Items.Add(item);
                    });
                    log.InfoFormat("[{0}:{1}] Load JobData : {2}", CLASS_NAME, FUNCTION_NAME, Job.JobName);
                }
                catch (Exception ex)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", ex.Message);
                }
            }

            MainTab.SelectedIndex = 0;
        }
        private void ToggleButtonAllClose(ToggleButton toggle)
        {

            if (toggle != SeletedBtn) if (SeletedBtn != null) { if (SeletedBtn.IsChecked.Value) SeletedBtn.IsChecked = false; }

            if (toggle != B_RealTimeToggle) if (B_RealTimeToggle.IsChecked.Value) B_RealTimeToggle.IsChecked = false;
            if (toggle != B_ScheduleToggle) if (B_ScheduleToggle.IsChecked.Value) B_ScheduleToggle.IsChecked = false;
            if (toggle != B_StorageToggle)  if (B_StorageToggle.IsChecked.Value) B_StorageToggle.IsChecked = false;
            
            if (toggle != B_Edit1  ) if (B_Edit1  .IsChecked.Value) B_Edit1.IsChecked = false;
            if (toggle != B_Edit_2  ) if (B_Edit_2  .IsChecked.Value) B_Edit_2.IsChecked = false;
            if (toggle != B_Edit_3  ) if (B_Edit_3  .IsChecked.Value) B_Edit_3.IsChecked = false;
            if (toggle != B_Edit_4  ) if (B_Edit_4  .IsChecked.Value) B_Edit_4.IsChecked = false;
            if (toggle != B_Delete_2) if (B_Delete_2.IsChecked.Value) B_Delete_2.IsChecked = false;
            if (toggle != B_Delete_3) if (B_Delete_3.IsChecked.Value) B_Delete_3.IsChecked = false;
            if (toggle != B_Delete_4) if (B_Delete_4.IsChecked.Value) B_Delete_4.IsChecked = false;
        }
        private void ToggleButtonAllClose()
        {
            if (SeletedBtn != null) { if (SeletedBtn.IsChecked.Value) SeletedBtn.IsChecked = false; }

            if (B_RealTimeToggle.IsChecked.Value) B_RealTimeToggle.IsChecked = false;
            if (B_ScheduleToggle.IsChecked.Value) B_ScheduleToggle.IsChecked = false;

            if (B_Edit1.IsChecked.Value) B_Edit1.IsChecked = false;
            if (B_Edit_2.IsChecked.Value) B_Edit_2.IsChecked = false;
            if (B_Edit_3.IsChecked.Value) B_Edit_3.IsChecked = false;
            if (B_Edit_4.IsChecked.Value) B_Edit_4.IsChecked = false;
            if (B_Delete_2.IsChecked.Value) B_Delete_2.IsChecked = false;
            if (B_Delete_3.IsChecked.Value) B_Delete_3.IsChecked = false;
            if (B_Delete_4.IsChecked.Value) B_Delete_4.IsChecked = false;
        }
        /**********************************************************************/

        #region Job Manager
        private string GetJobType(JobData.PolicyNameList Policy)
        {
            switch (Policy)
            {
                case JobData.PolicyNameList.Now: return MainData.INSTANT_BACKUP_NAME;
                case JobData.PolicyNameList.RealTime: return "Real-Time";
                case JobData.PolicyNameList.Schedule: return "Schedule";
                default: return "None";
            }
        }
        private void JobListUpdate()
        {
            List<JobData> NormalJobs = JobSQL.GetJobDatas(Environment.UserName);
            List<JobData> GlobalJobs = JobSQL.GetJobDatas(true);

            List<JobData> JobList = new List<JobData>();
            JobList.AddRange(GlobalJobs);
            JobList.AddRange(NormalJobs);

            GlobalCount = GlobalJobs.Count;

            //JobDetailData Check
            foreach (JobDetailData DetailData in JobDetailList) DetailData.Delete = true;

            foreach (JobData Job in JobList)
            {
                bool IsNewJob = true;

                foreach (JobDetailData DetailData in JobDetailList)
                {
                    if (DetailData.JobName == Job.JobName)
                    {
                        IsNewJob = false;
                        DetailData.Delete = false;

                        DetailData.ExtensionList = Job.WhiteFileExt;
                        if (Job.UserID == -1) DetailData.StorageName = NULL_STORAGE_NAME;
                        else
                        {
                            if (MainStorage != null)
                            {
                                if (MainStorage.IsGlobalUser == Job.IsGlobalUser && MainStorage.ID == Job.UserID)
                                {
                                    DetailData.StorageName = MainStorage.StorageName;
                                    break;
                                }
                            }
                            foreach (var storage in StorageList)
                                if (storage.IsGlobalUser == Job.IsGlobalUser && storage.ID == Job.UserID)
                                    DetailData.StorageName = storage.StorageName;
                            
                        }
                        break;
                    }
                }

                if (IsNewJob)
                {
                    bool CreateSuccess = false;
                    foreach (var storage in StorageList)
                    {
                        if (storage.IsGlobalUser == Job.IsGlobalUser)
                        {
                            if (storage.ID == Job.UserID)
                            {
                                Visibility BtnVisibility = Visibility.Hidden;
                                if (!Job.Global && Job.JobName != MainData.INSTANT_BACKUP_NAME) BtnVisibility = Visibility.Visible;

                                JobDetailList.Add(new JobDetailData(Job.HostName, Job.JobName, Job.ID)
                                {
                                    JobType = GetJobType(Job.Policy),
                                    ExtensionList = Job.WhiteFileExt,
                                    StorageName = storage.StorageName,
                                    CircleBlue = Image_CircleBlue.Source,
                                    CircleGray = Image_CircleGray.Source,
                                    TriangleRed = Image_TriangleRed.Source,
                                    SquareGray = Image_SquareGray.Source,
                                    TriangleGreen = Image_TriangleGreen.Source,
                                    BtnVisibility = BtnVisibility
                                });
                                CreateSuccess = true;
                                break;
                            }
                        }
                    }
                    if (MainStorage != null)
                    {
                        if (MainStorage.IsGlobalUser == Job.IsGlobalUser)
                        {
                            if (MainStorage.ID == Job.UserID)
                            {
                                Visibility BtnVisibility = Visibility.Hidden;
                                if (!Job.Global && Job.JobName != MainData.INSTANT_BACKUP_NAME) BtnVisibility = Visibility.Visible;

                                JobDetailList.Add(new JobDetailData(Job.HostName, Job.JobName, Job.ID)
                                {
                                    JobType = GetJobType(Job.Policy),
                                    ExtensionList = Job.WhiteFileExt,
                                    StorageName = MainStorage.StorageName,
                                    CircleBlue = Image_CircleBlue.Source,
                                    CircleGray = Image_CircleGray.Source,
                                    TriangleRed = Image_TriangleRed.Source,
                                    SquareGray = Image_SquareGray.Source,
                                    TriangleGreen = Image_TriangleGreen.Source,
                                    BtnVisibility = BtnVisibility
                                });
                                CreateSuccess = true;
                                break;
                            }
                        }
                    }
                    if (!CreateSuccess)
                    {
                        if (Job.JobName == MainData.INSTANT_BACKUP_NAME)
                        {
                            JobDetailList.Add(new JobDetailData(Job.HostName, Job.JobName, Job.ID)
                            {
                                JobType = GetJobType(Job.Policy),
                                ExtensionList = Job.WhiteFileExt,
                                StorageName = NULL_STORAGE_NAME,
                                CircleBlue = Image_CircleBlue.Source,
                                CircleGray = Image_CircleGray.Source,
                                TriangleRed = Image_TriangleRed.Source,
                                SquareGray = Image_SquareGray.Source,
                                TriangleGreen = Image_TriangleGreen.Source,
                                BtnVisibility = Visibility.Hidden
                            });
                        }
                    }
                }
            }

            for (int index = JobDetailList.Count - 1; index >= 0; index--)
                if (JobDetailList[index].Delete) JobDetailList.RemoveAt(index);

            L_JobList.Items.Refresh();
        }
        private void Btn_JobListUpdate(object sender, RoutedEventArgs e)
        {
            JobListUpdate();
        }
        private bool DuplicateJobCheck(string JobName)
        {
            for (int index = 2; index < MainTab.Items.Count; index++)
            {
                TabItem Item = MainTab.Items[index] as TabItem;
                JobTab Tab = Item.Content as JobTab;

                if (Tab.Job.JobName == JobName) return true;
            }
            if (JobSQL.IsJobName(Environment.UserName, JobName)) return true;
            return false;   
        }
        private bool CreateJob(string _JobName, JobData.PolicyNameList _Policy)
        {
            const string FUNCTION_NAME = "CreateJob";

            if (Utility.FileNameSpecialCharactersErrorCheck(_JobName))
            {
                Utility.ErrorMessageBox("Job 이름에 다음문자를 사용할 수 없습니다.\n[\\, /, :, *, ?, \", <, >, |]", Title);
                return false;
            }
            if(DuplicateJobCheck(_JobName))
            {
                Utility.ErrorMessageBox("같은 이름의 Job이 이미 존재합니다.", Title);
                return false;
            }

            try
            { 
                JobData Data = new JobData()
                {
                    JobName = _JobName,
                    HostName = Environment.UserName,
                    Policy = _Policy
                };
                MainTab.Dispatcher.Invoke(delegate
                {
                    TabItem item = new TabItem { Header = Data.JobName };

                    JobTab tabItemContent = new JobTab(item, Data, WatcherConfigs.RootPath, true);

                    item.Content = tabItemContent;
                    MainTab.Items.Add(item);
                    MainTab.SelectedIndex = MainTab.Items.Count - 1;
                });

                //B_NewJob.IsEnabled = false;
                log.InfoFormat("Create New Job : {0}", Data.JobName);
                return true;
            }
            catch (Exception ex)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", ex.Message);
                return false;
            }
        }

        private void Btn_CreateRealTimeJob(object sender, RoutedEventArgs e)
        {
            if(!string.IsNullOrWhiteSpace(T_RealTimeName.Text))
            {
                string JobName = T_RealTimeName.Text;
                if (CreateJob(JobName, JobData.PolicyNameList.RealTime))
                {
                    T_RealTimeName.Text = string.Empty;
                    B_RealTimeToggle.IsChecked = false;
                }
            }
        }
        private void Btn_RealTimeToggleClose(object sender, RoutedEventArgs e)
        {
            B_RealTimeToggle.IsChecked = false;
            T_RealTimeName.Text = string.Empty;
        }
        private void Txb_RealTimeTextEnterEvent(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (!string.IsNullOrWhiteSpace(T_RealTimeName.Text))
                {
                    string JobName = T_RealTimeName.Text;
                    if (CreateJob(JobName, JobData.PolicyNameList.RealTime))
                    {
                        T_RealTimeName.Text = string.Empty;
                        B_RealTimeToggle.IsChecked = false;
                    }
                }
            }
        }

        private void Btn_CreateScheduleJob(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(T_ScheduleName.Text))
            {
                string JobName = T_ScheduleName.Text;
                if(CreateJob(JobName, JobData.PolicyNameList.Schedule))
                {
                    T_ScheduleName.Text = string.Empty;
                    B_ScheduleToggle.IsChecked = false;
                }
            }
        }
        private void Btn_ScheduleToggleClose(object sender, RoutedEventArgs e)
        {
            B_ScheduleToggle.IsChecked = false;
            T_ScheduleName.Text = string.Empty;
        }

        private void Txb_ScheduleTextEnterEvent(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Return)
            {
                if (!string.IsNullOrWhiteSpace(T_ScheduleName.Text))
                {
                    string JobName = T_ScheduleName.Text;
                    B_ScheduleToggle.IsChecked = false;
                    if (CreateJob(JobName, JobData.PolicyNameList.Schedule))
                    {
                        T_ScheduleName.Text = string.Empty;
                        B_ScheduleToggle.IsChecked = false;
                    }
                }
            }
        }

        private void Btn_RealTimeToggleClick(object sender, RoutedEventArgs e) { ToggleButtonAllClose((ToggleButton)sender); T_RealTimeName.Focus(); }
        private void Btn_ScheduleToggleClick(object sender, RoutedEventArgs e) { ToggleButtonAllClose((ToggleButton)sender); T_ScheduleName.Focus(); }

        private void Btn_DeleteJobList(object sender, RoutedEventArgs e)
        {
            const string FUNCTION_NAME = "Btn_DeleteJobList";
            Button button = (Button)sender;

            string JobName = button.Tag.ToString();
            bool DeleteFlag = false;

            try
            {

                for (int index = 2; index < MainTab.Items.Count; index++)
                {
                    TabItem Item = MainTab.Items[index] as TabItem;
                    JobTab Tab = Item.Content as JobTab;
                    if (Tab.Job.JobName == JobName)
                    {
                        Tab.Delete();
                        MainTab.Items.RemoveAt(index);
                        DeleteFlag = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", ex.Message);
            }
            if (DeleteFlag) {
                SeletedBtn = null;
                JobListUpdate();
            }
            else Utility.ErrorMessageBox("Job 삭제 실패!", Title);
        }
        private void Btn_DeleteJobListClose(object sender, RoutedEventArgs e)
        {
            if (SeletedBtn != null)
            {
                SeletedBtn.IsChecked = false;
            }

        }
        private void ToggleBtn_DeleteJob(object sender, RoutedEventArgs e)
        {
            SeletedBtn = (ToggleButton)sender;
            ToggleButtonAllClose((ToggleButton)sender);
        }

        #endregion Job Manager

        #region Credential Manager

        private void MainstorageUpdate()
        {
            List<UserData> GlobalUsers = UserSQL.GetUsers(true);

            if (GlobalUsers.Count == 0)
            {
                T_EmptyStorage.Visibility = Visibility.Visible;
                DefaultGraph.Visibility = Visibility.Hidden;
            }
            else
            {
                UserData GlobalUser = GlobalUsers[0];
                
                Utility.GetVolumeSize(GlobalUser.URL, out long Total, out long Used);

                if(MainStorage == null)
                {
                    MainStorage = new StorageData(GlobalUser.ID, GlobalUser.HostName, GlobalUser.UserName)
                    {
                        StorageName = GlobalUser.StorageName,
                        URL = GlobalUser.URL,
                        AccessKey = GlobalUser.AccessKey,
                        AccessSecret = GlobalUser.AccessSecret,
                        S3FileManagerURL = GlobalUser.S3FileManagerURL,
                        TotalSize = Total,
                        UsedSize = Used,
                        Delete = false
                    };
                }
                else
                {
                    MainStorage.StorageName = GlobalUser.StorageName;
                    MainStorage.S3FileManagerURL = GlobalUser.S3FileManagerURL;
                    MainStorage.Delete = false;
                    MainStorage.TotalSize = Total;
                    MainStorage.UsedSize = Used;
                }
                MainStorageUIUpdate();
            }
        }
        private void MainStorageUIUpdate()
        {
            if(MainStorage != null)
            {
                MainStorageUI.Main.Dispatcher.Invoke(delegate
                {
                    MainStorageUI.Main.Visibility = Visibility.Visible;
                    MainStorageUI.StorageName.Text = MainStorage.StorageName;
                    MainStorageUI.URL.Text = MainStorage.URL;

                    MainStorageUI.SizeRate.Content = MainStorage.StrRate;
                    MainStorageUI.Total.Content = MainStorage.StrTotalSize;
                    MainStorageUI.Used.Content = MainStorage.StrUsedSize;
                    MainStorageUI.Free.Content = MainStorage.StrFreeSize;
                    GraphDrawing(MainStorage.Rate, MainStorageUI.Graph);
                });


                T_EmptyStorage.Visibility = Visibility.Hidden;
                DefaultGraph.Visibility = Visibility.Visible;
            }
        }
        private void StorageListUpdate()
        {
            MainstorageUpdate();

            List<UserData> NormalUsers = UserSQL.GetUsers(Environment.UserName);

            foreach (var Storage in StorageList) Storage.Delete = true;

            //New Storage Check
            foreach (var User in NormalUsers)
            {
                bool IsNewUser = true;
                Utility.GetVolumeSize(User.URL, out long Total, out long Used);

                foreach (var Storage in StorageList)
                {
                    if (Storage.UserName == User.UserName)
                    {
                        IsNewUser = false;
                        Storage.StorageName = User.StorageName;
                        Storage.S3FileManagerURL = User.S3FileManagerURL;
                        Storage.Delete = false;
                        Storage.TotalSize = Total;
                        Storage.UsedSize = Used;
                        break;
                    }
                }

                if (IsNewUser)
                {
                    StorageList.Add(new StorageData(User.ID, User.HostName, User.UserName)
                    {
                        StorageName = User.StorageName,
                        URL = User.URL,
                        AccessKey = User.AccessKey,
                        AccessSecret = User.AccessSecret,
                        S3FileManagerURL = User.S3FileManagerURL,
                        TotalSize = Total,
                        UsedSize = Used,
                        Delete = false
                    });
                }

            }

            //Storage Delete Check
            for (int i = StorageList.Count - 1; i >= 0; i--)
            {
                if (StorageList[i].Delete) { try { StorageList.RemoveAt(i); } catch { } }
            }

            StoratgeUIUpdate();

            if (StorageList.Count >= StorageUIList.Count)
            {
                B_StorageToggle.IsChecked = false;
                B_StorageToggle.IsEnabled = false;
            }
            else
            {
                B_StorageToggle.IsEnabled = true;
            }
        }
        private void StoratgeUIUpdate()
        {
            for (int i = 0; i < StorageUIList.Count; i++)
            {
                if (i < StorageList.Count)
                {
                    StorageUIList[i].Main.Dispatcher.Invoke(delegate {
                        StorageUIList[i].Main.Visibility = Visibility.Visible;
                        StorageUIList[i].StorageName.Text = StorageList[i].StorageName;
                        StorageUIList[i].URL.Text = StorageList[i].URL;
                        if (StorageList[i].IsAWS)
                        {
                            StorageUIList[i].Black_AWSMessage.Visibility = Visibility.Visible;
                            StorageUIList[i].Grid_Quota.Visibility = Visibility.Hidden;
                        }
                        else
                        {
                            StorageUIList[i].Black_AWSMessage.Visibility = Visibility.Hidden;
                            StorageUIList[i].Grid_Quota.Visibility = Visibility.Visible;

                            StorageUIList[i].SizeRate.Content = StorageList[i].StrRate;
                            StorageUIList[i].Total.Content    = StorageList[i].StrTotalSize;
                            StorageUIList[i].Used.Content     = StorageList[i].StrUsedSize;
                            StorageUIList[i].Free.Content     = StorageList[i].StrFreeSize;
                        }
                        GraphDrawing(StorageList[i].Rate, StorageUIList[i].Graph);
                    });
                }
                else StorageUIList[i].Main.Visibility = Visibility.Hidden;
            }
        }
        private void Btn_StorageUpdate(object sender, RoutedEventArgs e)
        {
            StoratgeUIUpdate();
            MainStorageUIUpdate();
        }

        private void Btn_StorageToggleClick(object sender, RoutedEventArgs e) { ToggleButtonAllClose((ToggleButton)sender); T_AddStorageName.Focus(); }
        
        private static bool CheckConnect(AmazonS3Client Client)
        {
            const string FUNCTION_NAME = "CheckConnect";
            try
            {
                var response = Client.ListBuckets();
                return true;
            }
            catch (AmazonS3Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}]{3}", CLASS_NAME, FUNCTION_NAME, "AmazonS3Exception", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}]{3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
        }
        private bool RegionEndpointCheck(string SystemName)
        {
            RegionEndpoint Endpoint = RegionEndpoint.GetBySystemName(SystemName);
            if (Endpoint.DisplayName.Equals(MainData.UNKNOWN)) return false;
            return true;
        }

        private bool LoginTest(UserData User)
        {
            const string FUNCTION_NAME = "LoginTest";

            try
            {
                AmazonS3Config config;

                if (User.URL.StartsWith(MainData.HTTP, StringComparison.OrdinalIgnoreCase))
                {
                    config = new AmazonS3Config { ServiceURL = User.URL, SignatureVersion = "2", ForcePathStyle = true };
                }
                else
                {
                    config = new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(User.URL), SignatureVersion = "2", ForcePathStyle = true };
                }
                AmazonS3Client Client = new AmazonS3Client(User.AccessKey, User.AccessSecret, config);
                if (CheckConnect(Client))
                {
                    log.InfoFormat("[{0}:{1}] Login Success : {2}", CLASS_NAME, FUNCTION_NAME, User.UserName);
                    return true;
                }
                else
                {
                    log.InfoFormat("[{0}:{1}] Login Fail : {2}", CLASS_NAME, FUNCTION_NAME, User.UserName);
                    return false;
                }
            }
            catch (AmazonS3Exception e)
            {
                log.FatalFormat("[{0}:{1}:{2}] Login fail : {3}", CLASS_NAME, FUNCTION_NAME, e.Message);
                return false;
            }
        }

        private bool BucketNameCheck(string BucketName)
        {
            foreach(char c in BucketName)
            {
                if (c >= 'a' && c <= 'z') { }
                else if (c >= '0' && c <= '9') { }
                else if (c == '.') { }
                else if (c == '-') { }
                else return true;
            }
            return false;
        }
        private void CreateCredential()
        {

            if (string.IsNullOrWhiteSpace(T_AddStorageName     .Text)) { Utility.ErrorMessageBox("Storage Credential Name is Empty!", Title); T_AddStorageName     .Focus(); return; }
            if (string.IsNullOrWhiteSpace(T_AddStorageURL      .Text)) { Utility.ErrorMessageBox("URL is Empty!", Title);                     T_AddStorageURL      .Focus(); return; }
            if (string.IsNullOrWhiteSpace(T_AddStorageAccessKey.Text)) { Utility.ErrorMessageBox("Access Key is Empty!", Title);              T_AddStorageAccessKey.Focus(); return; }
            if (string.IsNullOrWhiteSpace(T_AddStorageSecretKey.Text)) { Utility.ErrorMessageBox("Secret Key is Empty!", Title);              T_AddStorageSecretKey.Focus(); return; }
            if (string.IsNullOrWhiteSpace(T_AddStorageUserName .Text)) { Utility.ErrorMessageBox("User Name (Bucket) is Empty!", Title);      T_AddStorageUserName .Focus(); return; }

            string UserName = T_AddStorageUserName.Text;
            if (BucketNameCheck(UserName))
            {
                Utility.ErrorMessageBox("User Name (Bucket) is not correct!\nBucket name should contain only \nlowercase letters, numbers, periods(.) and dashes(-)", Title);
                return;
            }

            if (UserSQL.IsUserName(UserName, false))
            {
                Utility.ErrorMessageBox("User name (Bucket) already exists.", Title);
                return;
            }
            string URL = T_AddStorageURL.Text.Trim();
            if (!URL.StartsWith(MainData.HTTP, StringComparison.OrdinalIgnoreCase))
            {
                if (!RegionEndpointCheck(URL))
                {
                    Utility.ErrorMessageBox("Region Endpoint Check fail!", Title); T_AddStorageUserName.Focus(); return;
                }
            }
            string S3FileManagerURL = "";
            if (MainStorage != null) S3FileManagerURL = MainStorage.S3FileManagerURL;

            UserData User = new UserData
            {
                HostName = Environment.UserName,
                URL = T_AddStorageURL.Text.Trim(),
                AccessKey = T_AddStorageAccessKey.Text.Trim(),
                AccessSecret = T_AddStorageSecretKey.Text.Trim(),
                StorageName = T_AddStorageName.Text.Trim(),
                UserName = T_AddStorageUserName.Text.Trim(),
                S3FileManagerURL = S3FileManagerURL
            };


            if (!LoginTest(User)) Utility.ErrorMessageBox("Login Test Failed!", Title);
            else
            {
                if (!UserSQL.InsertUser(User, false)) Utility.ErrorMessageBox("User Data is not Save!", Title);
                else
                {
                    StorageListUpdate();
                    //B_StorageToggle.IsChecked = false;
                    T_AddStorageName.Text = string.Empty;
                    T_AddStorageURL.Text = string.Empty;
                    T_AddStorageAccessKey.Text = string.Empty;
                    T_AddStorageSecretKey.Text = string.Empty;
                    T_AddStorageUserName.Text = string.Empty;
                    B_StorageToggle.IsChecked = false;
                }
            }
        }
        private void CreateCredentialEvent(object sender, KeyEventArgs e) { if(e.Key == Key.Enter) CreateCredential(); }
        private void Btn_CreateCredential(object sender, RoutedEventArgs e) { CreateCredential(); }
        private void Btn_CredentialToggleClose(object sender, RoutedEventArgs e)
        {
            B_StorageToggle.IsChecked  = false;
            T_AddStorageName     .Text = string.Empty;
            T_AddStorageURL      .Text = string.Empty;
            T_AddStorageAccessKey.Text = string.Empty;
            T_AddStorageSecretKey.Text = string.Empty;
            T_AddStorageUserName .Text = string.Empty;
        }

        private void DefaultStorageSave()
        {
            if (string.IsNullOrWhiteSpace(T_StorageName.Text)) { Utility.ErrorMessageBox("Storage Name is Empty", Title); return; }
            if (string.IsNullOrWhiteSpace(T_EditMainIP .Text)) { Utility.ErrorMessageBox("IP Address is Empty", Title); return; }
            if (string.IsNullOrWhiteSpace(T_PortNumber .Text)) { Utility.ErrorMessageBox("Port Number is Empty", Title); return; }
            if (string.IsNullOrWhiteSpace(T_AditPCName .Text)) { Utility.ErrorMessageBox("PCName is Empty", Title); return; }
            const string FUNCTION_NAME = "Btn_Edit1Save";

            string StorageName = T_StorageName.Text;
            string S3FileManagerURL = T_S3FileManagerURL.Text;

            if (string.IsNullOrWhiteSpace(WatcherConfigs.IP))
            {
                string Address = T_EditMainIP.Text;
                string Port = T_PortNumber.Text;

                string URL = MainData.CreateAddress(Address, Port);

                try
                {
                    UserData GlobalUser = Utility.GetGlobalUser(URL, WatcherConfigs.PcName);
                    GlobalUser.StorageName = StorageName;

                    if (string.IsNullOrWhiteSpace(S3FileManagerURL)) GlobalUser.S3FileManagerURL = MainData.CreateS3FileManagerURL(Address);
                    else GlobalUser.S3FileManagerURL = S3FileManagerURL;

                    if (!UserSQL.InsertUser(GlobalUser, true)) return;
                }
                catch (Exception ex)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] Global User Get Error : {3}", CLASS_NAME, FUNCTION_NAME, "Exception", ex.Message);
                    Utility.ErrorMessageBox(string.Format("Global User Get Error : {0}", ex.Message), Title);
                    return;
                }
                WatcherConfigs.IP = Address;
                WatcherConfigs.Port = Port;
            }
            else
            {
                if(!MainStorage.StorageName.Equals(StorageName))
                {
                    if (!UserSQL.UpdateUserStorageName(MainStorage.ID, StorageName, true))
                    {
                        Utility.ErrorMessageBox("StorageName save failed!", Title);
                        return;
                    }
                }
                if(!MainStorage.S3FileManagerURL.Equals(S3FileManagerURL))
                {
                    if (!UserSQL.UpdateUserS3FileManagerURL(MainStorage.ID, S3FileManagerURL, true))
                    {
                        Utility.ErrorMessageBox("S3 File Manager URL save failed!", Title);
                        return;
                    }
                }
            }

            WatcherConfigs.PcName = T_AditPCName.Text;
            if (!string.IsNullOrWhiteSpace(T_AditEmail.Text)) WatcherConfigs.Email = T_AditEmail.Text;
            B_Edit1.IsChecked = false;
            StorageListUpdate();
        }

        private void Btn_Edit1Save(object sender, RoutedEventArgs e)
        {
            DefaultStorageSave();
        }
        private void Btn_Edit1Close(object sender, RoutedEventArgs e)
        {
            B_Edit1.IsChecked = false;
        }
        private void MainStorageSaveEvent(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter) DefaultStorageSave();
        }

        private void Btn_PopupAllClose(object sender, RoutedEventArgs e) { ToggleButtonAllClose(); }

        private void Btn_Edit1(object sender, RoutedEventArgs e)
        {
            ToggleButtonAllClose((ToggleButton)sender); 
            const string FUNCTION_NAME = "Btn_Edit1";
            try
            {
                T_StorageName.Focus();
                string IP = WatcherConfigs.IP;
                string Port = WatcherConfigs.Port;
                T_StorageName.Text = L_StorageName_1.Text.ToString();
                if (!string.IsNullOrWhiteSpace(IP))   { T_EditMainIP.Text = IP; T_EditMainIP.IsEnabled = false; }
                if (!string.IsNullOrWhiteSpace(Port)) { T_PortNumber.Text = Port; T_PortNumber.IsEnabled = false; }
                
                if (string.IsNullOrWhiteSpace(WatcherConfigs.PcName)) WatcherConfigs.PcName = Environment.MachineName;
                T_AditPCName.Text = WatcherConfigs.PcName;
                T_AditEmail.Text = WatcherConfigs.Email;
                T_S3FileManagerURL.Text = MainStorage.S3FileManagerURL;
                T_StorageName.Focus();
            }
            catch(Exception ex)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", ex.Message);
            }
        }

        private void SetStorageEditText(int index)
        {
            if (index >= StorageUIList.Count) return;

            StorageUIList[index].Box_StorageName     .Text = StorageList[index].StorageName;
            StorageUIList[index].Box_URL             .Text = StorageList[index].URL;
            StorageUIList[index].Box_AccessKey       .Text = StorageList[index].AccessKey;
            StorageUIList[index].Box_AccessSecret    .Text = StorageList[index].AccessSecret;
            StorageUIList[index].Box_UserName        .Text = StorageList[index].UserName;
            StorageUIList[index].Box_S3FileManagerURL.Text = StorageList[index].S3FileManagerURL;

            StorageUIList[index].Box_URL         .IsEnabled = false;
            StorageUIList[index].Box_AccessKey   .IsEnabled = false;
            StorageUIList[index].Box_AccessSecret.IsEnabled = false;
            StorageUIList[index].Box_UserName    .IsEnabled = false;

            StorageUIList[index].Box_StorageName.Focus();
        }
        private void Btn_Edit(object sender, RoutedEventArgs e)
        {
            ToggleButtonAllClose((ToggleButton)sender);

            if (int.TryParse(((ToggleButton)sender).Tag.ToString(), out int Index))
            {
                SetStorageEditText(Index);
            }
        }
        private void Btn_Delete(object sender, RoutedEventArgs e) { ToggleButtonAllClose((ToggleButton)sender); }

        private void SaveStorage(int index)
        {
            if (index >= StorageUIList.Count) return;

            //저장로직
            string StorageName = StorageUIList[index].Box_StorageName.Text;
            string S3FileManagerURL = StorageUIList[index].Box_S3FileManagerURL.Text;

            if (!StorageList[index].StorageName.Equals(StorageName))
            {
                if (!UserSQL.UpdateUserStorageName(StorageList[index].ID, StorageName, false))
                {
                    Utility.ErrorMessageBox("StorageName save failed!", Title);
                    return;
                }
            }
            if (!StorageList[index].S3FileManagerURL.Equals(S3FileManagerURL))
            {
                if (!UserSQL.UpdateUserS3FileManagerURL(StorageList[index].ID, S3FileManagerURL, false))
                {
                    Utility.ErrorMessageBox("S3 File Manager URL save failed!", Title);
                    return;
                }
            }
            StorageUIList[index].PopupButton.IsChecked = false;
            StorageListUpdate();
        }

        private void StorageSaveEvent(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox Box = (TextBox)sender;

                if (int.TryParse(Box.Tag.ToString(), out int Index))
                {
                    SaveStorage(Index);
                }
            }
        }

        private void BtnEditSave(object sender, RoutedEventArgs e)
        {
            Button Box = (Button)sender;

            if (int.TryParse(Box.Tag.ToString(), out int Index))
            {
                SaveStorage(Index);
            }
        }
        private void DeleteStorage(int index)
        {
            if (index >= StorageUIList.Count) return;

            const String FUNCTION_NAME = "DeleteStorage";

            int UserID = StorageList[index].ID;
            string HostName = StorageList[index].HostName;

            //Check Instant
            {
                TabItem Item = MainTab.Items[1] as TabItem;
                JobTab Tab = Item.Content as JobTab;

                if(Tab.BackupStart)
                {
                    if(!Tab.Job.IsGlobalUser && Tab.Job.UserID == UserID)
                    {
                        Utility.ErrorMessageBox("Instant가 백업을 진행중입니다. \n스토리지를 삭제하고 싶으시면 Instant 백업을 중단하십시오.", Title);
                        return;
                    }
                }
            }

            //관련 job 삭제
            for (int i = MainTab.Items.Count; i > 1; i--)
            {
                try
                {
                    TabItem Item = MainTab.Items[i] as TabItem;
                    JobTab Jobtab = Item.Content as JobTab;
                    if (Jobtab.Job.UserID == UserID && Jobtab.Job.HostName == HostName)
                    {
                        Jobtab.Delete();
                        MainTab.Items.RemoveAt(i);
                    }
                }catch(Exception e)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                }
            }
            //User 삭제
            UserSQL.DeleteUserToID(UserID, false);

            StorageListUpdate();
            JobListUpdate();
        }
        private void Btn_EditDelete(object sender, RoutedEventArgs e)
        {
            Button Box = (Button)sender;

            if (int.TryParse(Box.Tag.ToString(), out int Index))
            {
                DeleteStorage(Index);
            }
            ToggleButtonAllClose();
        }

        #region Graph
        private void GraphDrawing(double Rate, StackPanel Graph)
        {
            Graph.Children.Clear();
            double Angle = MaxAngle * Rate / 100;

            if (Angle >= MaxAngle - 1) Angle = MaxAngle - 1;
            if (Angle < 1) Angle = 1;

            Point Center = new Point(Graph.ActualWidth / 2, Graph.ActualHeight / 2);
            double RadiusX = Center.X;
            //double RadiusY = Center.Y;
            double angle = Angle;

            bool fullEllipse = angle >= MaxAngle;
            if (fullEllipse) angle = MaxAngle / 2;


            Point first = new Point(Center.X + RadiusX, Center.Y);
            Point second = new Point(Center.X + RadiusX * MyCos(angle), Center.Y - RadiusX * MySin(angle));


            PathFigure pthFigure = new PathFigure { StartPoint = Center };

            LineSegment line = new LineSegment() { Point = first, IsStroked = false };
            ArcSegment arcSeg = new ArcSegment
            {
                Point = second,
                Size = new Size(Center.X, Center.Y),
                IsLargeArc = angle > MaxAngle / 2,
                SweepDirection = SweepDirection.Counterclockwise
            };
            PathSegmentCollection myPathSegmentCollection = new PathSegmentCollection
            {
                line,
                arcSeg
            };

            pthFigure.Segments = myPathSegmentCollection;

            PathFigureCollection pthFigureCollection = new PathFigureCollection
            {
                pthFigure
            };

            PathGeometry pthGeometry = new PathGeometry
            {
                Figures = pthFigureCollection
            };

            Path arcPath = new Path
            {
                StrokeThickness = 0,
                Data = pthGeometry,
                Fill = new SolidColorBrush(Colors.CornflowerBlue)
            };

            Graph.Children.Add(arcPath);


        }
        static double MySin(double degrees)
        {
            return Math.Sin(degrees * 2d * Math.PI / MaxAngle);
        } //sin
        static double MyCos(double degrees)
        {
            return Math.Cos(degrees * 2d * Math.PI / MaxAngle);
        } //cos
        #endregion Graph

        #endregion Credential Manager
        /***************************** Event ***********************************/
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            bool first = true;
            foreach (TabItem item in MainTab.Items)
            {
                if (first) { first = false; continue; }
                JobTab temp = item.Content as JobTab;
                temp.Close();
            }
            if (browser != null) browser.Close();
        }
        private void MainTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainTab.SelectedIndex == 0)
            {
                L_MainTitle.Text = "IfsSync2 Service Summary";
                //B_JobType.Visibility = Visibility.Hidden;
            }
            else if (MainTab.SelectedIndex == 1)
            {
                L_MainTitle.Text = "Instant Backup";
                //B_JobType.Visibility = Visibility.Hidden;
            }
            else
            {
                TabItem Tab = MainTab.Items[MainTab.SelectedIndex] as TabItem;
                JobTab JobTabItem = Tab.Content as JobTab;


                string Name = JobTabItem.Job.JobName;
                L_MainTitle.Text = JobTabItem.Job.StrPolicy + " Job : " + Name;
                //L_MainTitle.Content =  Name;
                //B_JobType.Visibility = Visibility.Visible;
                //B_JobType.Content = JobTabItem.Job.StrPolicy + " Job";
                //double Width = FontLength * Name.Length; //L_MainTitle.ActualWidth;
                //B_JobType.Margin = new Thickness(Width + MarginLength, MarginLength, MarginLength, MarginLength);
                //Grid_Delete.Margin = new Thickness(Width + B_JobType.Width + MarginLength*2, MarginLength, MarginLength, MarginLength);
            }

        }
        private void PopupTextBoxMouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Activate();
                TextBox Text = (TextBox)sender;
                Keyboard.Focus(Text);

            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        private void JobListDoubleClickEvent(object sender, MouseButtonEventArgs e)
        {
            // MessageBox.Show("이 기능은 현재 점검중입니다");
            // return;
            
            if (L_JobList.SelectedIndex < 0) return;
            int index = L_JobList.SelectedIndex + 1 - GlobalCount;
            MainTab.SelectedIndex = index;

            //Console.WriteLine(index);

            //string StorageName = JobDetailList[index].StorageName;
            //Console.WriteLine(StorageName);
            //int count = 0;

            //if(MainStorage.StorageName == StorageName)
            //{
            //    if (browser == null)
            //    {
            //        Console.WriteLine("Create browser");
            //        browser = new S3Browser(MainStorage.S3FileManagerURL, MainStorage.URL, MainStorage.AccessKey, MainStorage.AccessSecret);
            //        browser.Show();
            //        return;
            //    }
            //    else
            //    {
            //        browser.Close();
            //        browser = new S3Browser(MainStorage.S3FileManagerURL, MainStorage.URL, MainStorage.AccessKey, MainStorage.AccessSecret);
            //        browser.Show();
            //        return;
            //    }
            //}

            //foreach (StorageData Storage in StorageList)
            //{
            //    Console.WriteLine("Storage : {0}", count++);
            //    if (Storage.StorageName == StorageName)
            //    {
            //        if(browser == null)
            //        {
            //            Console.WriteLine("Create browser");
            //            browser = new S3Browser(Storage.S3FileManagerURL, Storage.URL, Storage.AccessKey, Storage.AccessSecret);
            //            browser.Show();
            //            break;
            //        }
            //        else
            //        {
            //            browser.Close();
            //            browser = new S3Browser(Storage.S3FileManagerURL, Storage.URL, Storage.AccessKey, Storage.AccessSecret);
            //            browser.Show();
            //            break;
            //        }
            //    }
            //}
        }
    }
}
