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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using IfsSync2Data;
using System.Timers;

namespace IfsSync2UI
{
	public partial class JobTab : UserControl
	{
		private readonly static string CLASS_NAME = "JobTab";
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

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
		private readonly string RootPath;
		public readonly JobData Job;

		/*************************** Job State *************************************/
		private JobState State = null;
		private readonly System.Timers.Timer StateCheckTimer;
		/************************ Schedule Data *************************************/
		private readonly List<CheckBox> WeekCheckBoxList = new List<CheckBox>();

		/*************************** Db Data ***************************************/
		private readonly UserDbManager UserDb;
		private readonly JobDbManager JobDb;
		private readonly ExtensionDbManager ExtDb;
		private readonly TaskDbManager TaskDb;

		/************************ Extension List ************************************/
		private readonly List<string> ExtensionList = new List<string>();
		private readonly ObservableCollection<string> FindExtensionList = new ObservableCollection<string>();

		/************************* Analysis Data ************************************/
		private readonly ProgressData Analysis = new ProgressData();
		private Thread AnalysisThread = null;
		public bool BackupStart = false;
		/*************************** VSS Data ************************************/
		private readonly List<string> VSSFileExt = new List<string>();
		private readonly string VSSFILEEXTLIST = "pst|ost|db";

		/*************************** Log Data ************************************/
		LogViewWindow LogViewWindow = null;
		private readonly System.Timers.Timer UpdateLogTimer;
		private long StartPoint = 0;
		/****************************** Init *************************************/
		public JobTab(TabItem Tab, JobData job, string _RootPath, bool _NewTab = false)
		{
			NewTab = _NewTab;
			ThisTab = Tab;
			Job = job;
			RootPath = _RootPath;
			DirectoryList = new ObservableCollection<DirectoryData>();
			UserDb = new UserDbManager();
			JobDb = new JobDbManager();
			ExtDb = new ExtensionDbManager();
			TaskDb = new TaskDbManager(job.JobName);

			InitializeComponent();
			VSSFileExtInit();
			UnableButton();
			ScheduleInit();

			//dir Tree Create
			TreeList = new ObservableCollection<TreeNode>();

			UserDataListUpdate();
			SourceSelectionUpdate();
			ExtensionListUpdate();
			Bindings();

			StateCheckTimer = new System.Timers.Timer() { Interval = MainData.DEFAULT_STATUS_CHECK_DELAY };
			StateCheckTimer.Elapsed += new ElapsedEventHandler(StateCheck);


			UpdateLogTimer = new System.Timers.Timer { Interval = MainData.DEFAULT_STATUS_CHECK_DELAY };
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
			if (State == null) State = new JobState(Job.HostName, Job.JobName, true);

			StateCheckTimer.Start();
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
				case JobPolicyType.Now:
					Grid_Instant.Visibility = Visibility.Visible;
					L_Monitor.Content = "VSS";
					L_VSS.Content = "Analysis";
					L_Sender.Content = "Upload";
					L_Monitor.ToolTip = "네트워크 드라이브에서는 VSS가 적용되지 않습니다.";
					break;
				case JobPolicyType.Schedule:
					Grid_Schedule.Visibility = Visibility.Visible;
					B_Save.Visibility = Visibility.Visible;
					break;
				case JobPolicyType.RealTime:
					B_Save.Visibility = Visibility.Visible;
					break;
			}
		}

