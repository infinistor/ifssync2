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
using System.Reflection;
using System.Security.Cryptography;

namespace IfsSync2Common
{
	/// <summary>
	/// IfsSync2 애플리케이션에서 사용되는 일반 유틸리티 메서드를 제공하는 클래스
	/// </summary>
	public static class IfsSync2Utilities
	{
		#region 파일 및 경로 관련 유틸리티
		/// <summary>
		/// 실행 파일 경로를 생성합니다.
		/// </summary>
		/// <param name="targetPath">대상 경로</param>
		/// <param name="fileName">파일 이름</param>
		/// <returns>실행 파일 경로</returns>
		public static string CreateExeFilePath(string targetPath, string fileName)
			=> Path.Combine(targetPath, fileName + ".exe");

		/// <summary>
		/// 아이콘 파일 경로를 생성합니다.
		/// </summary>
		/// <param name="targetPath">대상 경로</param>
		/// <param name="iconName">아이콘 이름</param>
		/// <returns>아이콘 파일 경로</returns>
		public static string CreateIconFilePath(string targetPath, string iconName)
			=> Path.Combine(targetPath, iconName + ".ico");

		/// <summary>
		/// 파일 이름만 추출합니다.
		/// </summary>
		/// <param name="filePath">파일 경로</param>
		/// <returns>파일 이름</returns>
		public static string GetFileName(string filePath)
		{
			string[] result = filePath.Split(Path.DirectorySeparatorChar);
			return result[^1];
		}

		/// <summary>
		/// 파일을 생성합니다. 필요한 경우 디렉토리도 함께 생성합니다.
		/// </summary>
		/// <param name="filePath">파일 경로</param>
		/// <returns>성공 여부</returns>
		public static bool CreateFile(string filePath)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(filePath)) return false;

				var dirPath = Path.GetDirectoryName(filePath);
				if (string.IsNullOrWhiteSpace(dirPath)) return false;

				// 폴더가 없으면 생성
				if (!Directory.Exists(dirPath))
					Directory.CreateDirectory(dirPath);

				// 파일이 없으면 생성
				if (!File.Exists(filePath))
					File.Create(filePath).Close();

				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// UNC 폴더인지 확인합니다.
		/// </summary>
		/// <param name="rootPath">확인할 경로</param>
		/// <returns>UNC 폴더 여부</returns>
		public static bool CheckUNCFolder(string rootPath)
		{
			return rootPath.StartsWith(@"\\");
		}

		/// <summary>
		/// 드라이브가 접근 가능한지 확인합니다.
		/// </summary>
		/// <param name="path">확인할 경로</param>
		/// <returns>접근 가능 여부</returns>
		/// <exception cref="DirectoryNotFoundException">경로가 존재하지 않는 경우</exception>
		/// <exception cref="IOException">경로 접근 오류</exception>
		public static bool IsDriveAccessible(string path)
		{
			path = path.TrimEnd('*');
			var root = Path.GetPathRoot(path);

			if (!string.IsNullOrWhiteSpace(root) && root.Length >= 2 && root[1] == ':')
			{
				var driveType = new DriveInfo(root).DriveType;
				// 로컬 고정 디스크인 경우만 체크
				if (driveType == DriveType.Fixed)
				{
					bool exists = Directory.Exists(path);
					if (!exists)
						throw new DirectoryNotFoundException($"Path not accessible: {path}");
					return true;
				}
			}

			// 네트워크 드라이브나 다른 타입은 체크하지 않고 true 반환
			return true;
		}

		/// <summary>
		/// 로그 폴더 경로를 가져옵니다.
		/// </summary>
		/// <param name="processName">프로세스 이름</param>
		/// <returns>로그 폴더 경로</returns>
		public static string GetLogFolder(string processName)
			=> $"{IfsSync2Constants.LOG_DIRECTORY_NAME}{processName}";

		/// <summary>
		/// 데이터베이스 파일 경로를 가져옵니다.
		/// </summary>
		/// <param name="dbName">데이터베이스 이름</param>
		/// <returns>데이터베이스 파일 경로</returns>
		public static string GetDBFilePath(string dbName)
			=> $"{IfsSync2Constants.DB_DIRECTORY_NAME}{dbName}.{IfsSync2Constants.DB_EXTENSION_NAME}";
		#endregion

