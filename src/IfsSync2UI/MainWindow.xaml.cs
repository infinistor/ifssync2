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
using System.Windows.Controls.Primitives;
using System.Threading;
using Amazon;

[assembly: XmlConfigurator(ConfigFile = "IfsSync2UILogConfig.xml", Watch = true)]

namespace IfsSync2UI
{
	public partial class MainWindow : Window
	{
		const double MaxAngle = 360;
		const string NULL_STORAGE_NAME = "N/A";

		static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		readonly ObservableCollection<JobDetailData> JobDetailList = [];

		StorageData MainStorage = null;
		readonly StorageUI MainStorageUI = null;

		readonly List<StorageData> StorageList = [];
		readonly List<StorageUI> StorageUIList = [];

		readonly WatcherConfig WatcherConfigs;

		/***************************** SQL ************************************/
		readonly JobDbManager JobSQL;
		readonly UserDbManager UserSQL;
		/***************************** Timer ***********************************/
		readonly System.Timers.Timer UpdateJobListTimer;
		readonly System.Timers.Timer UpdateStorageListTimer;

		const int JOB_TIMER_DELAY = 5000;
		const int STORAGE_TIMER_DELAY = 60000;
		/**************************Toggle Button*******************************/
		ToggleButton SelectedBtn = null;
		Mutex mutex = null;
		int GlobalCount = 0;

