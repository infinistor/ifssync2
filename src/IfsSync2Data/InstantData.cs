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
using Microsoft.Win32;
using System;

namespace IfsSync2Data
{
    class InstantData
    {

        private const string ANALYSIS  = "Analysis";
        private const string RUNNING   = "Running";
        private const string TOTAL  = "TotalCount";
        private const string UPLOAD = "UploadCount";
        private const string PERCENT = "PercentCount";
        /***************************************************************/
        private const int MY_TRUE = 1;
        private const int MY_FALSE = 0;
        /***************************************************************/
        private readonly RegistryKey InstantKey = null;

        public bool Analysis
        {
            get
            {
                int value = MY_FALSE;
                try { value = Convert.ToInt32(InstantKey.GetValue(ANALYSIS)); } catch { return false; }

                if (value == MY_FALSE) return false;
                else return true;
            }
            set
            {
                int temp = MY_FALSE;
                if (value) temp = MY_TRUE;

                InstantKey.SetValue(ANALYSIS, temp, RegistryValueKind.DWord);
            }
        }
        public bool Running
        {
            get
            {
                int value = MY_FALSE;
                try { value = Convert.ToInt32(InstantKey.GetValue(RUNNING)); } catch { return false; }

                if (value == MY_FALSE) return false;
                else return true;
            }
            set
            {
                int temp = MY_FALSE;
                if (value) temp = MY_TRUE;

                InstantKey.SetValue(RUNNING, temp, RegistryValueKind.DWord);
            }
        }

        public long Total
        {
            get
            {
                long value = 0;
                try { value = Convert.ToInt64(InstantKey.GetValue(TOTAL)); } catch { }
                return value;
            }
            set
            {
                InstantKey.SetValue(TOTAL, value, RegistryValueKind.QWord);
            }
        }
        public long Upload
        {
            get
            {
                long value = 0;
                try { value = Convert.ToInt64(InstantKey.GetValue(UPLOAD)); } catch { }
                return value;
            }
            set
            {
                InstantKey.SetValue(UPLOAD, value, RegistryValueKind.QWord);
            }
        }
        public long Percent
        {
            get
            {
                long value = 0;
                try { value = Convert.ToInt64(InstantKey.GetValue(PERCENT)); } catch { }
                return value;
            }
            set
            {
                InstantKey.SetValue(PERCENT, value, RegistryValueKind.QWord);
            }
        }

        public InstantData()
        {
            InstantKey = Registry.LocalMachine.OpenSubKey(MainData.INSTANT_REGISTRY_ROOT_NAME, true);
            if (InstantKey == null)
            {
                InstantKey = Registry.LocalMachine.CreateSubKey(MainData.INSTANT_REGISTRY_ROOT_NAME, true);
                InstantKey.SetValue(ANALYSIS, MY_TRUE, RegistryValueKind.DWord);
                InstantKey.SetValue(TOTAL, 0, RegistryValueKind.QWord);
                InstantKey.SetValue(UPLOAD, 0, RegistryValueKind.QWord);
                InstantKey.SetValue(PERCENT, 0, RegistryValueKind.QWord);
            }
        }

        public void Clear()
        {
            Total = Upload = Percent = 0;
        }
    }
}
