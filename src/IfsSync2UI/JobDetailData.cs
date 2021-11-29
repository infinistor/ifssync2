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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using IfsSync2Data;

namespace IfsSync2UI
{
    public class JobDetailData : INotifyPropertyChanged
    {
        public string JobName { get; set; }
        public int ID { get; set; }
        public int UserID { get; set; }

        public string HostName { get; set; }

        public bool Delete { get; set; }

        public string JobType { get; set; }
        public string StorageName { get; set; }
        public ObservableCollection<string> ExtensionList { get; set; }
        public bool FilterFlag { get { return State.Filter; }}
        public bool VSSFlag { get { return State.VSS; } }
        public bool SenderFlag { get { return State.Sender; } }
        public string ButtonTag { get { return JobName; } }

        public Visibility BtnVisibility { set { ButtonVisibility = value; OnPropertyChanged("Visibility"); } }
        public Visibility ButtonVisibility { set; get; }

        public bool Update { set { OnPropertyChanged("Update"); } }

        public readonly JobState State;

        public JobDetailData(string _HostName, string _JobName, int JobID)
        {
            JobName = _JobName;
            HostName = _HostName;
            State = new JobState(HostName, JobName);
            ID = JobID;
            ButtonVisibility = Visibility.Visible;
        }

        public ImageSource FilterIcon
        {
            get
            {
                if (FilterFlag) return CircleBlue;
                else return CircleGray;
            }
        }
        public ImageSource VSSIcon { 
            get
            {
                if (VSSFlag) return CircleBlue;
                else return CircleGray;
            }
        }
        public ImageSource SenderIcon {
            get
            {
                if (State.Error) return TriangleRed;
                else if (SenderFlag) return TriangleGreen;
                else return SquareGray;
            }
        }

        public ImageSource CircleBlue   ;
        public ImageSource CircleGray   ;
        public ImageSource TriangleRed  ;
        public ImageSource SquareGray   ;
        public ImageSource TriangleGreen;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public string Status { get { return State.Status; } }
    }
}
