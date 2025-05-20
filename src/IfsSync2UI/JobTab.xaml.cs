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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IfsSync2Common;
using System.Timers;
using System.Runtime.Versioning;

namespace IfsSync2UI
{
	[SupportedOSPlatform("windows10.0")]
	public partial class JobTab : UserControl
	{
		private readonly static string CLASS_NAME = "JobTab";
		private static readonly ILog log = LogManager.GetLogger(typeof(JobTab));

		public bool IsChanged { get; set; }
		private TabItem ThisTab { get; set; }
		private bool NewTab { get; set; }
		/**************************** User Data *************************************/
		private List<UserData> NormalUserList;
		private List<UserData> GlobalUserList;

		/***************************** Job Data *************************************/
		private static readonly string Root = "MyPC";
		private readonly ObservableCollection<TreeNode> TreeList;
		private readonly ObservableCollection<DirectoryData> DirectoryList;
		public readonly JobData Job;

		/*************************** Job State *************************************/
		private JobStatus _status = null;
		private readonly System.Timers.Timer StatusCheckTimer;
		/************************ Schedule Data *************************************/
		private readonly List<CheckBox> WeekCheckBoxList = [];

		/*************************** Db Data ***************************************/
		readonly UserDbManager _userDb;
		readonly JobDbManager _jobDb;
		readonly ExtensionDbManager _extDb;
		readonly TaskDbManager _taskDb;

		/************************ Extension List ************************************/
		private readonly List<string> ExtensionList = [];
		private readonly ObservableCollection<string> FindExtensionList = [];

		/************************* Analysis Data ************************************/
		private readonly ProgressData Analysis = new();
		private Thread AnalysisThread = null;
		public bool BackupStart { get; set; } = false;
		/*************************** VSS Data ************************************/
		private readonly List<string> VSSFileExt = [];
		private readonly string VSSFILEEXTLIST = "pst|ost|db";

		/*************************** Log Data ************************************/
		LogViewWindow LogViewWindow = null;
		private readonly System.Timers.Timer UpdateLogTimer;
		/****************************** Init *************************************/
		public JobTab(TabItem Tab, JobData job, bool _NewTab = false)
		{
			NewTab = _NewTab;
			ThisTab = Tab;
			Job = job;
			DirectoryList = [];
			_userDb = new();
			_jobDb = new();
			_extDb = new();
			_taskDb = new(job.JobName);

			InitializeComponent();
			VSSFileExtInit();
			UnableButton();
			ScheduleInit();

			//dir Tree Create
			TreeList = [];

			UserDataListUpdate();
			SourceSelectionUpdate();
			ExtensionListUpdate();
			Bindings();

			// ALL 확장자 체크 상태 초기화 추가
			if (Job.WhiteFileExt.Contains("ALL"))
			{
				CB_ExtensionAll.IsChecked = true;
				// 확장자 관련 컨트롤 비활성화
				DisableExtensionControls();
			}

			StatusCheckTimer = new System.Timers.Timer() { Interval = IfsSync2Constants.DEFAULT_STATUS_CHECK_DELAY };
			StatusCheckTimer.Elapsed += new ElapsedEventHandler(StatusCheck);

			UpdateLogTimer = new System.Timers.Timer { Interval = IfsSync2Constants.DEFAULT_STATUS_CHECK_DELAY };
			UpdateLogTimer.Elapsed += new ElapsedEventHandler(LogUpdate);

			if (!NewTab) StateInit();
			UpdateButton();
			ChangeData(false);
		}
		private void VSSFileExtInit()
		{
			string[] result = VSSFILEEXTLIST.Split('|');

			foreach (string item in result) VSSFileExt.Add(item.Trim());
		}
		private void ScheduleInit()
		{
			WeekCheckBoxList.Add(C_Every);
			WeekCheckBoxList.Add(C_Sunday);
			WeekCheckBoxList.Add(C_Monday);
			WeekCheckBoxList.Add(C_Tuesday);
			WeekCheckBoxList.Add(C_Wednesday);
			WeekCheckBoxList.Add(C_Thursday);
			WeekCheckBoxList.Add(C_Friday);
			WeekCheckBoxList.Add(C_Saturday);

			for (int i = 0; i < 24; i++) C_Hours.Items.Add(i.ToString("D02"));
			for (int i = 0; i < 60; i++) C_Mins.Items.Add(i.ToString("D02"));
			SchedulePopupInit();
		}
		private void StateInit()
		{
			_status ??= new JobStatus(Job.HostName, Job.JobName, true);

			StatusCheckTimer.Start();
			UpdateLogTimer.Start();
		}
		private void Bindings()
		{
			T_DirTree.ItemsSource = TreeList;
			L_Schedules.ItemsSource = Job.ScheduleList;
			L_ExtensionList.ItemsSource = FindExtensionList;
			L_SelectedExtensionList.ItemsSource = Job.WhiteFileExt;
			L_DirList.ItemsSource = DirectoryList;
			ChangeData(false);
		}
		private void UnableButton()
		{
			Grid_Instant.Visibility = Visibility.Hidden;
			B_Save.Visibility = Visibility.Hidden;
			Grid_Schedule.Visibility = Visibility.Hidden;

			switch (Job.Policy)
			{
				case JobData.PolicyType.Now:
					Grid_Instant.Visibility = Visibility.Visible;
					L_Monitor.Content = "VSS";
					L_VSS.Content = "Analysis";
					L_Sender.Content = "Upload";
					L_Monitor.ToolTip = "네트워크 드라이브에서는 VSS가 적용되지 않습니다.";
					break;
				case JobData.PolicyType.Schedule:
					Grid_Schedule.Visibility = Visibility.Visible;
					B_Save.Visibility = Visibility.Visible;
					break;
				case JobData.PolicyType.RealTime:
					B_Save.Visibility = Visibility.Visible;
					break;
			}
		}
		public void Close()
		{
			LogViewWindow?.Close();
			if (Job.Policy == JobData.PolicyType.Now) Analysis.Quit();
		}
		public void Delete()
		{
			if (Job.Id > 0) _jobDb.Delete(Job.Id);
			log.Info($"Deleted : {Job.JobName}");

			StatusCheckTimer.Stop();
			UpdateLogTimer.Stop();
		}
		private void ChangeData(bool Changed)
		{
			if (Changed)
			{
				if (IsChanged != Changed)
				{
					IsChanged = Changed;
					ThisTab.Header += "*";
				}
			}
			else
			{
				ThisTab.Header = Job.JobName;
				IsChanged = Changed;
			}

			if (!NewTab)
			{
				B_LogView.IsEnabled = true;
				if (Job.Policy != JobData.PolicyType.Now)
				{
					C_UserList.IsEnabled = false;
					B_StorageRefresh.Visibility = Visibility.Hidden;
					B_Save.IsEnabled = Changed;
					B_Save.Content = "Update Config";
				}

			}
		}