		public MainWindow()
		{
			DuplicateExecution(MainData.MUTEX_NAME_UI);

			MainUtility.DeleteOldLogs(MainData.GetLogFolder("IfsSync2UI"));
			log.Info("Main Start");
			WatcherConfigs = new WatcherConfig(true);
			JobSQL = new JobDbManager();
			UserSQL = new UserDbManager();

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
		void DuplicateExecution(string MutexName)
		{
			try
			{
				mutex = new Mutex(false, MutexName);
			}
			catch (Exception)
			{
				Application.Current.Shutdown();
			}
			if (!mutex.WaitOne(0, false)) Application.Current.Shutdown();

		}
		void UpdateJobList(object sender, EventArgs e)
		{
			Dispatcher.Invoke(delegate
			{
				JobListUpdate();
			});
		}
		public void UpdateStorageList(object sender, EventArgs e)
		{
			Dispatcher.Invoke(delegate
			{
				StorageListUpdate();
			});
		}
		void TabInit()
		{
			//Set Job
			List<JobData> JobList;
			try
			{
				JobList = JobSQL.GetJobs(Environment.UserName);
			}
			catch { return; }

			if (JobList.Count == 0)
			{
				//Instant Backup
				var Instant = new JobData()
				{
					JobName = MainData.INSTANT_BACKUP_NAME,
					HostName = Environment.UserName,
					StrPolicy = JobData.PolicyName.Now.ToString()
				};
				JobSQL.PutJobData(Instant);
				Instant.Id = JobSQL.GetJobDataId(Instant.HostName, Instant.JobName);
				JobList.Add(Instant);
			}
			foreach (var Job in JobList)
			{
				try
				{
					MainTab.Dispatcher.Invoke(delegate
					{
						var item = new TabItem();

						var tabItemContent = new JobTab(item, Job, WatcherConfigs.RootPath);
						item.Content = tabItemContent;

						item.Header = Job.JobName;
						MainTab.Items.Add(item);
					});
					log.Info($"Load JobData : {Job.JobName}");
				}
				catch (Exception ex)
				{
					log.Error(ex);
				}
			}

			MainTab.SelectedIndex = 0;
		}
		void ToggleButtonAllClose(ToggleButton toggle)
		{

			if (toggle != SelectedBtn && SelectedBtn != null && SelectedBtn.IsChecked.Value) SelectedBtn.IsChecked = false;

			if (toggle != B_RealTimeToggle && B_RealTimeToggle.IsChecked.Value) B_RealTimeToggle.IsChecked = false;
			if (toggle != B_ScheduleToggle && B_ScheduleToggle.IsChecked.Value) B_ScheduleToggle.IsChecked = false;
			if (toggle != B_StorageToggle && B_StorageToggle.IsChecked.Value) B_StorageToggle.IsChecked = false;

			if (toggle != B_Edit1 && B_Edit1.IsChecked.Value) B_Edit1.IsChecked = false;
			if (toggle != B_Edit_2 && B_Edit_2.IsChecked.Value) B_Edit_2.IsChecked = false;
			if (toggle != B_Edit_3 && B_Edit_3.IsChecked.Value) B_Edit_3.IsChecked = false;
			if (toggle != B_Edit_4 && B_Edit_4.IsChecked.Value) B_Edit_4.IsChecked = false;
			if (toggle != B_Delete_2 && B_Delete_2.IsChecked.Value) B_Delete_2.IsChecked = false;
			if (toggle != B_Delete_3 && B_Delete_3.IsChecked.Value) B_Delete_3.IsChecked = false;
			if (toggle != B_Delete_4 && B_Delete_4.IsChecked.Value) B_Delete_4.IsChecked = false;
		}
		void ToggleButtonAllClose()
		{
			if (SelectedBtn != null && SelectedBtn.IsChecked.Value) SelectedBtn.IsChecked = false;

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

		#region Job Manager
		static string GetJobType(JobData.PolicyName Policy)
		{
			return Policy switch
			{
				JobData.PolicyName.Now => MainData.INSTANT_BACKUP_NAME,
				JobData.PolicyName.RealTime => "Real-Time",
				JobData.PolicyName.Schedule => "Schedule",
				_ => "None",
			};
		}
		void JobListUpdate()
		{
			List<JobData> NormalJobs = JobSQL.GetJobs(Environment.UserName);
			List<JobData> GlobalJobs = JobSQL.GetJobs(true);

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
							if (MainStorage != null && MainStorage.IsGlobalUser == Job.IsGlobalUser && MainStorage.ID == Job.UserID)
							{
								DetailData.StorageName = MainStorage.StorageName;
								break;
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
						if (storage.IsGlobalUser == Job.IsGlobalUser && storage.ID == Job.UserID)
						{
							Visibility BtnVisibility = Visibility.Hidden;
							if (!Job.Global && Job.JobName != MainData.INSTANT_BACKUP_NAME) BtnVisibility = Visibility.Visible;

							JobDetailList.Add(new JobDetailData(Job.HostName, Job.JobName, Job.Id)
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
					if (MainStorage != null && MainStorage.IsGlobalUser == Job.IsGlobalUser && MainStorage.ID == Job.UserID)
					{
						Visibility BtnVisibility = Visibility.Hidden;
						if (!Job.Global && Job.JobName != MainData.INSTANT_BACKUP_NAME) BtnVisibility = Visibility.Visible;

						JobDetailList.Add(new JobDetailData(Job.HostName, Job.JobName, Job.Id)
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
						break;
					}
					if (!CreateSuccess && Job.JobName == MainData.INSTANT_BACKUP_NAME)
					{
						JobDetailList.Add(new JobDetailData(Job.HostName, Job.JobName, Job.Id)
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

			for (int index = JobDetailList.Count - 1; index >= 0; index--)
				if (JobDetailList[index].Delete) JobDetailList.RemoveAt(index);

			L_JobList.Items.Refresh();
		}
		void Btn_JobListUpdate(object sender, RoutedEventArgs e)
		{
			JobListUpdate();
		}
		bool DuplicateJobCheck(string JobName)
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
		bool CreateJob(string _JobName, JobData.PolicyName _Policy)
		{
			if (Utility.FileNameSpecialCharactersErrorCheck(_JobName))
			{
				Utility.ErrorMessageBox("Job 이름에 다음문자를 사용할 수 없습니다.\n[\\, /, :, *, ?, \", <, >, |]", Title);
				return false;
			}
			if (DuplicateJobCheck(_JobName))
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

				log.Info($"Create New Job : {Data.JobName}");
				return true;
			}
			catch (Exception ex)
			{
				log.Error(ex);
				return false;
			}
		}

		void Btn_CreateRealTimeJob(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(T_RealTimeName.Text))
			{
				string JobName = T_RealTimeName.Text;
				if (CreateJob(JobName, JobData.PolicyName.RealTime))
				{
					T_RealTimeName.Text = string.Empty;
					B_RealTimeToggle.IsChecked = false;
				}
			}
		}
		void Btn_RealTimeToggleClose(object sender, RoutedEventArgs e)
		{
			B_RealTimeToggle.IsChecked = false;
			T_RealTimeName.Text = string.Empty;
		}
		void Txb_RealTimeTextEnterEvent(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return && !string.IsNullOrWhiteSpace(T_RealTimeName.Text))
			{
				string JobName = T_RealTimeName.Text;
				if (CreateJob(JobName, JobData.PolicyName.RealTime))
				{
					T_RealTimeName.Text = string.Empty;
					B_RealTimeToggle.IsChecked = false;
				}
			}
		}

		void Btn_CreateScheduleJob(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(T_ScheduleName.Text))
			{
				string JobName = T_ScheduleName.Text;
				if (CreateJob(JobName, JobData.PolicyName.Schedule))
				{
					T_ScheduleName.Text = string.Empty;
					B_ScheduleToggle.IsChecked = false;
				}
			}
		}
		void Btn_ScheduleToggleClose(object sender, RoutedEventArgs e)
		{
			B_ScheduleToggle.IsChecked = false;
			T_ScheduleName.Text = string.Empty;
		}

		void Txb_ScheduleTextEnterEvent(object sender, KeyEventArgs e)
		{

			if (e.Key == Key.Return && !string.IsNullOrWhiteSpace(T_ScheduleName.Text))
			{
				string JobName = T_ScheduleName.Text;
				B_ScheduleToggle.IsChecked = false;
				if (CreateJob(JobName, JobData.PolicyName.Schedule))
				{
					T_ScheduleName.Text = string.Empty;
					B_ScheduleToggle.IsChecked = false;
				}
			}
		}

		void Btn_RealTimeToggleClick(object sender, RoutedEventArgs e) { ToggleButtonAllClose((ToggleButton)sender); T_RealTimeName.Focus(); }
		void Btn_ScheduleToggleClick(object sender, RoutedEventArgs e) { ToggleButtonAllClose((ToggleButton)sender); T_ScheduleName.Focus(); }

		void Btn_DeleteJobList(object sender, RoutedEventArgs e)
		{
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
				log.Error(ex);
			}
			if (DeleteFlag)
			{
				SelectedBtn = null;
				JobListUpdate();
			}
			else Utility.ErrorMessageBox("Job 삭제 실패!", Title);
		}
		void Btn_DeleteJobListClose(object sender, RoutedEventArgs e)
		{
			if (SelectedBtn != null)
			{
				SelectedBtn.IsChecked = false;
			}

		}
		void ToggleBtn_DeleteJob(object sender, RoutedEventArgs e)
		{
			SelectedBtn = (ToggleButton)sender;
			ToggleButtonAllClose((ToggleButton)sender);
		}

		#endregion Job Manager

		#region Credential Manager

		void MainStorageUpdate()
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

				if (MainStorage == null)
				{
					MainStorage = new StorageData(GlobalUser.Id, GlobalUser.HostName, GlobalUser.UserName)
					{
						StorageName = GlobalUser.StorageName,
						URL = GlobalUser.URL,
						AccessKey = GlobalUser.AccessKey,
						AccessSecret = GlobalUser.SecretKey,
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
		void MainStorageUIUpdate()
		{
			if (MainStorage != null)
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
		void StorageListUpdate()
		{
			MainStorageUpdate();

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
					StorageList.Add(new StorageData(User.Id, User.HostName, User.UserName)
					{
						StorageName = User.StorageName,
						URL = User.URL,
						AccessKey = User.AccessKey,
						AccessSecret = User.SecretKey,
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

			StorageUIUpdate();

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
		void StorageUIUpdate()
		{
			for (int i = 0; i < StorageUIList.Count; i++)
			{
				if (i < StorageList.Count)
				{
					StorageUIList[i].Main.Dispatcher.Invoke(delegate
					{
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
							StorageUIList[i].Total.Content = StorageList[i].StrTotalSize;
							StorageUIList[i].Used.Content = StorageList[i].StrUsedSize;
							StorageUIList[i].Free.Content = StorageList[i].StrFreeSize;
						}
						GraphDrawing(StorageList[i].Rate, StorageUIList[i].Graph);
					});
				}
				else StorageUIList[i].Main.Visibility = Visibility.Hidden;
			}
		}
		void Btn_StorageUpdate(object sender, RoutedEventArgs e)
		{
			StorageUIUpdate();
			MainStorageUIUpdate();
		}

		void Btn_StorageToggleClick(object sender, RoutedEventArgs e) { ToggleButtonAllClose((ToggleButton)sender); T_AddStorageName.Focus(); }

		static bool CheckConnect(S3Client Client)
		{
			try
			{
				Client.ListBuckets();
				return true;
			}
			catch (AmazonS3Exception e) { log.Error(e); return false; }
			catch (Exception e) { log.Error(e); return false; }
		}

		static bool RegionEndpointCheck(string SystemName)
		{
			RegionEndpoint Endpoint = RegionEndpoint.GetBySystemName(SystemName);
			if (Endpoint.DisplayName.Equals(MainData.UNKNOWN)) return false;
			return true;
		}

		static bool LoginTest(UserData User)
		{
			try
			{
				var Client = new S3Client(User);
				if (CheckConnect(Client))
				{
					log.Info($"Login Success : {User.UserName}");
					return true;
				}
				else
				{
					log.Info($"Login Fail : {User.UserName}");
					return false;
				}
			}
			catch (AmazonS3Exception e)
			{
				log.Fatal(e);
				return false;
			}
		}

		static bool BucketNameCheck(string BucketName)
		{
			// 정규식으로 버킷 이름 체크.
			// 버킷 이름은 소문자, 숫자, 점(.), 대시(-)만 가능.
			// 길이는 3~63자
			string pattern = @"^[a-z0-9.-]{3,63}$";
			return !System.Text.RegularExpressions.Regex.IsMatch(BucketName, pattern);
		}
		void CreateCredential()
		{

			if (string.IsNullOrWhiteSpace(T_AddStorageName.Text)) { Utility.ErrorMessageBox("Storage Credential Name is Empty!", Title); T_AddStorageName.Focus(); return; }
			if (string.IsNullOrWhiteSpace(T_AddStorageURL.Text)) { Utility.ErrorMessageBox("URL is Empty!", Title); T_AddStorageURL.Focus(); return; }
			if (string.IsNullOrWhiteSpace(T_AddStorageAccessKey.Text)) { Utility.ErrorMessageBox("Access Key is Empty!", Title); T_AddStorageAccessKey.Focus(); return; }
			if (string.IsNullOrWhiteSpace(T_AddStorageSecretKey.Text)) { Utility.ErrorMessageBox("Secret Key is Empty!", Title); T_AddStorageSecretKey.Focus(); return; }
			if (string.IsNullOrWhiteSpace(T_AddStorageUserName.Text)) { Utility.ErrorMessageBox("User Name (Bucket) is Empty!", Title); T_AddStorageUserName.Focus(); return; }

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
			if (!URL.StartsWith(MainData.HTTP, StringComparison.OrdinalIgnoreCase) && !RegionEndpointCheck(URL))
			{
				Utility.ErrorMessageBox("Region Endpoint Check fail!", Title);
				T_AddStorageUserName.Focus();
				return;
			}
			string S3FileManagerURL = "";
			if (MainStorage != null) S3FileManagerURL = MainStorage.S3FileManagerURL;

			var User = new UserData
			{
				HostName = Environment.UserName,
				URL = T_AddStorageURL.Text.Trim(),
				AccessKey = T_AddStorageAccessKey.Text.Trim(),
				SecretKey = T_AddStorageSecretKey.Text.Trim(),
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
					T_AddStorageName.Text = string.Empty;
					T_AddStorageURL.Text = string.Empty;
					T_AddStorageAccessKey.Text = string.Empty;
					T_AddStorageSecretKey.Text = string.Empty;
					T_AddStorageUserName.Text = string.Empty;
					B_StorageToggle.IsChecked = false;
				}
			}
		}
		void CreateCredentialEvent(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) CreateCredential(); }
		void Btn_CreateCredential(object sender, RoutedEventArgs e) { CreateCredential(); }
		void Btn_CredentialToggleClose(object sender, RoutedEventArgs e)
		{
			B_StorageToggle.IsChecked = false;
			T_AddStorageName.Text = string.Empty;
			T_AddStorageURL.Text = string.Empty;
			T_AddStorageAccessKey.Text = string.Empty;
			T_AddStorageSecretKey.Text = string.Empty;
			T_AddStorageUserName.Text = string.Empty;
		}

		void DefaultStorageSave()
		{
			if (string.IsNullOrWhiteSpace(T_StorageName.Text)) { Utility.ErrorMessageBox("Storage Name is Empty", Title); return; }
			if (string.IsNullOrWhiteSpace(T_EditMainIP.Text)) { Utility.ErrorMessageBox("IP Address is Empty", Title); return; }
			if (string.IsNullOrWhiteSpace(T_PortNumber.Text)) { Utility.ErrorMessageBox("Port Number is Empty", Title); return; }
			if (string.IsNullOrWhiteSpace(T_AditPCName.Text)) { Utility.ErrorMessageBox("PCName is Empty", Title); return; }

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
					log.Error(ex);
					Utility.ErrorMessageBox($"Global User Get Error : {ex.Message}", Title);
					return;
				}
				WatcherConfigs.IP = Address;
				WatcherConfigs.Port = Port;
			}
			else
			{
				if (!MainStorage.StorageName.Equals(StorageName) && !UserSQL.UpdateUserStorageName(MainStorage.ID, StorageName, true))
				{
					Utility.ErrorMessageBox("StorageName save failed!", Title);
					return;

				}
				if (!MainStorage.S3FileManagerURL.Equals(S3FileManagerURL) && !UserSQL.UpdateUserS3FileManagerURL(MainStorage.ID, S3FileManagerURL, true))
				{
					Utility.ErrorMessageBox("S3 File Manager URL save failed!", Title);
					return;
				}

			}

			WatcherConfigs.PcName = T_AditPCName.Text;
			if (!string.IsNullOrWhiteSpace(T_AditEmail.Text)) WatcherConfigs.Email = T_AditEmail.Text;
			B_Edit1.IsChecked = false;
			StorageListUpdate();
		}

		void Btn_Edit1Save(object sender, RoutedEventArgs e)
		{
			DefaultStorageSave();
		}
		void Btn_Edit1Close(object sender, RoutedEventArgs e)
		{
			B_Edit1.IsChecked = false;
		}
		void MainStorageSaveEvent(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) DefaultStorageSave();
		}

		void Btn_PopupAllClose(object sender, RoutedEventArgs e) { ToggleButtonAllClose(); }

		void Btn_Edit1(object sender, RoutedEventArgs e)
		{
			ToggleButtonAllClose((ToggleButton)sender);
			try
			{
				T_StorageName.Focus();
				string IP = WatcherConfigs.IP;
				string Port = WatcherConfigs.Port;
				T_StorageName.Text = L_StorageName_1.Text.ToString();
				if (!string.IsNullOrWhiteSpace(IP)) { T_EditMainIP.Text = IP; T_EditMainIP.IsEnabled = false; }
				if (!string.IsNullOrWhiteSpace(Port)) { T_PortNumber.Text = Port; T_PortNumber.IsEnabled = false; }

				if (string.IsNullOrWhiteSpace(WatcherConfigs.PcName)) WatcherConfigs.PcName = Environment.MachineName;
				T_AditPCName.Text = WatcherConfigs.PcName;
				T_AditEmail.Text = WatcherConfigs.Email;
				T_S3FileManagerURL.Text = MainStorage.S3FileManagerURL;
				T_StorageName.Focus();
			}
			catch (Exception ex)
			{
				log.Error(ex);
			}
		}

		void SetStorageEditText(int index)
		{
			if (index >= StorageUIList.Count) return;

			StorageUIList[index].Box_StorageName.Text = StorageList[index].StorageName;
			StorageUIList[index].Box_URL.Text = StorageList[index].URL;
			StorageUIList[index].Box_AccessKey.Text = StorageList[index].AccessKey;
			StorageUIList[index].Box_AccessSecret.Text = StorageList[index].AccessSecret;
			StorageUIList[index].Box_UserName.Text = StorageList[index].UserName;
			StorageUIList[index].Box_S3FileManagerURL.Text = StorageList[index].S3FileManagerURL;

			StorageUIList[index].Box_URL.IsEnabled = false;
			StorageUIList[index].Box_AccessKey.IsEnabled = false;
			StorageUIList[index].Box_AccessSecret.IsEnabled = false;
			StorageUIList[index].Box_UserName.IsEnabled = false;

			StorageUIList[index].Box_StorageName.Focus();
		}
		void Btn_Edit(object sender, RoutedEventArgs e)
		{
			ToggleButtonAllClose((ToggleButton)sender);

			if (int.TryParse(((ToggleButton)sender).Tag.ToString(), out int Index))
			{
				SetStorageEditText(Index);
			}
		}
		void Btn_Delete(object sender, RoutedEventArgs e) { ToggleButtonAllClose((ToggleButton)sender); }

		void SaveStorage(int index)
		{
			if (index >= StorageUIList.Count) return;

			//저장로직
			string StorageName = StorageUIList[index].Box_StorageName.Text;
			string S3FileManagerURL = StorageUIList[index].Box_S3FileManagerURL.Text;

			if (!StorageList[index].StorageName.Equals(StorageName) && !UserSQL.UpdateUserStorageName(StorageList[index].ID, StorageName, false))
			{
				Utility.ErrorMessageBox("StorageName save failed!", Title);
				return;
			}
			if (!StorageList[index].S3FileManagerURL.Equals(S3FileManagerURL) && !UserSQL.UpdateUserS3FileManagerURL(StorageList[index].ID, S3FileManagerURL, false))
			{
				Utility.ErrorMessageBox("S3 File Manager URL save failed!", Title);
				return;
			}
			StorageUIList[index].PopupButton.IsChecked = false;
			StorageListUpdate();
		}

		void StorageSaveEvent(object sender, KeyEventArgs e)
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

		void BtnEditSave(object sender, RoutedEventArgs e)
		{
			Button Box = (Button)sender;

			if (int.TryParse(Box.Tag.ToString(), out int Index))
			{
				SaveStorage(Index);
			}
		}
		void DeleteStorage(int index)
		{
			if (index >= StorageUIList.Count) return;

			int UserID = StorageList[index].ID;
			string HostName = StorageList[index].HostName;

			//Check Instant
			{
				TabItem Item = MainTab.Items[1] as TabItem;
				JobTab Tab = Item.Content as JobTab;

				if (Tab.BackupStart && !Tab.Job.IsGlobalUser && Tab.Job.UserID == UserID)
				{
					Utility.ErrorMessageBox("Instant가 백업을 진행중입니다. \n스토리지를 삭제하고 싶으시면 Instant 백업을 중단하십시오.", Title);
					return;
				}
			}

			//관련 job 삭제
			for (int i = MainTab.Items.Count; i > 1; i--)
			{
				try
				{
					TabItem Item = MainTab.Items[i] as TabItem;
					JobTab Tab = Item.Content as JobTab;
					if (Tab.Job.UserID == UserID && Tab.Job.HostName == HostName)
					{
						Tab.Delete();
						MainTab.Items.RemoveAt(i);
					}
				}
				catch (Exception e)
				{
					log.Error(e);
				}
			}
			//User 삭제
			UserSQL.DeleteUserToId(UserID, false);

			StorageListUpdate();
			JobListUpdate();
		}
		void Btn_EditDelete(object sender, RoutedEventArgs e)
		{
			Button Box = (Button)sender;

			if (int.TryParse(Box.Tag.ToString(), out int Index))
			{
				DeleteStorage(Index);
			}
			ToggleButtonAllClose();
		}

		#region Graph
		void GraphDrawing(double Rate, StackPanel Graph)
		{
			Graph.Children.Clear();
			double Angle = MaxAngle * Rate / 100;

			if (Angle >= MaxAngle - 1) Angle = MaxAngle - 1;
			if (Angle < 1) Angle = 1;

			var Center = new Point(Graph.ActualWidth / 2, Graph.ActualHeight / 2);
			double RadiusX = Center.X;
			//double RadiusY = Center.Y;
			double angle = Angle;

			bool fullEllipse = angle >= MaxAngle;
			if (fullEllipse) angle = MaxAngle / 2;


			var arcPath = new Path
			{
				StrokeThickness = 0,
				Data = new PathGeometry
				{
					Figures = [
						new PathFigure
						{
							StartPoint = Center,
							Segments = [
								new LineSegment()
								{
									Point = new Point(Center.X + RadiusX, Center.Y),
									IsStroked = false
								},
								new ArcSegment
								{
									Point = new Point(Center.X + RadiusX * MyCos(angle), Center.Y - RadiusX * MySin(angle)),
									Size = new Size(Center.X, Center.Y),
									IsLargeArc = angle > MaxAngle / 2,
									SweepDirection = SweepDirection.Counterclockwise
								}]
						}
					]
				},
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
		void Window_Closing(object sender, CancelEventArgs e)
		{
			bool first = true;
			foreach (TabItem item in MainTab.Items)
			{
				if (first) { first = false; continue; }
				JobTab temp = item.Content as JobTab;
				temp.Close();
			}
		}
		void MainTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (MainTab.SelectedIndex == 0) L_MainTitle.Text = "IfsSync2 Service Summary";
			else if (MainTab.SelectedIndex == 1) L_MainTitle.Text = "Instant Backup";
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
		void PopupTextBoxMouseUp(object sender, MouseButtonEventArgs e)
		{
			try
			{
				Activate();
				TextBox Text = (TextBox)sender;
				Keyboard.Focus(Text);

			}
			catch (Exception ex) { Console.WriteLine(ex.Message); }
		}

		void JobListDoubleClickEvent(object sender, MouseButtonEventArgs e)
		{
			if (L_JobList.SelectedIndex >= 0)
			{
				int index = L_JobList.SelectedIndex + 1 - GlobalCount;
				MainTab.SelectedIndex = index;
			}
		}
	}
}
