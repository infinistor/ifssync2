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
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using IfsSync2Common;
using log4net;
using System.Runtime.Versioning;

namespace IfsSync2UI
{
	/// <summary>
	/// LogViewWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	[SupportedOSPlatform("windows6.1")]
	public partial class LogViewWindow : Window
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(LogViewWindow));
		private readonly JobData job;

		private readonly LogTab successTab = new();
		private readonly LogTab failureTab = new();

		private readonly TaskDbManager taskSql;

		public bool IsClose { get; private set; } = false;

		public LogViewWindow(JobData job)
		{
			InitializeComponent();
			this.job = job;
			TabInit();
			taskSql = new TaskDbManager(job.JobName);
			Title = job.JobName + " Log View";
			UpdateLogList();
		}

		private void UpdateLogList()
		{
			var successIndex = successTab.LogIndex;
			var failureIndex = failureTab.LogIndex;

			var success = taskSql.GetSuccessList(successIndex, 10000);
			var failure = taskSql.GetFailureList(failureIndex, 10000);

			successTab.TaskLogUpdates(success);
			failureTab.TaskLogUpdates(failure);
		}

		private void TabInit()
		{
			try
			{
				MainTab.Dispatcher.Invoke(delegate
				{
					TabItem success = new() { Content = successTab, Header = "Success" };
					MainTab.Items.Add(success);

					TabItem failure = new() { Content = failureTab, Header = "Failure" };
					MainTab.Items.Add(failure);
				});
			}
			catch (Exception e) { log.Error(e); }
		}

		private void Btn_Clear(object sender, RoutedEventArgs e)
		{
			successTab.Clear();
			failureTab.Clear();
		}

		private void Btn_Refresh(object sender, RoutedEventArgs e)
		{
			UpdateLogList();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			IsClose = true;
		}

		private void Btn_SaveCSV(object sender, RoutedEventArgs e)
		{
			int index = MainTab.SelectedIndex;
			if (index < 0)
			{
				Utility.ErrorMessageBox("Not Selected Tab", Title);
				return;
			}

			FileDialog fileDialog = new SaveFileDialog
			{
				FileName = "Data.csv",
				Filter = "Excel File(*.csv)|*.csv"
			};

			if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.Cancel) return;

			if (string.IsNullOrWhiteSpace(fileDialog.FileName)) return;
			var fileName = fileDialog.FileName;
			var item = MainTab.Items[index] as TabItem;
			var tab = item.Content as LogTab;

			if (tab.SaveCSV(fileName)) System.Windows.MessageBox.Show(fileName + "\n저장 성공!", Title);
			else Utility.ErrorMessageBox(fileName + "\n저장 실패!", Title);
		}
	}
}