		#region BackupManagement

		public void UserDataListUpdate()
		{
			C_UserList.Items.Clear();

			GlobalUserList = _userDb.GetUsers(true);
			NormalUserList = _userDb.GetUsers(Environment.UserName);

			foreach (UserData User in GlobalUserList) C_UserList.Items.Add(User.StorageName);
			foreach (UserData User in NormalUserList) C_UserList.Items.Add(User.StorageName);

			if (Job.UserId != -1)
			{
				string StorageName = string.Empty;

				if (Job.IsGlobalUser) StorageName = IfsSync2Constants.MAIN_STORAGE_NAME;
				else
					foreach (UserData User in NormalUserList)
						if (User.Id == Job.UserId) StorageName = User.StorageName;

				if (!string.IsNullOrWhiteSpace(StorageName)) C_UserList.SelectedItem = StorageName;
			}
			if (C_UserList.SelectedIndex == -1) C_UserList.SelectedIndex = 0;
		}
		private void Btn_UserListUpdate(object sender, RoutedEventArgs e) { UserDataListUpdate(); }

		#region Schedule Manager
		#region CheckBox
		private bool EveryCheckFlag = true;
		private void WeekCheckBoxChanged(object sender, RoutedEventArgs e)
		{
			CheckBox Box = sender as CheckBox;

			if (!Box.IsChecked.Value)
			{
				if (C_Every.IsChecked.Value)
				{
					EveryCheckFlag = false;
					C_Every.IsChecked = false;
				}
			}
			else
			{
				if (!C_Every.IsChecked.Value)
				{
					bool AllcheckFlag = true;
					for (int i = 1; i < WeekCheckBoxList.Count; i++)
						if (!WeekCheckBoxList[i].IsChecked.Value) AllcheckFlag = false;
					if (AllcheckFlag) C_Every.IsChecked = true;
				}
			}

		}
		private void WeekAllCheck(object sender, RoutedEventArgs e)
		{
			CheckBox Box = sender as CheckBox;

			if (EveryCheckFlag) for (int i = 0; i < WeekCheckBoxList.Count; i++) WeekCheckBoxList[i].IsChecked = Box.IsChecked;
			EveryCheckFlag = true;
		}
		#endregion CheckBox
		private void SchedulePopupInit()
		{
			B_ScheduleToggle.IsChecked = false;
			C_Hours.SelectedIndex = 0;
			C_Mins.SelectedIndex = 0;
			C_Every.IsChecked = true;

			T_ForHours.Text = "0";
		}
		private void Btn_DelSchedule(object sender, RoutedEventArgs e)
		{
			if (L_Schedules.SelectedIndex < 0) return;

			while (true)
			{
				int index = L_Schedules.SelectedIndex;
				if (index < 0) break;
				else Job.ScheduleList.RemoveAt(index);
			}
			ChangeData(true);
		}

