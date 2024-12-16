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
using System;
using System.Collections.ObjectModel;
using IfsSync2Data;

namespace IfsSync2Filter
{
	public class FilterEventHandler
	{
		public enum EventList
		{
			None = 0,
			SaveFile,
			SaveNewFile,
			Rename,
			Delete
		}

		private const string RECYCLE = "RECYCLE";
		private const string CSV = ".csv";
		private const char DotSeparator = '.';
		private const string PDF = ".pdf";
		private const string TMP = ".tmp";
		private const string TEMP = ".tmp";
		private const string VISUALSTUDIO_TMP = "~";
		private const string APP_TEMP_PATH = @"\AppData\Local\Temp\";

		public static EventList FindSaveByRenameEvent(ObservableCollection<string> Path, string FilePath, string NewFilePath)
		{
			if (RecycleCheck(NewFilePath)) return EventList.None;
			if (CSVSaveCheck(FilePath, NewFilePath)) return EventList.SaveFile;
			if (PDFSaveCheck(FilePath, NewFilePath)) return EventList.SaveNewFile;
			if (MoveSaveCheck(Path, FilePath)) return EventList.SaveNewFile;
			if (TmpSaveCheck(FilePath, NewFilePath)) return EventList.SaveNewFile;
			if (MoveDeleteCheck(Path, NewFilePath)) return EventList.Delete;
			if (IsTempFile(FilePath, NewFilePath)) return EventList.None;
			return EventList.Rename;
		}

		private static bool RecycleCheck(string NewFilePath)
		{
			if (NewFilePath.IndexOf(RECYCLE) > 0) return true;
			return false;
		}
		private static bool CSVSaveCheck(string FilePath, string NewFilePath)
		{
			if (FilePath.EndsWith(CSV, StringComparison.OrdinalIgnoreCase))
			{
				string NewFileName = MainData.GetFileName(NewFilePath);

				return IsFileNameHex(NewFileName);
			}
			return false;
		}
		private static bool PDFSaveCheck(string FilePath, string NewFilePath)
		{
			if (NewFilePath.EndsWith(PDF, StringComparison.OrdinalIgnoreCase))
			{
				if (FilePath.EndsWith(TMP, StringComparison.OrdinalIgnoreCase)) return true;
			}
			return false;
		}
		private static bool TmpSaveCheck(string FilePath, string NewFilePath)
		{
			if (TempFileCheck(TMP, FilePath)) { if (!TempFileCheck(TMP, NewFilePath)) return true; }
			else if (TempFileCheck(TEMP, FilePath)) { if (!TempFileCheck(TEMP, NewFilePath)) return true; }
			else if (IsFileNameHex(MainData.GetFileName(FilePath))) { if (!TempFileCheck(TEMP, NewFilePath)) return true; }
			else
			{
				string[] result = FilePath.Split(DotSeparator);
				string Ext = result[result.Length - 1];
				if (Ext.Length == 4 && Ext.EndsWith(VISUALSTUDIO_TMP))
					if (!NewFilePath.EndsWith(TMP, StringComparison.OrdinalIgnoreCase)) return true;
			}
			return false;
		}

		private static bool TempFileCheck(string TempExt, string FilePath)
		{
			return FilePath.EndsWith(TempExt, StringComparison.OrdinalIgnoreCase);
		}

		private static bool MoveSaveCheck(ObservableCollection<string> PathList, string FilePath)
		{
			foreach (string Root in PathList) if (!FilePath.StartsWith(Root)) return true;

			return false;
		}
		public static bool MoveDeleteCheck(ObservableCollection<string> PathList, string NewFilePath)
		{
			foreach (string Root in PathList) if (!NewFilePath.StartsWith(Root)) return true;

			return false;
		}
		private static bool IsFileNameHex(string FileName)
		{
			string ToUpper_FileName = FileName.ToUpper();

			foreach (char temp in ToUpper_FileName)
			{
				if ((temp >= '0') && (temp <= '9')) continue;
				else if ((temp >= 'A') && (temp <= 'F')) continue;
				else return false;
			}
			return true;
		}
		private static bool IsTempFile(string FilePath, string NewFilePath)
		{
			if (FilePath.IndexOf(APP_TEMP_PATH) > 0) return true;
			if (NewFilePath.EndsWith(TMP, StringComparison.OrdinalIgnoreCase)) return true;
			return false;
		}


	}
}
