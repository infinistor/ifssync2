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
using System.Collections.ObjectModel;
using IfsSync2Common;

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

		/// <summary>
		/// 파일 이름 변경 또는 이동 이벤트의 유형을 결정합니다.
		/// </summary>
		/// <param name="Path">모니터링 중인 경로 목록</param>
		/// <param name="FilePath">원본 파일 경로</param>
		/// <param name="NewFilePath">새 파일 경로</param>
		/// <returns>파일 이벤트 유형 (None, SaveFile, SaveNewFile, Rename, Delete 중 하나)</returns>
		public static EventList FindSaveByRenameEvent(ObservableCollection<string> Path, string FilePath, string NewFilePath)
		{
			if (RecycleCheck(NewFilePath))
			{
				if (IsLikeFolder(FilePath))
				{
					return EventList.Delete;
				}
				return EventList.None;
			}
			if (CSVSaveCheck(FilePath, NewFilePath)) return EventList.SaveFile;
			if (PDFSaveCheck(FilePath, NewFilePath)) return EventList.SaveNewFile;
			if (MoveSaveCheck(Path, FilePath)) return EventList.SaveNewFile;
			if (TmpSaveCheck(FilePath, NewFilePath)) return EventList.SaveNewFile;
			if (MoveDeleteCheck(Path, NewFilePath)) return EventList.Delete;
			if (IsTempFile(FilePath, NewFilePath)) return EventList.None;
			return EventList.Rename;
		}

		/// <summary>
		/// 파일 경로가 휴지통(Recycle Bin)에 있는지 확인합니다.
		/// </summary>
		/// <param name="NewFilePath">확인할 파일 경로</param>
		/// <returns>휴지통 경로이면 true, 아니면 false</returns>
		public static bool RecycleCheck(string NewFilePath)
		{
			return NewFilePath.Contains(RECYCLE);
		}

		/// <summary>
		/// 파일이 CSV 파일인지 확인합니다.
		/// </summary>
		/// <param name="FilePath">원본 파일 경로</param>
		/// <param name="NewFilePath">새 파일 경로</param>
		/// <returns>CSV 파일이면 true, 아니면 false</returns>
		private static bool CSVSaveCheck(string FilePath, string NewFilePath)
		{
			if (FilePath.EndsWith(CSV, StringComparison.OrdinalIgnoreCase))
			{
				string NewFileName = IfsSync2Utilities.GetFileName(NewFilePath);

				return IsFileNameHex(NewFileName);
			}
			return false;
		}

		/// <summary>
		/// 파일이 PDF 파일인지 확인합니다.
		/// </summary>
		/// <param name="FilePath">원본 파일 경로</param>
		/// <param name="NewFilePath">새 파일 경로</param>
		private static bool PDFSaveCheck(string FilePath, string NewFilePath)
		{
			return NewFilePath.EndsWith(PDF, StringComparison.OrdinalIgnoreCase)
				&& FilePath.EndsWith(TMP, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// 파일이 TMP 파일인지 확인합니다.
		/// </summary>
		/// <param name="FilePath">원본 파일 경로</param>
		/// <param name="NewFilePath">새 파일 경로</param>
		private static bool TmpSaveCheck(string FilePath, string NewFilePath)
		{
			if (TempFileCheck(TMP, FilePath)) { if (!TempFileCheck(TMP, NewFilePath)) return true; }
			else if (TempFileCheck(TEMP, FilePath)) { if (!TempFileCheck(TEMP, NewFilePath)) return true; }
			else if (IsFileNameHex(IfsSync2Utilities.GetFileName(FilePath))) { if (!TempFileCheck(TEMP, NewFilePath)) return true; }
			else
			{
				string[] result = FilePath.Split(DotSeparator);
				string Ext = result[^1];
				if (Ext.Length == 4 && Ext.EndsWith(VISUALSTUDIO_TMP)
					&& !NewFilePath.EndsWith(TMP, StringComparison.OrdinalIgnoreCase)) return true;
			}
			return false;
		}

		/// <summary>
		/// 파일이 임시 파일인지 확인합니다.
		/// </summary>
		/// <param name="TempExt">임시 파일 확장자</param>
		/// <param name="FilePath">확인할 파일 경로</param>
		/// <returns>임시 파일이면 true, 아니면 false</returns>
		private static bool TempFileCheck(string TempExt, string FilePath)
		{
			return FilePath.EndsWith(TempExt, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// 파일이 모니터링 경로 외부로 이동되었는지 확인합니다.
		/// </summary>
		/// <param name="PathList">모니터링 중인 경로 목록</param>
		/// <param name="FilePath">원본 파일 경로</param>
		private static bool MoveSaveCheck(ObservableCollection<string> PathList, string FilePath)
		{
			foreach (string Root in PathList)
				if (!FilePath.StartsWith(Root)) return true;
			return false;
		}
		/// <summary>
		/// 파일이 모니터링 경로 외부로 이동되었는지 확인합니다.
		/// 모니터링 경로 외부로 이동된 파일은 삭제 이벤트로 처리됩니다.
		/// </summary>
		/// <param name="PathList">모니터링 중인 경로 목록</param>
		/// <param name="NewFilePath">새 파일 경로</param>
		/// <returns>파일이 모니터링 경로 외부로 이동되었으면 true, 아니면 false</returns>
		public static bool MoveDeleteCheck(ObservableCollection<string> PathList, string NewFilePath)
		{
			foreach (string Root in PathList)
				if (!NewFilePath.StartsWith(Root)) return true;
			return false;
		}

		/// <summary>
		/// 파일 이름이 16진수 형식인지 확인합니다.
		/// </summary>
		/// <param name="FileName">확인할 파일 이름</param>
		/// <returns>16진수 형식이면 true, 아니면 false</returns>
		private static bool IsFileNameHex(string FileName)
		{
			string ToUpper_FileName = FileName.ToUpper();

			foreach (char temp in ToUpper_FileName)
			{
				if (!((temp >= '0' && temp <= '9') || (temp >= 'A' && temp <= 'F')))
					return false;
			}
			return true;
		}

		/// <summary>
		/// 파일이 임시 파일인지 확인합니다.
		/// </summary>
		/// <param name="FilePath">원본 파일 경로</param>
		/// <param name="NewFilePath">새 파일 경로</param>
		private static bool IsTempFile(string FilePath, string NewFilePath)
		{
			if (FilePath.Contains(APP_TEMP_PATH)) return true;
			if (NewFilePath.EndsWith(TMP, StringComparison.OrdinalIgnoreCase)) return true;
			return false;
		}

		/// <summary>
		/// 파일 경로가 폴더일 가능성이 높은지 확인합니다.
		/// 확장자가 없는 경우 폴더로 간주합니다.
		/// </summary>
		/// <param name="path">확인할 파일 경로</param>
		/// <returns>폴더일 가능성이 높으면 true, 아니면 false</returns>
		public static bool IsLikeFolder(string path)
		{
			try
			{
				// 경로가 없거나 비어있는 경우
				if (string.IsNullOrWhiteSpace(path))
					return false;

				// 경로가 백슬래시나 슬래시로 끝나는 경우
				if (path.EndsWith('\\') || path.EndsWith('/'))
					return true;

				// 확장자가 없는 경우 폴더로 간주
				return string.IsNullOrEmpty(System.IO.Path.GetExtension(path));
			}
			catch
			{
				// 예외 발생 시 false 반환
				return false;
			}
		}
	}
}