		private void Btn_CreateSchedule(object sender, RoutedEventArgs e)
		{
			const string Title = "Schedule";

			Schedule Schedules = new();
			//Get Day of the Week
			if (C_Every.IsChecked.Value) Schedules.AddDay(WeekDays.Every);
			else
			{
				if (C_Sunday.IsChecked.Value) Schedules.AddDay(WeekDays.Sunday);
				if (C_Monday.IsChecked.Value) Schedules.AddDay(WeekDays.Monday);
				if (C_Tuesday.IsChecked.Value) Schedules.AddDay(WeekDays.Tuesday);
				if (C_Wednesday.IsChecked.Value) Schedules.AddDay(WeekDays.Wednesday);
				if (C_Thursday.IsChecked.Value) Schedules.AddDay(WeekDays.Thursday);
				if (C_Friday.IsChecked.Value) Schedules.AddDay(WeekDays.Friday);
				if (C_Saturday.IsChecked.Value) Schedules.AddDay(WeekDays.Saturday);
			}
			Schedules.SetAtTime(C_Hours.SelectedIndex, C_Mins.SelectedIndex);

			//Get for Hours
			if (string.IsNullOrWhiteSpace(T_ForHours.Text))
			{
				Utility.ErrorMessageBox("오류 : 실행 시간이 비어 있습니다", Title);
				return;
			}
			if (!int.TryParse(T_ForHours.Text, out int ForHours))
			{
				Utility.ErrorMessageBox("오류 : 실행 시간이 유효한 값이 아닙니다", Title);
				return;
			}
			Schedules.ForHours = ForHours;

			if (string.IsNullOrWhiteSpace(Schedules.StrWeek)) Utility.ErrorMessageBox("오류 : 주간 일정이 선택되지 않았습니다!", Title);
			else
			{
				Job.ScheduleList.Add(Schedules);
				SchedulePopupInit();
				ChangeData(true);
			}
		}
		private void Btn_ScheduleToggleClose(object sender, RoutedEventArgs e)
		{
			SchedulePopupInit();
		}
		#endregion Schedule Manager

		private void Btn_Save(object sender, RoutedEventArgs e)
		{
			//empty Check
			if (Job.Path.Count == 0)
			{
				Utility.ErrorMessageBox("오류 : 디렉토리 목록이 비어 있습니다", "저장");
				return;
			}
			if (Job.WhiteFileExt.Count == 0)
			{
				Utility.ErrorMessageBox("오류 : 확장자 목록이 비어 있습니다", "저장");
				return;
			}
			if (Job.Policy == JobData.PolicyType.Schedule && Job.ScheduleList.Count == 0)
			{
				Utility.ErrorMessageBox("오류 : 일정 목록이 비어 있습니다", "저장");
				return;
			}

			if (!_jobDb.PutJobData(Job))
			{
				Utility.ErrorMessageBox("오류 : 작업 저장에 실패했습니다", "저장");
				return;
			}
			if (Job.Id < 0) Job.Id = _jobDb.GetJobDataId(Job.HostName, Job.JobName);
			if (NewTab)
			{
				Job.StrBlackPath = IfsSync2Constants.DEFAULT_BLACK_PATH_LIST;
				StateInit();
				NewTab = false;
			}

			ChangeData(false);
		}

		#endregion BackupManagement

		#region Source Selection
		private void SourceSelectionUpdate()
		{
			LoadDrive();
			DirectoryList.Clear();
			foreach (string item in Job.Path)
			{
				AddCheckFromTree(item);
				ListViewAdd(item);
			}
		}
		private void Btn_SourceUpdate(object sender, RoutedEventArgs e)
		{
			SourceSelectionUpdate();
		}

		public void LoadDrive()
		{
			TreeNode root = new()
			{
				Name = Root,
				FullPath = Root,
				FileIcon = image_ComputerIcon.Source
			};

			//driver Add
			DriveInfo[] allDrives = DriveInfo.GetDrives();
			foreach (var drive in allDrives)
			{
				if (drive.IsReady)
				{
					TreeNode item = new()
					{
						Name = drive.VolumeLabel + " (" + drive.Name.TrimEnd('\\') + ")",
						FullPath = drive.Name,
						FileIcon = Utility.GetIconImageSource(drive.Name),
					};
					GetPCSubDirectories(item);
					root.Children.Add(item);
				}
			}

			root.Initialize();
			log.Info("Load root directory");

			string userName = "Default";
			string myUserPath = string.Empty;
			try
			{
				myUserPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				userName = Environment.UserName;
			}
			catch (Exception ex)
			{
				log.Error($"LoadDrive : {ex.Message}");
			}

			var myUserItem = new TreeNode()
			{
				Name = userName,
				FullPath = myUserPath,
				FileIcon = Utility.GetIconImageSource(myUserPath),
			};
			GetPCSubDirectories(myUserItem);
			log.Info("Load my user directory");

			TreeList.Clear();
			TreeList.Add(myUserItem);
			TreeList.Add(root);
		}

		private static void GetPCSubDirectories(TreeNode node)
		{
			if (node == null) return;
			if (node.Children.Count > 0) return;

			node.Children.Clear();
			try
			{
				DirectoryInfo dirInfo = new(node.FullPath);

				foreach (var subDir in dirInfo.GetDirectories())
				{
					if (subDir.Name == "Documents and Settings" ||
						subDir.Name == "System Volume Information" ||
						subDir.FullName.Contains('$'))
						continue;

					var defaultIcon = Utility.GetIcon(subDir.FullName, IconSize.Small, ItemState.Close);
					var defaultIconSource = Utility.IconToImageSource(defaultIcon);

					var item = new TreeNode()
					{
						Name = subDir.Name,
						FullPath = subDir.FullName,
						FileIcon = Utility.GetIconImageSource(subDir.FullName) ?? defaultIconSource
					};
					node.Children.Add(item);
				}
				node.Initialize();
			}
			catch (Exception e)
			{
				log.Debug(e);
			}
		}

