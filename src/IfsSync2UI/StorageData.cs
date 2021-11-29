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
using Amazon.S3.Model.Internal.MarshallTransformations;
using IfsSync2Data;

namespace IfsSync2UI
{
    public class StorageData
    {
        public readonly int ID;
        public readonly string HostName;
        public readonly string UserName;
        
        public string AccessKey { get; set; }
        public string AccessSecret { get; set; }
        public string S3FileManagerURL { get; set; }

        public bool Delete { get; set; }
        public string StorageName { get; set; }
        public string URL { get; set; }
        public bool IsGlobalUser
        {
            get
            {
                if (string.IsNullOrWhiteSpace(HostName)) return true;
                else return false;
            }
        }
        public long TotalSize { get; set; }
        public long UsedSize { get; set; }

        public bool IsAWS
        {
            get
            {
                if (URL.StartsWith(MainData.HTTP)) return false;
                return true;
            }
        }
        public long FreeSize { get { return TotalSize - UsedSize; } }
        public double Rate { 
            get 
            {
                if (TotalSize == 0) return 0;
                return (double)UsedSize / (double)TotalSize * 100.0;
            }
        }

        public string StrTotalSize { get { return MainData.SizeToString(TotalSize); } }
        public string StrUsedSize { get { return MainData.SizeToString(UsedSize); } }
        public string StrFreeSize { get { return MainData.SizeToString(FreeSize); } }
        public string StrRate { 
            get
            {
                if (TotalSize == 0) return "ERROR";
                return string.Format("{0:0.0} %", Rate);
            }
        }
        
        public StorageData(int _ID, string _HostName, string _UserName)
        {
            UserName     = _UserName;
            ID           = _ID;
            HostName     = _HostName;
            Delete       = false;
        }
    }
}
