/*
* Copyright (c) 2021 PSPACE, inc. KSAN Development Team ksan@pspace.co.kr
* KSAN is a suite of free software: you can redistribute it and/or modify it under the terms of
* the GNU General Public License as published by the Free Software Foundation, either version 
* 3 of the License. See LICENSE for details
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
using System.Runtime.Versioning;

[assembly: XmlConfigurator(ConfigFile = "IfsSync2UILogConfig.xml", Watch = true)]

namespace IfsSync2UI
{
	[SupportedOSPlatform("windows10.0")]
	public partial class MainWindow : Window
	{
		const double MAX_ANGLE = 360;
		const string NULL_STORAGE_NAME = "N/A";

		static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		readonly ObservableCollection<JobDetailData> _jobDetailList = [];

		StorageData _mainStorage = null;
		readonly StorageUI _mainStorageUI = null;

		readonly List<StorageData> _storageList = [];
		readonly List<StorageUI> _storageUIList = [];

		readonly WatcherConfig _watcherConfigs;

		/***************************** SQL ************************************/
		readonly JobDbManager _jobSQL;
		readonly UserDbManager _userSQL;
		/***************************** Timer ***********************************/
		readonly System.Timers.Timer _updateJobListTimer;
		readonly System.Timers.Timer _updateStorageListTimer;

		const int JOB_TIMER_DELAY = 5000;
		const int STORAGE_TIMER_DELAY = 60000;
		/**************************Toggle Button*******************************/
		ToggleButton _selectedBtn = null;
		Mutex _mutex = null;
		int _globalCount = 0;

		public MainWindow()
		{
			DuplicateExecution(MainData.MUTEX_NAME_UI);

			MainUtility.DeleteOldLogs(MainData.GetLogFolder("IfsSync2UI"));
			_log.Info("Main Start");
			_watcherConfigs = new WatcherConfig(true);
			_jobSQL = new JobDbManager();
			_userSQL = new UserDbManager();

			InitializeComponent();
			_mainStorageUI = new StorageUI(Storage_1, L_StorageName_1, L_URL_1, P_Graph_1, L_SizeRate_1, L_Total_1, L_Used_1, L_Free_1, T_StorageName, T_S3FileManagerURL);
			_storageUIList.Add(new StorageUI(Storage_2, L_StorageName_2, L_URL_2, P_Graph_2, L_SizeRate_2, L_Total_2, L_Used_2, L_Free_2, T_EditStorage_2, T_EditS3FileManagerURL_2, B_Edit_2, T_EditURL_2, T_EditAccessKey_2, T_EditAccessSecret_2, T_EditUserName_2, T_EmptyStorage_2, G_Quota_2));
			_storageUIList.Add(new StorageUI(Storage_3, L_StorageName_3, L_URL_3, P_Graph_3, L_SizeRate_3, L_Total_3, L_Used_3, L_Free_3, T_EditStorage_3, T_EditS3FileManagerURL_3, B_Edit_3, T_EditURL_3, T_EditAccessKey_3, T_EditAccessSecret_3, T_EditUserName_3, T_EmptyStorage_3, G_Quota_3));
			_storageUIList.Add(new StorageUI(Storage_4, L_StorageName_4, L_URL_4, P_Graph_4, L_SizeRate_4, L_Total_4, L_Used_4, L_Free_4, T_EditStorage_4, T_EditS3FileManagerURL_4, B_Edit_4, T_EditURL_4, T_EditAccessKey_4, T_EditAccessSecret_4, T_EditUserName_4, T_EmptyStorage_4, G_Quota_4));
			L_JobList.ItemsSource = _jobDetailList;

			TabInit();
			StorageListUpdate();
			JobListUpdate();

			_updateJobListTimer = new System.Timers.Timer { Interval = JOB_TIMER_DELAY };
			_updateJobListTimer.Elapsed += UpdateJobList;
			_updateJobListTimer.Start();

			_updateStorageListTimer = new System.Timers.Timer { Interval = STORAGE_TIMER_DELAY };
			_updateStorageListTimer.Elapsed += UpdateStorageList;
			_updateStorageListTimer.Start();

			Title += " V" + MainData.GetVersion();
		}
		void DuplicateExecution(string mutexName)
		{
			try
			{
				_mutex = new Mutex(false, mutexName);
			}
			catch (Exception)
			{
				Application.Current.Shutdown();
			}
			if (!_mutex.WaitOne(0, false)) Application.Current.Shutdown();

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
				JobList = _jobSQL.GetJobs(Environment.UserName);
			}
			catch { return; }

			if (JobList.Count == 0)
			{
				//Instant Backup
				var instant = new JobData()
				{
					JobName = MainData.INSTANT_BACKUP_NAME,
					HostName = Environment.UserName,
					StrPolicy = JobData.PolicyType.Now.ToString()
				};
				_jobSQL.PutJobData(instant);
				instant.Id = _jobSQL.GetJobDataId(instant.HostName, instant.JobName);
				JobList.Add(instant);
			}
			foreach (var Job in JobList)
			{
				try
				{
					MainTab.Dispatcher.Invoke(delegate
					{
						var item = new TabItem();

						var tabItemContent = new JobTab(item, Job);
						item.Content = tabItemContent;

						item.Header = Job.JobName;
						MainTab.Items.Add(item);
					});
					_log.Info($"Load JobData : {Job.JobName}");
				}
				catch (Exception ex)
				{
					_log.Error(ex);
				}
			}

			MainTab.SelectedIndex = 0;
		}
		void ToggleButtonAllClose(ToggleButton toggle)
		{

			if (toggle != _selectedBtn && _selectedBtn != null && _selectedBtn.IsChecked.Value) _selectedBtn.IsChecked = false;

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
			if (_selectedBtn != null && _selectedBtn.IsChecked.Value) _selectedBtn.IsChecked = false;

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
		static string GetJobType(JobData.PolicyType Policy)
		{
			return Policy switch
			{
				JobData.PolicyType.Now => MainData.INSTANT_BACKUP_NAME,
				JobData.PolicyType.RealTime => "Real-Time",
				JobData.PolicyType.Schedule => "Schedule",
				_ => "None",
			};
		}
		void JobListUpdate()
		{
			List<JobData> NormalJobs = _jobSQL.GetJobs(Environment.UserName);
			List<JobData> GlobalJobs = _jobSQL.GetJobs();

			List<JobData> JobList = [.. GlobalJobs, .. NormalJobs];

			_globalCount = GlobalJobs.Count;

			//JobDetailData Check
			foreach (JobDetailData DetailData in _jobDetailList) DetailData.Delete = true;

			foreach (JobData Job in JobList)
			{
				bool IsNewJob = true;

				foreach (JobDetailData DetailData in _jobDetailList)
				{
					if (DetailData.JobName == Job.JobName)
					{
						IsNewJob = false;
						DetailData.Delete = false;

						DetailData.ExtensionList = Job.WhiteFileExt;
						if (Job.UserId == -1) DetailData.StorageName = NULL_STORAGE_NAME;
						else
						{
							if (_mainStorage != null && _mainStorage.IsGlobalUser == Job.IsGlobalUser && _mainStorage.Id == Job.UserId)
							{
								DetailData.StorageName = _mainStorage.StorageName;
								break;
							}
							foreach (var storage in _storageList)
								if (storage.IsGlobalUser == Job.IsGlobalUser && storage.Id == Job.UserId)
									DetailData.StorageName = storage.StorageName;

						}
						break;
					}
				}

				if (IsNewJob)
				{
					bool CreateSuccess = false;
					foreach (var storage in _storageList)
					{
						if (storage.IsGlobalUser == Job.IsGlobalUser && storage.Id == Job.UserId)
						{
							Visibility BtnVisibility = Visibility.Hidden;
							if (!Job.Global && Job.JobName != MainData.INSTANT_BACKUP_NAME) BtnVisibility = Visibility.Visible;

							_jobDetailList.Add(new JobDetailData(Job.HostName, Job.JobName, Job.Id)
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
					if (_mainStorage != null && _mainStorage.IsGlobalUser == Job.IsGlobalUser && _mainStorage.Id == Job.UserId)
					{
						Visibility BtnVisibility = Visibility.Hidden;
						if (!Job.Global && Job.JobName != MainData.INSTANT_BACKUP_NAME) BtnVisibility = Visibility.Visible;

						_jobDetailList.Add(new JobDetailData(Job.HostName, Job.JobName, Job.Id)
						{
							JobType = GetJobType(Job.Policy),
							ExtensionList = Job.WhiteFileExt,
							StorageName = _mainStorage.StorageName,
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
						_jobDetailList.Add(new JobDetailData(Job.HostName, Job.JobName, Job.Id)
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

			for (int index = _jobDetailList.Count - 1; index >= 0; index--)
				if (_jobDetailList[index].Delete) _jobDetailList.RemoveAt(index);

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
			if (_jobSQL.IsJobName(Environment.UserName, JobName)) return true;
			return false;
		}
		bool CreateJob(string _JobName, JobData.PolicyType _Policy)
		{
			if (Utility.SpecialCharactersErrorCheck(_JobName))
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
				JobData Data = new()
				{
					JobName = _JobName,
					HostName = Environment.UserName,
					Policy = _Policy
				};
				MainTab.Dispatcher.Invoke(delegate
				{
					TabItem item = new() { Header = Data.JobName };

					JobTab tabItemContent = new(item, Data, true);

					item.Content = tabItemContent;
					MainTab.Items.Add(item);
					MainTab.SelectedIndex = MainTab.Items.Count - 1;
				});

				_log.Info($"Create New Job : {Data.JobName}");
				return true;
			}
			catch (Exception ex)
			{
				_log.Error(ex);
				return false;
			}
		}

		void Btn_CreateRealTimeJob(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(T_RealTimeName.Text))
			{
				string JobName = T_RealTimeName.Text;
				if (CreateJob(JobName, JobData.PolicyType.RealTime))
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
				if (CreateJob(JobName, JobData.PolicyType.RealTime))
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
				if (CreateJob(JobName, JobData.PolicyType.Schedule))
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
				if (CreateJob(JobName, JobData.PolicyType.Schedule))
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
				_log.Error(ex);
			}
			if (DeleteFlag)
			{
				_selectedBtn = null;
				JobListUpdate();
			}
			else Utility.ErrorMessageBox("Job 삭제 실패!", Title);
		}
		void Btn_DeleteJobListClose(object sender, RoutedEventArgs e)
		{
			if (_selectedBtn != null)
			{
				_selectedBtn.IsChecked = false;
			}

		}
		void ToggleBtn_DeleteJob(object sender, RoutedEventArgs e)
		{
			_selectedBtn = (ToggleButton)sender;
			ToggleButtonAllClose((ToggleButton)sender);
		}

		#endregion Job Manager

		#region Credential Manager

		void MainStorageUpdate()
		{
			List<UserData> globalUsers = _userSQL.GetUsers(true);

			if (globalUsers.Count == 0)
			{
				T_EmptyStorage.Visibility = Visibility.Visible;
				DefaultGraph.Visibility = Visibility.Hidden;
			}
			else
			{
				UserData globalUser = globalUsers[0];

				if (_mainStorage == null)
				{
					_mainStorage = new StorageData(globalUser.Id, globalUser.HostName, globalUser.UserName)
					{
						StorageName = globalUser.StorageName,
						URL = globalUser.URL,
						AccessKey = globalUser.AccessKey,
						AccessSecret = globalUser.SecretKey,
						S3FileManagerURL = globalUser.S3FileManagerURL,
						Delete = false
					};
				}
				else
				{
					_mainStorage.StorageName = globalUser.StorageName;
					_mainStorage.S3FileManagerURL = globalUser.S3FileManagerURL;
					_mainStorage.Delete = false;
				}
				MainStorageUIUpdate();
			}
		}
		void MainStorageUIUpdate()
		{
			if (_mainStorage != null)
			{
				_mainStorageUI.Main.Dispatcher.Invoke(delegate
				{
					_mainStorageUI.Main.Visibility = Visibility.Visible;
					_mainStorageUI.StorageName.Text = _mainStorage.StorageName;
					_mainStorageUI.URL.Text = _mainStorage.URL;

					_mainStorageUI.SizeRate.Content = _mainStorage.StrRate;
					_mainStorageUI.Total.Content = _mainStorage.StrTotalSize;
					_mainStorageUI.Used.Content = _mainStorage.StrUsedSize;
					_mainStorageUI.Free.Content = _mainStorage.StrFreeSize;
					GraphDrawing(_mainStorage.Rate, _mainStorageUI.Graph);
				});


				T_EmptyStorage.Visibility = Visibility.Hidden;
				DefaultGraph.Visibility = Visibility.Visible;
			}
		}
		void StorageListUpdate()
		{
			MainStorageUpdate();

			List<UserData> NormalUsers = _userSQL.GetUsers(Environment.UserName);

			foreach (var Storage in _storageList) Storage.Delete = true;

			//New Storage Check
			foreach (var User in NormalUsers)
			{
				bool IsNewUser = true;

				foreach (var Storage in _storageList)
				{
					if (Storage.UserName == User.UserName)
					{
						IsNewUser = false;
						Storage.StorageName = User.StorageName;
						Storage.S3FileManagerURL = User.S3FileManagerURL;
						Storage.Delete = false;
						break;
					}
				}

				if (IsNewUser)
				{
					_storageList.Add(new StorageData(User.Id, User.HostName, User.UserName)
					{
						StorageName = User.StorageName,
						URL = User.URL,
						AccessKey = User.AccessKey,
						AccessSecret = User.SecretKey,
						S3FileManagerURL = User.S3FileManagerURL,
						Delete = false
					});
				}

			}

			//Storage Delete Check
			for (int i = _storageList.Count - 1; i >= 0; i--)
			{
				if (_storageList[i].Delete)
				{
					try
					{
						_storageList.RemoveAt(i);
					}
					catch (Exception ex)
					{
						_log.Error(ex);
					}
				}
			}

			StorageUIUpdate();

			if (_storageList.Count >= _storageUIList.Count)
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
			for (int i = 0; i < _storageUIList.Count; i++)
			{
				if (i < _storageList.Count)
				{
					_storageUIList[i].Main.Dispatcher.Invoke(delegate
					{
						_storageUIList[i].Main.Visibility = Visibility.Visible;
						_storageUIList[i].StorageName.Text = _storageList[i].StorageName;
						_storageUIList[i].URL.Text = _storageList[i].URL;
						if (_storageList[i].IsAWS)
						{
							_storageUIList[i].Black_AWSMessage.Visibility = Visibility.Visible;
							_storageUIList[i].Grid_Quota.Visibility = Visibility.Hidden;
						}
						else
						{
							_storageUIList[i].Black_AWSMessage.Visibility = Visibility.Hidden;
							_storageUIList[i].Grid_Quota.Visibility = Visibility.Visible;

							_storageUIList[i].SizeRate.Content = _storageList[i].StrRate;
							_storageUIList[i].Total.Content = _storageList[i].StrTotalSize;
							_storageUIList[i].Used.Content = _storageList[i].StrUsedSize;
							_storageUIList[i].Free.Content = _storageList[i].StrFreeSize;
						}
						GraphDrawing(_storageList[i].Rate, _storageUIList[i].Graph);
					});
				}
				else _storageUIList[i].Main.Visibility = Visibility.Hidden;
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
			catch (AmazonS3Exception e) { _log.Error(e); return false; }
			catch (Exception e) { _log.Error(e); return false; }
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
					_log.Info($"Login Success : {User.UserName}");
					return true;
				}
				else
				{
					_log.Info($"Login Fail : {User.UserName}");
					return false;
				}
			}
			catch (AmazonS3Exception e)
			{
				_log.Fatal(e);
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
			string userName = T_AddStorageUserName.Text;
			if (BucketNameCheck(userName))
			{
				Utility.ErrorMessageBox("User Name (Bucket) is not correct!\nBucket name should contain only \nlowercase letters, numbers, periods(.) and dashes(-)", Title);
				return;
			}

			if (_userSQL.IsUserName(userName, false))
			{
				Utility.ErrorMessageBox("User name (Bucket) already exists.", Title);
				return;
			}
			string url = T_AddStorageURL.Text.Trim();
			if (!url.StartsWith(MainData.HTTP, StringComparison.OrdinalIgnoreCase) && !RegionEndpointCheck(url))
			{
				Utility.ErrorMessageBox("Region Endpoint Check fail!", Title);
				T_AddStorageUserName.Focus();
				return;
			}
			string s3FileManagerURL = "";
			if (_mainStorage != null) s3FileManagerURL = _mainStorage.S3FileManagerURL;

			var user = new UserData
			{
				HostName = Environment.UserName,
				URL = T_AddStorageURL.Text.Trim(),
				AccessKey = T_AddStorageAccessKey.Text.Trim(),
				SecretKey = T_AddStorageSecretKey.Text.Trim(),
				StorageName = T_AddStorageName.Text.Trim(),
				UserName = T_AddStorageUserName.Text.Trim(),
				S3FileManagerURL = s3FileManagerURL
			};

			if (!LoginTest(user)) Utility.ErrorMessageBox("Login Test Failed!", Title);
			else
			{
				if (!_userSQL.InsertUser(user, false)) Utility.ErrorMessageBox("User Data is not Save!", Title);
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

			if (string.IsNullOrWhiteSpace(_watcherConfigs.IP))
			{
				string Address = T_EditMainIP.Text;
				string Port = T_PortNumber.Text;

				string URL = MainData.CreateAddress(Address, Port);

				try
				{
					UserData GlobalUser = Utility.GetGlobalUser(URL, _watcherConfigs.PcName);
					GlobalUser.StorageName = StorageName;

					if (string.IsNullOrWhiteSpace(S3FileManagerURL)) GlobalUser.S3FileManagerURL = MainData.CreateS3FileManagerURL(Address);
					else GlobalUser.S3FileManagerURL = S3FileManagerURL;

					if (!_userSQL.InsertUser(GlobalUser, true)) return;
				}
				catch (Exception ex)
				{
					_log.Error(ex);
					Utility.ErrorMessageBox($"Global User Get Error : {ex.Message}", Title);
					return;
				}
				_watcherConfigs.IP = Address;
				_watcherConfigs.Port = Port;
			}
			else
			{
				if (!_mainStorage.StorageName.Equals(StorageName) && !_userSQL.UpdateUserStorageName(_mainStorage.Id, StorageName, true))
				{
					Utility.ErrorMessageBox("StorageName save failed!", Title);
					return;

				}
				if (!_mainStorage.S3FileManagerURL.Equals(S3FileManagerURL) && !_userSQL.UpdateUserS3FileManagerURL(_mainStorage.Id, S3FileManagerURL, true))
				{
					Utility.ErrorMessageBox("S3 File Manager URL save failed!", Title);
					return;
				}

			}

			_watcherConfigs.PcName = T_AditPCName.Text;
			if (!string.IsNullOrWhiteSpace(T_AditEmail.Text)) _watcherConfigs.Email = T_AditEmail.Text;
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
				string IP = _watcherConfigs.IP;
				string Port = _watcherConfigs.Port;
				T_StorageName.Text = L_StorageName_1.Text.ToString();
				if (!string.IsNullOrWhiteSpace(IP)) { T_EditMainIP.Text = IP; T_EditMainIP.IsEnabled = false; }
				if (!string.IsNullOrWhiteSpace(Port)) { T_PortNumber.Text = Port; T_PortNumber.IsEnabled = false; }

				if (string.IsNullOrWhiteSpace(_watcherConfigs.PcName)) _watcherConfigs.PcName = Environment.MachineName;
				T_AditPCName.Text = _watcherConfigs.PcName;
				T_AditEmail.Text = _watcherConfigs.Email;
				T_S3FileManagerURL.Text = _mainStorage.S3FileManagerURL;
				T_StorageName.Focus();
			}
			catch (Exception ex)
			{
				_log.Error(ex);
			}
		}

		void SetStorageEditText(int index)
		{
			if (index >= _storageUIList.Count) return;

			_storageUIList[index].Box_StorageName.Text = _storageList[index].StorageName;
			_storageUIList[index].Box_URL.Text = _storageList[index].URL;
			_storageUIList[index].Box_AccessKey.Text = _storageList[index].AccessKey;
			_storageUIList[index].Box_AccessSecret.Text = _storageList[index].AccessSecret;
			_storageUIList[index].Box_UserName.Text = _storageList[index].UserName;
			_storageUIList[index].Box_S3FileManagerURL.Text = _storageList[index].S3FileManagerURL;

			_storageUIList[index].Box_URL.IsEnabled = false;
			_storageUIList[index].Box_AccessKey.IsEnabled = false;
			_storageUIList[index].Box_AccessSecret.IsEnabled = false;
			_storageUIList[index].Box_UserName.IsEnabled = false;

			_storageUIList[index].Box_StorageName.Focus();
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
			if (index >= _storageUIList.Count) return;

			//저장로직
			string StorageName = _storageUIList[index].Box_StorageName.Text;
			string S3FileManagerURL = _storageUIList[index].Box_S3FileManagerURL.Text;

			if (!_storageList[index].StorageName.Equals(StorageName) && !_userSQL.UpdateUserStorageName(_storageList[index].Id, StorageName, false))
			{
				Utility.ErrorMessageBox("StorageName save failed!", Title);
				return;
			}
			if (!_storageList[index].S3FileManagerURL.Equals(S3FileManagerURL) && !_userSQL.UpdateUserS3FileManagerURL(_storageList[index].Id, S3FileManagerURL, false))
			{
				Utility.ErrorMessageBox("S3 File Manager URL save failed!", Title);
				return;
			}
			_storageUIList[index].PopupButton.IsChecked = false;
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
			if (index >= _storageUIList.Count) return;

			int UserID = _storageList[index].Id;
			string HostName = _storageList[index].HostName;

			//Check Instant
			{
				TabItem Item = MainTab.Items[1] as TabItem;
				JobTab Tab = Item.Content as JobTab;

				if (Tab.BackupStart && !Tab.Job.IsGlobalUser && Tab.Job.UserId == UserID)
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
					if (Tab.Job.UserId == UserID && Tab.Job.HostName == HostName)
					{
						Tab.Delete();
						MainTab.Items.RemoveAt(i);
					}
				}
				catch (Exception e)
				{
					_log.Error(e);
				}
			}
			//User 삭제
			_userSQL.DeleteUserToId(UserID, false);

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
		void GraphDrawing(double rate, StackPanel graph)
		{
			graph.Children.Clear();
			double angle = MAX_ANGLE * rate / 100;

			if (angle >= MAX_ANGLE - 1) angle = MAX_ANGLE - 1;
			if (angle < 1) angle = 1;

			var center = new Point(graph.ActualWidth / 2, graph.ActualHeight / 2);
			double radiusX = center.X;
			double angleValue = angle;

			bool fullEllipse = angleValue >= MAX_ANGLE;
			if (fullEllipse) angleValue = MAX_ANGLE / 2;

			var arcPath = new Path
			{
				StrokeThickness = 0,
				Data = new PathGeometry
				{
					Figures = [
						new PathFigure
						{
							StartPoint = center,
							Segments = [
								new LineSegment()
								{
									Point = new Point(center.X + radiusX, center.Y),
									IsStroked = false
								},
								new ArcSegment
								{
									Point = new Point(center.X + radiusX * MyCos(angleValue), center.Y - radiusX * MySin(angleValue)),
									Size = new Size(center.X, center.Y),
									IsLargeArc = angleValue > MAX_ANGLE / 2,
									SweepDirection = SweepDirection.Counterclockwise
								}]
						}
					]
				},
				Fill = new SolidColorBrush(Colors.CornflowerBlue)
			};

			graph.Children.Add(arcPath);
		}
		static double MySin(double degrees)
		{
			return Math.Sin(degrees * 2d * Math.PI / MAX_ANGLE);
		} //sin
		static double MyCos(double degrees)
		{
			return Math.Cos(degrees * 2d * Math.PI / MAX_ANGLE);
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
				int index = L_JobList.SelectedIndex + 1 - _globalCount;
				MainTab.SelectedIndex = index;
			}
		}

		void SettingsButtonClieck(object sender, RoutedEventArgs e)
		{
			ConfigWindow settingsWindow = new();
			settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			settingsWindow.Show();
		}
	}
}
