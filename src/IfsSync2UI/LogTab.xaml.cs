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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using IfsSync2Data;

namespace IfsSync2UI
{
    /// <summary>
    /// LogTab.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LogTab : UserControl
    {
        public int LogIndex = 0;
        public LogTab()
        {
            InitializeComponent();
        }

        #region Log Event
        /*******************************Log***********************************/
        public void TaskLogUpdates(List<TaskData> Tasks)
        {
            LogIndex += Tasks.Count;
            try
            {
                L_TaskView.Dispatcher.Invoke(delegate { foreach (var Task in Tasks) L_TaskView.Items.Add(Task); });
            }
            catch { }
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
        GridViewColumnHeader LastHeaderClicked = null;
        ListSortDirection LastDirection = ListSortDirection.Ascending;
        private void LogViewClicked(object sender, RoutedEventArgs e)
        {
            ListSortDirection direction;

            if (e.OriginalSource is GridViewColumnHeader headerClicked)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != LastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (LastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    var columnBinding = headerClicked.Column.DisplayMemberBinding as System.Windows.Data.Binding;
                    var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

                    Sort(sortBy, direction);

                    if (direction == ListSortDirection.Ascending)
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowUp"] as DataTemplate;
                    }
                    else
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowDown"] as DataTemplate;
                    }

                    // Remove arrow from previously sorted header
                    if (LastHeaderClicked != null && LastHeaderClicked != headerClicked)
                    {
                        LastHeaderClicked.Column.HeaderTemplate = null;
                    }

                    LastHeaderClicked = headerClicked;
                    LastDirection = direction;
                }
            }
        }
        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(L_TaskView.Items);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }
        #endregion Log Event

        private void L_TaskView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //출처: https://icodebroker.tistory.com/3500 [ICODEBROKER]
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (L_TaskView.SelectedIndex < 0) return;

                // 키 입력을 처리한다.
                string Msg = string.Empty;

                foreach(var item in L_TaskView.SelectedItems)
                {
                    TaskData Data = item as TaskData;
                    Msg += string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
                        Data.Index, Data.StrTaskName, Data.FilePath, Data.NewFilePath, Data.EventTime, Data.UploadTime, Data.Result);
                }
                Clipboard.SetText(Msg);

                MessageBox.Show("클립보드로 복사되었습니다.", "LogView");
            }
        }
        public bool SaveCSV(string FileName)
        {
            if (L_TaskView.SelectedIndex < 0) return false;

            try
            {
                if (File.Exists(FileName)) File.Delete(FileName);
                StreamWriter sw = new StreamWriter(FileName, false, System.Text.Encoding.Unicode);

                //Header
                string columnheader = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
                "Index", "TaskName", "FilePath", "NewFilePath", "EventTime", "UploadTime", "Result");
                sw.WriteLine(columnheader);

                foreach (var item in L_TaskView.SelectedItems)
                {
                    TaskData Data = item as TaskData;
                    sw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
                        Data.Index, Data.StrTaskName, Data.FilePath, Data.NewFilePath, Data.EventTime, Data.UploadTime, Data.Result);
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
