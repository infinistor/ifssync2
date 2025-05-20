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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using IfsSync2Common;
using log4net;

namespace IfsSync2UI
{
	/// <summary>
	/// LogTab.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class LogTab : UserControl
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(LogTab));

		public int LogIndex { get; private set; } = int.MaxValue;
		public LogTab()
		{
			InitializeComponent();
		}

		#region Log Event
		/*******************************Log***********************************/
		public void TaskLogUpdates(List<TaskData> Tasks)
		{
			try
			{
				LogIndex = Tasks.Count > 0 ? (int)Tasks[^1].Index : int.MaxValue;
				L_TaskView.Dispatcher.Invoke(delegate { foreach (var Task in Tasks) L_TaskView.Items.Add(Task); });
			}
			catch (Exception ex)
			{
				log.Error($"TaskLogUpdates : {ex.Message}");
			}
		}
		public void TaskLogUpdate(TaskData Task)
		{
			for (int i = 0; i < L_TaskView.Items.Count; i++)
			{
				TaskData item = L_TaskView.Items[i] as TaskData;
				if (item.Index == Task.Index)
				{
					L_TaskView.Items.RemoveAt(i);
					L_TaskView.Items.Add(Task);
					break;
				}
			}
		}

		public void Clear()
		{
			L_TaskView.Items.Clear();
		}
		/****************************Log Sort********************************/
		GridViewColumnHeader lastHeaderClicked = null;
		ListSortDirection lastDirection = ListSortDirection.Ascending;
		private void LogViewClicked(object sender, RoutedEventArgs e)
		{
			ListSortDirection direction;

			if (e.OriginalSource is GridViewColumnHeader headerClicked && headerClicked.Role != GridViewColumnHeaderRole.Padding)
			{
				if (headerClicked != lastHeaderClicked)
				{
					direction = ListSortDirection.Ascending;
				}
				else
				{
					if (lastDirection == ListSortDirection.Ascending)
						direction = ListSortDirection.Descending;
					else
						direction = ListSortDirection.Ascending;
				}

				var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
				var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

				Sort(sortBy, direction);

				if (direction == ListSortDirection.Ascending)
					headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowUp"] as DataTemplate;
				else
					headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowDown"] as DataTemplate;

				// Remove arrow from previously sorted header
				if (lastHeaderClicked != null && lastHeaderClicked != headerClicked)
					lastHeaderClicked.Column.HeaderTemplate = null;

				lastHeaderClicked = headerClicked;
				lastDirection = direction;
			}
		}
		private void Sort(string sortBy, ListSortDirection direction)
		{
			ICollectionView dataView = CollectionViewSource.GetDefaultView(L_TaskView.Items);

			dataView.SortDescriptions.Clear();
			SortDescription sd = new(sortBy, direction);
			dataView.SortDescriptions.Add(sd);
			dataView.Refresh();
		}
		#endregion Log Event

		private void L_TaskView_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
			{
				if (L_TaskView.SelectedIndex < 0) return;

				// 키 입력을 처리한다.
				StringBuilder msg = new();

				foreach (var item in L_TaskView.SelectedItems)
				{
					var data = item as TaskData;
					msg.Append($"{data.Index}\t{data.StrTaskType}\t{data.FilePath}\t{data.NewFilePath}\t{data.EventTime}\t{data.UploadTime}\t{data.Result}\n");
				}
				Clipboard.SetText(msg.ToString());

				MessageBox.Show("클립보드로 복사되었습니다.", "LogView");
			}
		}
		public bool SaveCSV(string fileName)
		{
			// L_TaskView가 비어있는 경우 저장할 필요가 없으므로 제외
			if (L_TaskView.Items.Count == 0) return false;

			try
			{
				if (File.Exists(fileName)) File.Delete(fileName);
				StreamWriter sw = new(fileName, false, Encoding.UTF8);

				//Header
				string columnheader = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
				"Index", "TaskName", "FilePath", "NewFilePath", "EventTime", "UploadTime", "Result");
				sw.WriteLine(columnheader);

				// 선택된 항목이 아닌 모든 항목을 저장
				foreach (var item in L_TaskView.Items)
				{
					TaskData data = item as TaskData;
					sw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
						data.Index, data.StrTaskType, data.FilePath, data.NewFilePath, data.EventTime, data.UploadTime, data.Result);
				}
				sw.Close();
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