		#region 문자열 및 형식 관련 유틸리티
		/// <summary>
		/// 뮤텍스 이름을 생성합니다.
		/// </summary>
		/// <param name="name">뮤텍스 기본 이름</param>
		/// <returns>형식화된 뮤텍스 이름</returns>
		public static string CreateMutexName(string name)
			=> $"{IfsSync2Constants.MUTEX_GLOBAL_NAME}{{{name}}}";

		/// <summary>
		/// 레지스트리 작업 이름을 생성합니다.
		/// </summary>
		/// <param name="hostName">호스트 이름</param>
		/// <param name="jobName">작업 이름</param>
		/// <returns>레지스트리 작업 이름</returns>
		public static string CreateRegistryJobName(string hostName, string jobName)
			=> $"{IfsSync2Constants.REGISTRY_ROOT}{IfsSync2Constants.JOB_CONFIG_NAME}{hostName}\\{jobName}";

		/// <summary>
		/// 주소를 생성합니다.
		/// </summary>
		/// <param name="address">기본 주소</param>
		/// <param name="port">포트</param>
		/// <returns>형식화된 주소</returns>
		public static string CreateAddress(string address, string port)
			=> (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(port))
			   ? "" : $"https://{address}:{port}/api/v1/IfsSyncClients/";

		/// <summary>
		/// S3 파일 매니저 URL을 생성합니다.
		/// </summary>
		/// <param name="address">주소</param>
		/// <param name="port">포트 (옵션)</param>
		/// <returns>S3 파일 매니저 URL</returns>
		public static string CreateS3FileManagerURL(string address, int port = -1)
		{
			if (port <= 0) port = IfsSync2Constants.S3_FILE_MANAGER_DEFAULT_PORT;
			return string.Format("{0}:{1}", address, port);
		}

		/// <summary>
		/// 현재 시간을 문자열 형식으로 반환합니다.
		/// </summary>
		/// <returns>현재 시간 문자열</returns>
		public static string GetCurrentTime() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
		#endregion

		#region 해시 및 인증 관련 유틸리티
		/// <summary>
		/// 파일의 MD5 해시를 계산합니다.
		/// </summary>
		/// <param name="fileName">파일 이름</param>
		/// <returns>MD5 해시 문자열</returns>
		public static string CalculateMD5(string fileName)
		{
			try
			{
				// 긴 경로인 경우 MD5 계산을 건너뛰고 빈 문자열 반환
				if (fileName.Length > 320)
				{
					return string.Empty;
				}

				using var md5 = MD5.Create();
				using var stream = File.OpenRead(fileName);
				var hash = md5.ComputeHash(stream);
				return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
			}
			catch
			{
				return string.Empty;
			}
		}
		#endregion

		#region 시스템 정보 관련 유틸리티
		/// <summary>
		/// 애플리케이션 버전을 가져옵니다.
		/// </summary>
		/// <returns>버전 문자열</returns>
		public static string GetVersion()
		{
			try
			{
				Assembly assembly = Assembly.GetExecutingAssembly();
				Version? v = assembly.GetName().Version;

				if (v != null)
					return $"v{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
				else
					return "";
			}
			catch
			{
				return "";
			}
		}
		#endregion

		#region 파일 관리 관련 유틸리티
		/// <summary>
		/// 오래된 로그 파일을 삭제합니다.
		/// </summary>
		/// <param name="dirPath">디렉토리 경로</param>
		/// <param name="deleteDate">삭제 기준 일수</param>
		public static void DeleteOldLogs(string dirPath, int deleteDate = IfsSync2Constants.DEFAULT_DELETE_DATE)
		{
			var dirInfo = new DirectoryInfo(dirPath);
			if (!dirInfo.Exists) return;

			DateTime cmpTime = DateTime.Now.AddDays(-deleteDate);

			foreach (FileInfo file in dirInfo.GetFiles())
			{
				// 파일 생성 날짜가 기준일보다 오래되었으면 삭제
				if (file.CreationTime < cmpTime)
					file.Delete();
			}
		}
		#endregion
	}
}