		private void Tree_Expanded(object sender, RoutedEventArgs e)
		{
			TreeViewItem tvi = e.OriginalSource as TreeViewItem;
			TreeNode node = (TreeNode)tvi.Header;

			if (node == null) return;
			if (node.Children.Count == 0) return;

			foreach (TreeNode item in node.Children) GetPCSubDirectories(item);
		}

		private void AddCheckFromTree(string DirPath)
		{
			foreach (TreeNode Node in TreeList)
			{
				if (Node.FullPath == DirPath)
				{
					Node.IsChecked = true;
					return;
				}

				if (Node.Children.Count > 0)
				{
					foreach (TreeNode item in Node.Children) GetPCSubDirectories(item);
					if (CheckCheckBox(Node, DirPath)) return;
				}
			}
			log.Info(DirPath);
		}
		private static bool CheckCheckBox(TreeNode Node, string DirPath)
		{
			foreach (var childNode in Node.Children)
			{
				if (!DirPath.StartsWith(childNode.FullPath)) continue;

				foreach (TreeNode item in Node.Children) GetPCSubDirectories(item);

				if (childNode.FullPath == DirPath)
				{
					childNode.IsChecked = true;
					return true;
				}

				if (childNode.Children.Count > 0 && CheckCheckBox(childNode, DirPath))
					return true;
			}
			return false;
		}

		private void DeleteSelectedPathFromButton(object sender, RoutedEventArgs e)
		{
			Button button = (Button)sender;
			DeletePath(button.Tag.ToString());
			RemoveCheckFromTree(button.Tag.ToString());
			UpdateButton();
		}
		private void RemoveCheckFromTree(string DirPath)
		{
			foreach (var node in TreeList)
			{
				if (node.FullPath == DirPath && node.IsChecked == true)
				{
					node.IsChecked = false;
					return;
				}
				if (node.Children.Count > 0 && UncheckCheckBox(node, DirPath))
					return;
			}
			log.Info(DirPath);
		}
		private static bool UncheckCheckBox(TreeNode Node, string DirPath)
		{
			bool uncheck = false;

			foreach (var childNode in Node.Children)
			{
				if (childNode.FullPath == DirPath && childNode.IsChecked == true)
				{
					childNode.IsChecked = false;
					return true;
				}
				if (childNode.Children.Count > 0 && UncheckCheckBox(childNode, DirPath))
					return true;
			}
			return uncheck;
		}

		private void CheckBox_Changed(object sender, RoutedEventArgs e)
		{
			CheckBox ThisCheckBox = sender as CheckBox;
			string DirPath = ThisCheckBox.Tag.ToString();

			if (ThisCheckBox.IsChecked.HasValue)
			{
				if (ThisCheckBox.IsChecked.Value)
				{
					if (IsExistAllSubPath(DirPath)) DelSubPath(DirPath);
					AddPath(DirPath);
				}
				else
				{
					DeletePath(DirPath);
				}
			}
			else
			{
				RelocationNULLNode(DirPath);
			}
			UpdateButton();
		}

		private void AddPath(string DirPath)
		{
			try
			{
				if (DirPath == Root) return;
				if (!Job.ExistsDirectory(DirPath))
				{
					ListViewAdd(DirPath);
					Job.Path.Add(DirPath);
				}
			}
			catch (Exception e)
			{
				log.Error(e);
			}

			ChangeData(true);
		}

		private bool DeletePath(string DirPath)
		{
			try
			{
				if (Job.DeleteDirectory(DirPath))
				{
					//DirectoryList
					for (int index = DirectoryList.Count - 1; index >= 0; index--)
					{
						if (DirectoryList[index].DirectoryPath.Equals(DirPath))
						{
							DirectoryList.RemoveAt(index);
						}
					}
					ChangeData(true);
					return true;
				}
			}
			catch (Exception e)
			{
				log.Error(e);
			}
			return false;
		}
		private void DelSubPath(string MainPath)
		{
			if (Job.Path.Count > 0)
			{
				try
				{
					List<string> DeletePathList = [];

					foreach (var SubDirectory in Job.Path) if (SubDirectory.StartsWith(MainPath)) DeletePathList.Add(SubDirectory);
					foreach (string SubPath in DeletePathList) DeletePath(SubPath);
				}
				catch (Exception e)
				{
					log.Error(e);
				}
			}
		}

		private bool IsExistAllSubPath(string MainPath)
		{
			foreach (var Node in TreeList)
			{
				if (Node.Children.Count > 0) 
					if (IsCheckedSubNode(Node, MainPath)) return true;
					else return true;
			}
			return false;
		}
		private static bool IsCheckedSubNode(TreeNode Node, string DirPath)
		{
			foreach (var SubNode in Node.Children)
			{
				if (SubNode.Name.Equals(DirPath) && SubNode.IsAllChildrenChecked()) return true;
			}
			return false;
		}

		private void RelocationNULLNode(string DirPath)
		{
			//Directory 목록에 해당 디렉토리가 있으면 삭제하고
			if (DeletePath(DirPath))
			{
				//삭제한 그 하위노드중에 IsChecked == true라면 Directory목록에 추가하기
				foreach (TreeNode Node in TreeList)
				{
					if (Node.FullPath == DirPath)
					{
						CheckTrueSubNode(Node);
						return;
					}

					if (Node.Children.Count > 0 && FindNodeToPath(Node, DirPath))
						return;
				}
			}
		}