		public void Close()
		{
			LogViewWindow?.Close();
			if (Job.Policy == JobPolicyType.Now) Analysis.Stop();
		}
		public void Delete()
		{
			if (Job.Id > 0) JobDb.Delete(Job.Id);
			log.Info($"Deleted : {Job.JobName}");
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
				if (Job.Policy != JobPolicyType.Now)
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

			GlobalUserList = UserDb.GetUsers(true);
			NormalUserList = UserDb.GetUsers(Environment.UserName);

			foreach (UserData User in GlobalUserList) C_UserList.Items.Add(User.StorageName);
			foreach (UserData User in NormalUserList) C_UserList.Items.Add(User.StorageName);

			if (Job.UserID != -1)
			{
				string StorageName = string.Empty;

				if (Job.IsGlobalUser) StorageName = MainData.MAIN_STORAGE_NAME;
				else
					foreach (UserData User in NormalUserList)
						if (User.Id == Job.UserID) StorageName = User.StorageName;

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

			Schedule Schedules = new Schedule();
			//Get Day of the Week
			if (C_Every.IsChecked.Value) Schedules.AddWeek(Schedule.EVERY);
			else
			{
				if (C_Sunday.IsChecked.Value) Schedules.AddWeek(Schedule.SUNDAY);
				if (C_Monday.IsChecked.Value) Schedules.AddWeek(Schedule.MONDAY);
				if (C_Tuesday.IsChecked.Value) Schedules.AddWeek(Schedule.TUESDAY);
				if (C_Wednesday.IsChecked.Value) Schedules.AddWeek(Schedule.WEDNESDAY);
				if (C_Thursday.IsChecked.Value) Schedules.AddWeek(Schedule.THURSDAY);
				if (C_Friday.IsChecked.Value) Schedules.AddWeek(Schedule.FRIDAY);
				if (C_Saturday.IsChecked.Value) Schedules.AddWeek(Schedule.SATURDAY);
			}
			Schedules.SetAtTime(C_Hours.SelectedIndex, C_Mins.SelectedIndex);

			//Get for Hours
			if (string.IsNullOrWhiteSpace(T_ForHours.Text))
			{
				Utility.ErrorMessageBox("Error : For Hours is Empty", Title);
				return;
			}
			if (!int.TryParse(T_ForHours.Text, out int ForHours))
			{
				Utility.ErrorMessageBox("Error : For Hours is not Value", Title);
				return;
			}
			Schedules.ForHours = ForHours;

			if (string.IsNullOrWhiteSpace(Schedules.StrWeek)) Utility.ErrorMessageBox("Error : Week not Selected!", Title);
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
				Utility.ErrorMessageBox("Error : Directory List is Empty", "Save");
				return;
			}
			if (Job.WhiteFileExt.Count == 0)
			{
				Utility.ErrorMessageBox("Error : Extension List is Empty", "Save");
				return;
			}
			if (Job.Policy == JobPolicyType.Schedule)
			{
				if (Job.ScheduleList.Count == 0)
				{
					Utility.ErrorMessageBox("Error : Schedule List is Empty", "Save");
					return;
				}
			}

			if (!JobDb.PutJobData(Job))
			{
				Utility.ErrorMessageBox("Error : Failed to save job", "Save");
				return;
			}
			if (Job.Id < 0) Job.Id = JobDb.GetJobDataId(Job.HostName, Job.JobName);
			if (NewTab)
			{
				Job.StrBlackPath = MainData.DEFAULT_BLACK_PATH_LIST;
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
			TreeNode root = new TreeNode()
			{
				Name = Root,
				FullPath = Root,
				FileIcon = image_ComputerIcon.Source
			};

			//UNC Type Add
			string NetworkDirName = Environment.GetFolderPath(Environment.SpecialFolder.NetworkShortcuts);
			DirectoryInfo[] NetworkDirList = new DirectoryInfo(NetworkDirName).GetDirectories();

			foreach (DirectoryInfo NetworkDir in NetworkDirList)
			{
				string DirName = NetworkDir.FullName + "\\target.lnk";
				if (File.Exists(DirName))
				{
					// TODO : UNC Type Add
					// WshShellClass shell = new WshShellClass();
					// WshShell shell = new WshShell(); //Create a new WshShell Interface
					// IWshShortcut link = (IWshShortcut)shell.CreateShortcut(DirName); //Link the interface to our shortcut

					// TreeNode item = new TreeNode()
					// {
					// 	Name = NetworkDir.Name,
					// 	FullPath = link.TargetPath,
					// 	FileIcon = Utility.GetIconImageSource(link.TargetPath),//FileIcon = fileIcon,
					// };
					// GetPCSubDirectories(item);
					// root.Children.Add(item);
				}
			}

			//driver Add
			DriveInfo[] allDrives = DriveInfo.GetDrives();
			foreach (var drive in allDrives)
			{
				if (drive.IsReady)
				{
					TreeNode item = new TreeNode()
					{
						Name = drive.VolumeLabel + " (" + drive.Name.TrimEnd('\\') + ")",
						FullPath = drive.Name,
						FileIcon = Utility.GetIconImageSource(drive.Name),//FileIcon = fileIcon,
					};
					GetPCSubDirectories(item);
					root.Children.Add(item);
				}
			}

			root.Initialize();
			log.Info("Load root directory");

			string userName = "Default";

			string myUserPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			try
			{
				userName = Environment.UserName;
			}
			catch { }

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
		private void GetPCSubDirectories(TreeNode node)
		{
			if (node == null) return;
			if (node.Children.Count > 0) return;

			node.Children.Clear();
			try
			{
				DirectoryInfo dirInfo = new DirectoryInfo(node.FullPath);

				foreach (DirectoryInfo subDir in dirInfo.GetDirectories())
				{
					if (subDir.Name == "Documents and Settings" ||
						subDir.Name == "System Volume Information" ||
						subDir.FullName.IndexOf("$") > 0)
						continue;

					Icon defaultIcon = Utility.GetIcon(subDir.FullName, IconSize.Small, ItemState.Close);
					ImageSource fileIcon = Utility.IconToImageSource(defaultIcon);

					var item = new TreeNode()
					{
						Name = subDir.Name,
						FullPath = subDir.FullName,
						FileIcon = Utility.GetIconImageSource(subDir.FullName) //FileIcon = fileIcon
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
		private bool CheckCheckBox(TreeNode Node, string DirPath)
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

				if (childNode.Children.Count > 0)
				{
					if (CheckCheckBox(childNode, DirPath))
						return true;
				}
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
				if (node.FullPath == DirPath)
				{
					if (node.IsChecked == true)
					{
						node.IsChecked = false;
						return;
					}
				}
				if (node.Children.Count > 0)
				{
					if (UncheckCheckBox(node, DirPath)) return;
				}
			}
			log.Info(DirPath);
		}
		private bool UncheckCheckBox(TreeNode Node, string DirPath)
		{
			bool uncheck = false;

			foreach (var childNode in Node.Children)
			{
				if (childNode.FullPath == DirPath)
				{
					if (childNode.IsChecked == true)
					{
						childNode.IsChecked = false;
						return true;
					}
				}
				if (childNode.Children.Count > 0)
				{
					if (UncheckCheckBox(childNode, DirPath)) return true;
				}
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

		private bool AddPath(string DirPath)
		{
			try
			{
				if (DirPath == Root) { }
				else if (!Job.ExistsDirectory(DirPath))
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
			return true;
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
					List<string> DeletePathList = new List<string>();

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
				if (Node.Children.Count > 0) if (IsCheckedSubNode(Node, MainPath)) return true;
					else return true;
			}
			return false;
		}
		private bool IsCheckedSubNode(TreeNode Node, string DirPath)
		{
			foreach (var SubNode in Node.Children)
			{
				if (SubNode.Name.Equals(DirPath)) if (SubNode.IsAllChildrenChecked()) return true;
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

					if (Node.Children.Count > 0)
					{
						if (FindNodeToPath(Node, DirPath)) return;
					}
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

				if (childNode.Children.Count > 0)
				{
					if (FindNodeToPath(childNode, TargetPath)) return true;
				}
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
			List<string> ExtList = ExtDb.GetExtensionList();

			if (ExtList == null || ExtList.Count == 0)
			{
				Utility.ErrorMessageBox("Extension List is Empty", "ExtensionListUpdate");
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
				//B_ExtAdd.IsEnabled = false;
				foreach (string Ext in ExtensionList)
					if (!Job.WhiteFileExt.Contains(Ext)) FindExtensionList.Add(Ext);
			}
			else
			{
				string FindExt = T_ExtensionName.Text;

				foreach (string Ext in ExtensionList)
				{
					if (!Job.WhiteFileExt.Contains(Ext))
					{
						if (Ext.Contains(FindExt)) FindExtensionList.Add(Ext);
					}
				}

				if (FindExtensionList.Count == 0) B_ExtAdd.IsEnabled = true;
				//else B_ExtAdd.IsEnabled = false;
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
				Utility.ErrorMessageBox("Extension is empty", "Btn_AddExtension");
				return;
			}

			try
			{
				string Extension = T_ExtensionName.Text.ToLower();

				if (Utility.ExtensionCharactersErrorCheck(Extension))
				{
					Utility.ErrorMessageBox("확장자명에 다음 단어를 사용할 수 없습니다.\n[\\, /, :, *, ?, \", <, >, |]", "Extension");
					return;
				}
				if (ExtDb.Check(Extension))
				{
					Utility.ErrorMessageBox("해당 확장자명이 목록에 이미 존재합니다!", "Extension");
					return;
				}

				if (ExtDb.Insert(Extension))
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
					ExtDb.Delete(del);
					FindExtensionList.Remove(del);
				}
			}
			ExtensionListUpdate();
		}
		#endregion Extension Selection


		#region Instant Backup

		#region Analysis

		private bool AnalysisRun()
		{
			log.Info("Start");
			TaskDb.InsertLog("Analysis Start!");
			Analysis.Start();
			Stopwatch sw = new Stopwatch();
			sw.Start();
			State.Filter = true;
			State.Quit = false;

			//Analysis Init
			ButtonLock(false);

			//Analysis
			List<string> extensionList = new List<string>(Job.WhiteFileExt);
			List<string> directoryList = new List<string>(Job.Path);

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
			State.Filter = false;
			State.Quit = true;

			if (Analysis.IsQuit) return false;
			Analysis.Stop();

			log.Info($"End. Time : {sw.ElapsedMilliseconds.ToString()}ms");
			TaskDb.InsertLog($"Analysis End. Time : {sw.ElapsedMilliseconds.ToString()}ms");
			TaskDb.InsertLog($"Upload File Count : {Analysis.UploadFileCount}\tUpload File Size : {MainData.SizeToString(Analysis.UploadFileSize)}");
			return true;
		}

		private void SubDirectory(string ParentDirectory, List<string> ExtensionList)
		{
			DirectoryInfo dInfoParent = new DirectoryInfo(ParentDirectory);

			if (!dInfoParent.Exists) return;
			//add Folder List
			foreach (DirectoryInfo dInfo in dInfoParent.GetDirectories())
			{
				if (Analysis.IsQuit) break;
				try { SubDirectory(dInfo.FullName, ExtensionList); } catch { }
			}
			AddBackupFile(ParentDirectory, ExtensionList);
		}
		private void AddBackupFile(string ParentDirectory, List<string> ExtensionList)
		{
			//add File List
			string[] files = Directory.GetFiles(ParentDirectory);

			// 파일 나열
			foreach (string file in files)
			{

				if (Analysis.IsQuit) break;
				FileInfo info = new FileInfo(file);

				if (!info.Exists) continue;

				if ((info.Attributes & FileAttributes.System) == FileAttributes.System) { /*empty*/ }
				else if (ExtensionList.Contains(info.Extension.Replace(".", "").ToLower()))
				{
					if (info.FullName.IndexOf("$") < 0)
					{
						Analysis.UploadFileCount++;
						Analysis.UploadFileSize += info.Length;

					}
				}
				Analysis.CheckCount++;
			}
		}
		#endregion Analysis

		private bool PathCheck(out string RootPath)
		{
			RootPath = string.Empty;

			foreach (string Root in Job.Path)
			{
				DirectoryInfo dir = new DirectoryInfo(Root);
				if (!dir.Exists)
				{
					RootPath = Root;
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
			if (Analysis.IsRunning) { Utility.ErrorMessageBox("Analysis is Running!", CLASS_NAME); return; }
			if (Job.WhiteFileExt.Count == 0) { Utility.ErrorMessageBox("Extension List is empty!", CLASS_NAME); return; }
			if (Job.Path.Count == 0) { Utility.ErrorMessageBox("Directory List is empty!", CLASS_NAME); return; }
			if (PathCheck(out string ErrorPath)) { Utility.ErrorMessageBox(string.Format("[{0}] is not exists!", ErrorPath), CLASS_NAME); return; }
			if (C_UserList.SelectedIndex < GlobalUserList.Count)
			{
				Job.UserID = GlobalUserList[C_UserList.SelectedIndex].Id;
				Job.IsGlobalUser = true;
			}
			else
			{
				Job.UserID = NormalUserList[C_UserList.SelectedIndex - GlobalUserList.Count].Id;
				Job.IsGlobalUser = false;
			}
			State.Quit = false;
			InstantData instantData = new InstantData();
			instantData.Analysis = false;

			if (!JobDb.PutJobData(Job))
			{
				Utility.ErrorMessageBox("Error : Failed to save job", "Save");
				return;
			}

			ChangeData(false);
			ButtonLock(false);
			if (Job.Id < 0) Job.Id = JobDb.GetJobDataId(Job.HostName, Job.JobName);
			//BackupStart = true;

		}
		private void Btn_Analysis(object sender, RoutedEventArgs e)
		{
			if (Analysis.IsRunning) { Utility.ErrorMessageBox("Analysis is Running!", CLASS_NAME); return; }
			if (Job.WhiteFileExt.Count == 0) { Utility.ErrorMessageBox("Extension List is empty!", CLASS_NAME); return; }
			if (Job.Path.Count == 0) { Utility.ErrorMessageBox("Directory List is empty!", CLASS_NAME); return; }
			if (PathCheck(out string ErrorPath)) { Utility.ErrorMessageBox(string.Format("[{0}] is not exists!", ErrorPath), CLASS_NAME); return; }

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
				Job.UserID = GlobalUserList[C_UserList.SelectedIndex].Id;
				Job.IsGlobalUser = true;
			}
			else
			{
				Job.UserID = NormalUserList[C_UserList.SelectedIndex - GlobalUserList.Count].Id;
				Job.IsGlobalUser = false;
			}
		}

		private void Btn_InstantQuit(object sender, RoutedEventArgs e)
		{
			if (Analysis.IsRunning) TaskDb.InsertLog("Analysis Stop!");

			State.Quit = true;
			Analysis.Stop();

		}
		#endregion Instant Backup

		#region State

		private void StateCheck(object sender, ElapsedEventArgs e)
		{
			if (Job.Policy == JobPolicyType.Now)
			{
				//if (State.Error) State.Quit = true;

				if (Analysis.IsRunning) ButtonLock(false);
				else if (!State.Quit) ButtonLock(false);
				else
				{
					if (State.Filter || State.VSS || State.Sender) { ButtonLock(false); BackupStart = true; }
					else
					{
						if (BackupStart) { BackupStart = false; }
						ButtonLock(true);
					}
				}
			}

			if (State.Filter) Image_Filter.Dispatcher.Invoke(delegate
			{
				Image_Filter.Source = Image_CircleBlue.Source;
				State_Filter.Content = "구동중";
				if (Job.Policy == JobPolicyType.Now)
				{
					Image_VSS.Source = Image_CircleBlue.Source;
					State_VSS.Content = "분석중";
				}
			});
			else Image_Filter.Dispatcher.Invoke(delegate
			{
				if (Job.Policy == JobPolicyType.Now)
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

			if (State.VSS) Image_VSS.Dispatcher.Invoke(delegate
			{
				if (Job.Policy == JobPolicyType.Now)
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
				if (Job.Policy == JobPolicyType.Now)
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

			if (State.Sender) Image_Sender.Dispatcher.Invoke(delegate { Image_Sender.Source = Image_TriangleGreen.Source; State_Sender.Content = "업로드중"; });
			else Image_Sender.Dispatcher.Invoke(delegate { Image_Sender.Source = Image_SquareGray.Source; State_Sender.Content = "정지"; });

			if (State.Error) Image_Sender.Dispatcher.Invoke(delegate { Image_Sender.Source = Image_TriangleRed.Source; State_Sender.Content = "접속에러"; });

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
			List<string> LogList = TaskDb.GetLog(StartPoint);

			if (LogList.Count > 0)
			{
				StartPoint += LogList.Count;

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

	}
}