		private bool FindNodeToPath(TreeNode Node, string TargetPath)
		{
			foreach (var childNode in Node.Children)
			{
				if (!TargetPath.StartsWith(childNode.FullPath)) continue;

				if (childNode.FullPath == TargetPath)
				{
					CheckTrueSubNode(childNode);
					return true;
				}

				if (childNode.Children.Count > 0 && FindNodeToPath(childNode, TargetPath))
					return true;
			}
			return false;
		}

		private void CheckTrueSubNode(TreeNode Node)
		{
			foreach (TreeNode Children in Node.Children)
			{
				if (Children.IsChecked == true) AddPath(Children.FullPath);
			}
		}

		private void ListViewAdd(string DirPath)
		{
			DirectoryList.Add(new DirectoryData
			{
				DirectoryIcon = Utility.GetIconImageSource(DirPath),
				DirectoryPath = DirPath,
				AnalysisFiles = string.Empty,
				AnalysisSize = string.Empty,
				IsCounting = false
			});
			AutoSizeColumns();
			log.Info(DirPath);
		}
		public void AutoSizeColumns()
		{
			if (L_DirList.View is GridView gv)
			{
				foreach (var c in gv.Columns)
				{
					// Code below was found in GridViewColumnHeader.OnGripperDoubleClicked() event handler (using Reflector)
					// i.e. it is the same code that is executed when the gripper is double clicked
					if (double.IsNaN(c.Width))
					{
						c.Width = c.ActualWidth;
					}
					c.Width = double.NaN;
				}
			}
		}
		#endregion Source Selection

		#region Extension Selection

		private void ExtensionListUpdate()
		{
			List<string> ExtList = _extDb.GetExtensionList();

			if (ExtList == null || ExtList.Count == 0)
			{
				Utility.ErrorMessageBox("확장자 목록이 비어 있습니다", "확장자 목록 업데이트");
				return;
			}

			ExtensionList.Clear();
			ExtensionList.AddRange(ExtList);

			FindExtensionListUpdate();

			L_ExtensionList.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("", System.ComponentModel.ListSortDirection.Ascending));
			L_SelectedExtensionList.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("", System.ComponentModel.ListSortDirection.Ascending));
		}
		private void FindExtensionListUpdate()
		{
			FindExtensionList.Clear();
			if (string.IsNullOrWhiteSpace(T_ExtensionName.Text))
			{
				foreach (string Ext in ExtensionList)
					if (!Job.WhiteFileExt.Contains(Ext)) FindExtensionList.Add(Ext);
			}
			else
			{
				string FindExt = T_ExtensionName.Text;

				foreach (string Ext in ExtensionList)
				{
					if (!Job.WhiteFileExt.Contains(Ext) && Ext.Contains(FindExt))
						FindExtensionList.Add(Ext);
				}

				if (FindExtensionList.Count == 0) B_ExtAdd.IsEnabled = true;
			}
		}

		private void Btn_ExtensionUpdate(object sender, RoutedEventArgs e)
		{
			ExtensionListUpdate();
		}

		private void Btn_AddExtension(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(T_ExtensionName.Text))
			{
				Utility.ErrorMessageBox("확장자가 비어 있습니다", "확장자 추가");
				return;
			}

			try
			{
				string Extension = T_ExtensionName.Text.ToLower();

				if (Utility.SpecialCharactersErrorCheck(Extension))
				{
					Utility.ErrorMessageBox("확장자명에 다음 단어를 사용할 수 없습니다.\n[\\, /, :, *, ?, \", <, >, |]", "Extension");
					return;
				}
				if (_extDb.Check(Extension))
				{
					Utility.ErrorMessageBox("해당 확장자명이 목록에 이미 존재합니다!", "Extension");
					return;
				}

				if (_extDb.Insert(Extension))
				{
					T_ExtensionName.Text = string.Empty;
					ExtensionListUpdate();
				}
			}
			catch (Exception ex)
			{
				log.Error(ex);
			}
		}
		private void Btn_DelExtension(object sender, RoutedEventArgs e)
		{
			if (L_SelectedExtensionList.SelectedIndex < 0) return;

			try
			{
				while (true)
				{
					int index = L_SelectedExtensionList.SelectedIndex;
					if (index < 0) break;
					Job.WhiteFileExt.RemoveAt(index);
				}
			}
			catch (Exception ex)
			{
				log.Error(ex);
			}
			ChangeData(true);
			UpdateButton();
		}

		private void ExtensionNameTextChanged(object sender, TextChangedEventArgs e)
		{
			ExtensionListUpdate();
		}

		private void Btn_ExtensionAdd(object sender, RoutedEventArgs e)
		{
			if (L_ExtensionList.SelectedIndex < 0) return;

			while (L_ExtensionList.SelectedIndex > -1)
			{
				string Ext = L_ExtensionList.SelectedItem.ToString();
				FindExtensionList.Remove(Ext);
				Job.WhiteFileExt.Add(Ext);
				if (VSSFileExt.Contains(Ext)) Job.VSSFileExt.Add(Ext);
			}
			ExtensionListUpdate();
			ChangeData(true);
			UpdateButton();
		}
		private void Btn_ExtensionAllAdd(object sender, RoutedEventArgs e)
		{
			Job.WhiteFileExt.Clear();
			foreach (string Ext in ExtensionList)
			{
				Job.WhiteFileExt.Add(Ext);
				if (VSSFileExt.Contains(Ext)) Job.VSSFileExt.Add(Ext);
			}
			ExtensionListUpdate();
			ChangeData(true);
			UpdateButton();
		}
		private void Btn_ExtensionDelete(object sender, RoutedEventArgs e)
		{
			if (L_SelectedExtensionList.SelectedIndex < 0) return;

			while (L_SelectedExtensionList.SelectedIndex > -1)
			{
				string Ext = L_SelectedExtensionList.SelectedItem.ToString();
				Job.WhiteFileExt.Remove(Ext);
				Job.VSSFileExt.Remove(Ext);
			}
			ExtensionListUpdate();
			ChangeData(true);
			UpdateButton();
		}
		private void Btn_ExtensionAllDelete(object sender, RoutedEventArgs e)
		{
			Job.WhiteFileExt.Clear();
			Job.VSSFileExt.Clear();
			ExtensionListUpdate();
			ChangeData(true);
			UpdateButton();
		}

		private void ExtensionListKeyDownEvent(object sender, KeyEventArgs e)
		{

			if (e.Key == Key.Delete)
			{
				while (L_ExtensionList.SelectedIndex >= 0)
				{
					string del = L_ExtensionList.SelectedItem.ToString();
					_extDb.Delete(del);
					FindExtensionList.Remove(del);
				}
			}
			ExtensionListUpdate();
		}
		#endregion Extension Selection


		#region Instant Backup

		#region Analysis

		private void AnalysisRun()
		{
			log.Info("Start");
			_taskDb.InsertLog("Analysis Start!");
			Analysis.Start();
			Stopwatch sw = new();
			sw.Start();
			_status.Filter = true;
			_status.Quit = false;

			//Analysis Init
			ButtonLock(false);

			//Analysis
			List<string> extensionList = [.. Job.WhiteFileExt];
			List<string> directoryList = [.. Job.Path];

			//Directory Search
			foreach (var myDir in directoryList)
			{
				if (Analysis.IsQuit) break;
				try
				{
					SubDirectory(myDir, extensionList);
				}
				catch (Exception e)
				{
					log.Error(e);
				}
			}

			ButtonLock(true);
			sw.Stop();
			_status.Filter = false;
			_status.Quit = true;

			if (Analysis.IsQuit) return;
			Analysis.Quit();

			log.Info($"End. Time : {sw.ElapsedMilliseconds}ms");
			_taskDb.InsertLog($"Analysis End. Time : {sw.ElapsedMilliseconds}ms");
			_taskDb.InsertLog($"Upload File Count : {Analysis.UploadFileCount}\tUpload File Size : {CapacityUnit.Format(Analysis.UploadFileSize)}");
		}

		private void SubDirectory(string ParentDirectory, List<string> ExtensionList)
		{
			DirectoryInfo dInfoParent = new(ParentDirectory);

			if (!dInfoParent.Exists) return;
			//add Folder List
			foreach (DirectoryInfo dInfo in dInfoParent.GetDirectories())
			{
				if (Analysis.IsQuit) break;
				try { SubDirectory(dInfo.FullName, ExtensionList); } catch (Exception ex) { log.Error(ex); }
			}
			AddBackupFile(ParentDirectory, ExtensionList);
		}
		private void AddBackupFile(string ParentDirectory, List<string> ExtensionList)
		{
			//add File List
			string[] files = Directory.GetFiles(ParentDirectory);

			// ALL 확장자 처리를 먼저 체크
			bool isAllExtension = ExtensionList.Contains("ALL");

			// 파일 나열
			foreach (string file in files)
			{
				if (Analysis.IsQuit) break;
				FileInfo info = new(file);

				if (!info.Exists) continue;

				if ((info.Attributes & FileAttributes.System) == FileAttributes.System) { /*empty*/ }
				else if ((isAllExtension || ExtensionList.Contains(info.Extension.Replace(".", "").ToLower()))
						 && info.FullName.IndexOf('$') < 0)
				{
					Analysis.UploadFileCount++;
					Analysis.UploadFileSize += info.Length;
				}
				Analysis.CheckCount++;
			}
		}
		#endregion Analysis

		private bool PathCheck(out string RootPath)
		{
			RootPath = string.Empty;

			foreach (string root in Job.Path)
			{
				DirectoryInfo dir = new(root);
				if (!dir.Exists)
				{
					RootPath = root;
					return true;
				}
			}
			return false;
		}
		private void ButtonLock(bool Lock)
		{
			B_BackupStart.Dispatcher.Invoke(delegate
			{
				C_UserList.IsEnabled = Lock;
				if (Lock) B_BackupStart.Visibility = Visibility.Visible;
				else B_BackupStart.Visibility = Visibility.Hidden;
				T_DirTree.IsEnabled = Lock;
				L_DirList.IsEnabled = Lock;
				B_ExtensionAllDelete.IsEnabled = Lock;
				B_ExtensionDelete.IsEnabled = Lock;
				B_ExtensionAdd.IsEnabled = Lock;
				B_ExtensionAllAdd.IsEnabled = Lock;
			});
			B_Analysis.Dispatcher.Invoke(delegate
			{
				if (Lock) B_Analysis.Visibility = Visibility.Visible;
				else B_Analysis.Visibility = Visibility.Hidden;
			});
			B_InstantQuit.Dispatcher.Invoke(delegate
			{
				if (Lock) B_InstantQuit.Visibility = Visibility.Hidden;
				else B_InstantQuit.Visibility = Visibility.Visible;
			});
		}
		private void UpdateButton()
		{
			if (Job.Path.Count == 0 || Job.WhiteFileExt.Count == 0)
			{
				B_BackupStart.IsEnabled = false;
				B_Analysis.IsEnabled = false;
			}
			else
			{
				B_BackupStart.IsEnabled = true;
				B_Analysis.IsEnabled = true;
			}
		}

		private void Btn_BackupStart(object sender, RoutedEventArgs e)
		{
			if (Analysis.IsRunning) { Utility.ErrorMessageBox("분석이 실행 중입니다!", CLASS_NAME); return; }
			if (Job.WhiteFileExt.Count == 0) { Utility.ErrorMessageBox("확장자 목록이 비어 있습니다!", CLASS_NAME); return; }
			if (Job.Path.Count == 0) { Utility.ErrorMessageBox("디렉토리 목록이 비어 있습니다!", CLASS_NAME); return; }
			if (PathCheck(out string ErrorPath)) { Utility.ErrorMessageBox(string.Format("[{0}] 경로가 존재하지 않습니다!", ErrorPath), CLASS_NAME); return; }
			if (C_UserList.SelectedIndex < GlobalUserList.Count)
			{
				Job.UserId = GlobalUserList[C_UserList.SelectedIndex].Id;
				Job.IsGlobalUser = true;
			}
			else
			{
				Job.UserId = NormalUserList[C_UserList.SelectedIndex - GlobalUserList.Count].Id;
				Job.IsGlobalUser = false;
			}
			_status.Quit = false;
			InstantData instantData = new();
			instantData.Init();

			if (!_jobDb.PutJobData(Job))
			{
				Utility.ErrorMessageBox("오류 : 작업 저장에 실패했습니다", "저장");
				return;
			}

			ChangeData(false);
			ButtonLock(false);
			if (Job.Id < 0) Job.Id = _jobDb.GetJobDataId(Job.HostName, Job.JobName);
		}

		private void Btn_Analysis(object sender, RoutedEventArgs e)
		{
			if (Analysis.IsRunning) { Utility.ErrorMessageBox("분석이 실행 중입니다!", CLASS_NAME); return; }
			if (Job.WhiteFileExt.Count == 0) { Utility.ErrorMessageBox("확장자 목록이 비어 있습니다!", CLASS_NAME); return; }
			if (Job.Path.Count == 0) { Utility.ErrorMessageBox("디렉토리 목록이 비어 있습니다!", CLASS_NAME); return; }
			if (PathCheck(out string ErrorPath)) { Utility.ErrorMessageBox(string.Format("[{0}] 경로가 존재하지 않습니다!", ErrorPath), CLASS_NAME); return; }

			if (AnalysisThread != null) AnalysisThread = null;
			AnalysisThread = new Thread(() => AnalysisRun());

			if (AnalysisThread != null) log.Info($"Analysis Thread : {Job.JobName}");
			else
			{
				log.FatalFormat("Create thread fail");
				((MainWindow)Application.Current.MainWindow).Close();
			}

			AnalysisThread.Start();
		}

		private void UserSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (C_UserList.SelectedIndex < 0) return;

			if (C_UserList.SelectedIndex < GlobalUserList.Count)
			{
				Job.UserId = GlobalUserList[C_UserList.SelectedIndex].Id;
				Job.IsGlobalUser = true;
			}
			else
			{
				Job.UserId = NormalUserList[C_UserList.SelectedIndex - GlobalUserList.Count].Id;
				Job.IsGlobalUser = false;
			}
		}

		private void Btn_InstantQuit(object sender, RoutedEventArgs e)
		{
			if (Analysis.IsRunning) _taskDb.InsertLog("Analysis Stop!");

			_status.Quit = true;
			_jobDb.UpdateFilterCheck(Job);
			Analysis.Quit();

		}
		#endregion Instant Backup

		#region Status

		private void StatusCheck(object sender, ElapsedEventArgs e)
		{
			if (Job.Policy == JobData.PolicyType.Now)
			{
				if (Analysis.IsRunning) ButtonLock(false);
				else if (!_status.Quit) ButtonLock(false);
				else
				{
					if (_status.Filter || _status.VSS || _status.Sender) { ButtonLock(false); BackupStart = true; }
					else
					{
						if (BackupStart) { BackupStart = false; }
						ButtonLock(true);
					}
				}
			}

			if (_status.Filter) Image_Filter.Dispatcher.Invoke(delegate
			{
				Image_Filter.Source = Image_CircleBlue.Source;
				State_Filter.Content = "구동중";
				if (Job.Policy == JobData.PolicyType.Now)
				{
					Image_VSS.Source = Image_CircleBlue.Source;
					State_VSS.Content = "분석중";
				}
			});
			else Image_Filter.Dispatcher.Invoke(delegate
			{
				if (Job.Policy == JobData.PolicyType.Now)
				{
					if (BackupStart)
					{
						Image_VSS.Source = Image_CircleGray.Source;
						State_VSS.Content = "완료";
					}
					else
					{
						Image_VSS.Source = Image_CircleGray.Source;
						State_VSS.Content = "정지";
					}
				}
				else
				{
					Image_Filter.Source = Image_CircleGray.Source;
					State_Filter.Content = "정지";
				}

			});

			if (_status.VSS) Image_VSS.Dispatcher.Invoke(delegate
			{
				if (Job.Policy == JobData.PolicyType.Now)
				{
					Image_Filter.Source = Image_CircleBlue.Source;
					State_Filter.Content = "활성화";
				}
				else
				{
					Image_VSS.Source = Image_CircleBlue.Source;
					State_VSS.Content = "활성화";
				}
			});
			else Image_VSS.Dispatcher.Invoke(delegate
			{
				if (Job.Policy == JobData.PolicyType.Now)
				{
					Image_Filter.Source = Image_CircleGray.Source;
					State_Filter.Content = "비활성화";
				}
				else
				{
					Image_VSS.Source = Image_CircleGray.Source;
					State_VSS.Content = "비활성화";
				}
			});

			if (_status.Sender) Image_Sender.Dispatcher.Invoke(delegate { Image_Sender.Source = Image_TriangleGreen.Source; State_Sender.Content = "업로드중"; });
			else Image_Sender.Dispatcher.Invoke(delegate { Image_Sender.Source = Image_SquareGray.Source; State_Sender.Content = "정지"; });

			if (_status.Error) Image_Sender.Dispatcher.Invoke(delegate { Image_Sender.Source = Image_TriangleRed.Source; State_Sender.Content = "접속에러"; });

		}

		#endregion State

		#region Log View
		private void Btn_LogView(object sender, RoutedEventArgs e)
		{
			if (LogViewWindow == null)
			{
				LogViewWindow = new LogViewWindow(Job);
				LogViewWindow.Show();
			}
			else
			{
				if (LogViewWindow.IsClose)
				{
					LogViewWindow = new LogViewWindow(Job);
					LogViewWindow.Show();
				}
				else
				{
					LogViewWindow.WindowState = WindowState.Normal;
					LogViewWindow.Activate();
				}
			}
		}

		private void LogUpdate(object sender, ElapsedEventArgs e)
		{
			// L_LogList에서 가장 마지막 인덱스를 찾아서 그 이후의 로그를 가져온다.
			var lastIndex = int.MaxValue;
			if (L_LogList.Items.Count > 0)
				L_LogList.Dispatcher.Invoke(() =>
				{
					lastIndex = L_LogList.Items.Count;
				});

			var LogList = _taskDb.GetLog(lastIndex);

			if (LogList?.Count > 0)
			{
				L_LogList.Dispatcher.Invoke(delegate
				{
					foreach (string Msg in LogList) L_LogList.Items.Add(Msg);
					L_LogList.SelectedIndex = L_LogList.Items.Count - 1;
				});
			}
		}
		private void Btn_LogClear(object sender, RoutedEventArgs e)
		{
			L_LogList.Items.Clear();
		}

		#endregion Log View

		private void ExtensionAll_Checked(object sender, RoutedEventArgs e)
		{
			Job.WhiteFileExt.Clear();
			Job.VSSFileExt.Clear();
			Job.WhiteFileExt.Add("ALL");
			ExtensionListUpdate();
			ChangeData(true);
			UpdateButton();

			DisableExtensionControls();
		}

		private void ExtensionAll_Unchecked(object sender, RoutedEventArgs e)
		{
			// ALL 확장자 제거
			Job.WhiteFileExt.Remove("ALL");

			// 확장자 관련 컨트롤 활성화
			EnableExtensionControls();

			ExtensionListUpdate();
			UpdateButton();
		}

		// 확장자 컨트롤 비활성화를 위한 헬퍼 메서드
		private void DisableExtensionControls()
		{
			T_ExtensionName.IsEnabled = false;
			B_ExtAdd.IsEnabled = false;
			B_ExtDel.IsEnabled = false;
			L_ExtensionList.IsEnabled = false;
			L_SelectedExtensionList.IsEnabled = false;
			B_ExtensionAllDelete.IsEnabled = false;
			B_ExtensionDelete.IsEnabled = false;
			B_ExtensionAdd.IsEnabled = false;
			B_ExtensionAllAdd.IsEnabled = false;
			B_ExtensionRefresh.IsEnabled = false;
		}

		// 확장자 컨트롤 활성화를 위한 헬퍼 메서드 추가
		private void EnableExtensionControls()
		{
			T_ExtensionName.IsEnabled = true;
			B_ExtAdd.IsEnabled = true;
			B_ExtDel.IsEnabled = true;
			L_ExtensionList.IsEnabled = true;
			L_SelectedExtensionList.IsEnabled = true;
			B_ExtensionAllDelete.IsEnabled = true;
			B_ExtensionDelete.IsEnabled = true;
			B_ExtensionAdd.IsEnabled = true;
			B_ExtensionAllAdd.IsEnabled = true;
			B_ExtensionRefresh.IsEnabled = true;
		}

	}
}