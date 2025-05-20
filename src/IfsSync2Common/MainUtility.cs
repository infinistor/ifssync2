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
using log4net;
using System.IO;

namespace IfsSync2Common
{
	/// <summary> 유틸리티 </summary>
	public static class MainUtility
	{
		static readonly ILog _log = LogManager.GetLogger(typeof(MainUtility));
		public const int DEFAULT_DELETE_DATE = 30;
		//파일삭제 함수
		public static void DeleteOldLogs(string dirPath, int deleteDate = DEFAULT_DELETE_DATE)
		{
			try
			{
				DirectoryInfo dirInfo = new(dirPath);
				if (!dirInfo.Exists) return;

				// 현재 날짜에서 deleteDate일 전의 날짜를 계산
				var cutoffDate = DateTime.Now.AddDays(-deleteDate);

				foreach (var file in dirInfo.GetFiles())
				{
					// 파일 생성 시간이 기준 날짜보다 이전(오래된)이면 삭제
					if (file.CreationTime < cutoffDate)
						File.Delete(file.FullName);
				}
			}
			catch (Exception ex)
			{
				_log.Error(ex.Message);
			}
		}

		/// <summary>
		/// 큐 기반 디렉토리 탐색으로 지정된 확장자와 일치하는 파일 목록을 반환합니다.
		/// </summary>
		/// <typeparam name="T">확장자 목록의 컬렉션 타입</typeparam>
		/// <param name="directoryPath">탐색할 디렉토리 경로</param>
		/// <param name="extensionList">허용할 확장자 목록 (컬렉션 형태)</param>
		/// <returns>조건에 맞는 파일의 경로 목록</returns>
		public static List<string> GetFilesInDirectory<T>(string directoryPath, T extensionList) where T : IEnumerable<string>
		{
			var fileList = new List<string>();

			if (string.IsNullOrWhiteSpace(directoryPath))
			{
				_log.Error("디렉토리 경로가 null 또는 비어 있습니다.");
				return fileList;
			}

			// 디렉토리 존재 확인
			if (!Directory.Exists(directoryPath))
			{
				_log.Warn($"디렉토리가 존재하지 않습니다: {directoryPath}");
				return fileList;
			}

			// 큐를 사용한 너비 우선 탐색(BFS) 구현
			var directoryQueue = new Queue<string>();
			directoryQueue.Enqueue(directoryPath);

			// "ALL" 확장자 옵션 확인
			bool isAllExtension = extensionList.Contains("ALL");

			while (directoryQueue.Count > 0)
			{
				string currentDirectory = directoryQueue.Dequeue();

				try
				{
					// 현재 디렉토리의 파일 처리
					string[] files;
					try
					{
						files = Directory.GetFiles(currentDirectory);
					}
					catch (UnauthorizedAccessException ex)
					{
						_log.Error($"디렉토리 접근 권한이 없습니다: {currentDirectory}", ex);
						continue;
					}
					catch (DirectoryNotFoundException ex)
					{
						_log.Error($"디렉토리를 찾을 수 없습니다: {currentDirectory}", ex);
						continue;
					}
					catch (Exception ex)
					{
						_log.Error($"파일 목록 조회 중 오류 발생: {currentDirectory}", ex);
						continue;
					}

					// 파일 필터링 처리
					foreach (string file in files)
					{
						try
						{
							var fileInfo = new FileInfo(file);

							// 시스템, 숨김 파일 제외
							if ((fileInfo.Attributes & FileAttributes.System) == FileAttributes.System ||
								(fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
							{
								continue;
							}

							// 임시 파일 제외 ($로 시작하는 파일 등)
							if (IsSpecialFile(fileInfo.Name))
							{
								continue;
							}

							// 확장자 확인 (ALL이거나 목록에 있는 확장자만 포함)
							string extension = fileInfo.Extension.Replace(".", "").ToLower();
							if (isAllExtension || extensionList.Contains(extension))
							{
								fileList.Add(fileInfo.FullName);
							}
						}
						catch (Exception ex)
						{
							_log.Error($"파일 처리 중 오류 발생: {file}", ex);
						}
					}

					// 하위 디렉토리 큐에 추가
					try
					{
						foreach (string subDir in Directory.GetDirectories(currentDirectory))
						{
							var dirInfo = new DirectoryInfo(subDir);

							// 시스템 또는 숨김 폴더 제외
							if ((dirInfo.Attributes & FileAttributes.System) == FileAttributes.System ||
								(dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
							{
								continue;
							}

							directoryQueue.Enqueue(subDir);
						}
					}
					catch (UnauthorizedAccessException ex)
					{
						_log.Error($"하위 디렉토리 접근 권한이 없습니다: {currentDirectory}", ex);
					}
					catch (Exception ex)
					{
						_log.Error($"하위 디렉토리 처리 중 오류 발생: {currentDirectory}", ex);
					}
				}
				catch (Exception ex)
				{
					_log.Error($"디렉토리 처리 중 오류 발생: {currentDirectory}", ex);
				}
			}

			return fileList;
		}

		/// <summary>
		/// 큐 기반 디렉토리 탐색으로 지정된 확장자와 일치하는 파일과 모든 폴더를 찾아 TaskData 객체 목록으로 반환합니다.
		/// </summary>
		/// <typeparam name="T">확장자 목록의 컬렉션 타입</typeparam>
		/// <param name="directoryPath">탐색할 디렉토리 경로</param>
		/// <param name="extensionList">허용할 확장자 목록 (컬렉션 형태)</param>
		/// <param name="taskType">생성할 작업 유형 (기본값: Upload)</param>
		/// <returns>조건에 맞는 파일들과 모든 폴더의 TaskData 객체 목록</returns>
		public static List<TaskData> GetFilesWithTaskData<T>(
			string directoryPath,
			T extensionList,
			TaskData.TaskTypeList taskType = TaskData.TaskTypeList.Upload) where T : IEnumerable<string>
		{
			var taskList = new List<TaskData>();

			_log.Debug($"GetFilesWithTaskData 시작: 경로={directoryPath}, 작업유형={taskType}");

			if (string.IsNullOrWhiteSpace(directoryPath))
			{
				_log.Error("디렉토리 경로가 null 또는 비어 있습니다.");
				return taskList;
			}

			// 디렉토리 존재 확인
			if (!Directory.Exists(directoryPath))
			{
				_log.Warn($"디렉토리가 존재하지 않습니다: {directoryPath}");
				return taskList;
			}

			// 큐를 사용한 너비 우선 탐색(BFS) 구현
			var directoryQueue = new Queue<string>();
			directoryQueue.Enqueue(directoryPath);

			// "ALL" 확장자 옵션 확인
			bool isAllExtension = extensionList.Contains("ALL");
			_log.Debug($"ALL 확장자 옵션: {isAllExtension}");

			while (directoryQueue.Count > 0)
			{
				string currentDirectory = directoryQueue.Dequeue();
				_log.Debug($"현재 처리 중인 디렉토리: {currentDirectory}");

				try
				{
					// 현재 디렉토리 자체를 TaskData에 추가
					try
					{
						var dirInfo = new DirectoryInfo(currentDirectory);

						// 시스템 또는 숨김 폴더가 아닌 경우에만 추가
						if ((dirInfo.Attributes & FileAttributes.System) != FileAttributes.System &&
							(dirInfo.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
						{
							// 폴더 자체를 TaskData에 추가
							var folderTask = new TaskData(
								taskType,
								EnsureDirectoryEndsWithSeparator(dirInfo.FullName),
								GetCurrentTime(),
								0); // 폴더의 경우 크기는 0으로 설정

							taskList.Add(folderTask);
							_log.Debug($"폴더가 TaskData에 추가됨: {dirInfo.FullName}, 현재 항목 수: {taskList.Count}");
						}
						else
						{
							_log.Debug($"시스템 또는 숨김 폴더로 제외됨: {dirInfo.FullName}");
						}
					}
					catch (Exception ex)
					{
						_log.Error($"폴더 처리 중 오류 발생: {currentDirectory}", ex);
					}

					// 현재 디렉토리의 파일 처리
					try
					{
						var files = new DirectoryInfo(currentDirectory).GetFiles();
						_log.Debug($"디렉토리 {currentDirectory}의 파일 수: {files.Length}");

						foreach (var fileInfo in files)
						{
							try
							{
								// 시스템, 숨김 파일 제외
								if ((fileInfo.Attributes & FileAttributes.System) == FileAttributes.System ||
									(fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
								{
									_log.Debug($"시스템 또는 숨김 파일로 제외됨: {fileInfo.FullName}");
									continue;
								}

								// 임시 파일 제외 ($로 시작하는 파일 등)
								if (IsSpecialFile(fileInfo.Name))
								{
									_log.Debug($"특수 파일로 제외됨: {fileInfo.FullName}");
									continue;
								}

								// 확장자 확인 (ALL이거나 목록에 있는 확장자만 포함)
								string extension = fileInfo.Extension.Replace(".", "").ToLower();
								if (isAllExtension || extensionList.Contains(extension))
								{
									// TaskData 객체 생성 및 추가
									var fileTask = new TaskData(
										taskType,
										fileInfo.FullName,
										GetCurrentTime(),
										fileInfo.Length);

									taskList.Add(fileTask);
									_log.Debug($"파일이 TaskData에 추가됨: {fileInfo.FullName}, 현재 항목 수: {taskList.Count}");
								}
								else
								{
									_log.Debug($"확장자 불일치로 제외됨: {fileInfo.FullName}, 확장자: {extension}");
								}
							}
							catch (Exception ex)
							{
								_log.Error($"파일 처리 중 오류 발생: {fileInfo.FullName}", ex);
							}
						}
					}
					catch (UnauthorizedAccessException ex)
					{
						_log.Error($"디렉토리 접근 권한이 없습니다: {currentDirectory}", ex);
					}
					catch (DirectoryNotFoundException ex)
					{
						_log.Error($"디렉토리를 찾을 수 없습니다: {currentDirectory}", ex);
					}
					catch (Exception ex)
					{
						_log.Error($"파일 목록 조회 중 오류 발생: {currentDirectory}", ex);
					}

					// 하위 디렉토리 큐에 추가
					try
					{
						var subDirs = new DirectoryInfo(currentDirectory).GetDirectories();
						_log.Debug($"디렉토리 {currentDirectory}의 하위 디렉토리 수: {subDirs.Length}");

						foreach (var dirInfo in subDirs)
						{
							// 시스템 또는 숨김 폴더 제외
							if ((dirInfo.Attributes & FileAttributes.System) == FileAttributes.System ||
								(dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
							{
								_log.Debug($"시스템 또는 숨김 하위 폴더로 제외됨: {dirInfo.FullName}");
								continue;
							}

							directoryQueue.Enqueue(dirInfo.FullName);
							_log.Debug($"하위 폴더가 큐에 추가됨: {dirInfo.FullName}");
						}
					}
					catch (UnauthorizedAccessException ex)
					{
						_log.Error($"하위 디렉토리 접근 권한이 없습니다: {currentDirectory}", ex);
					}
					catch (Exception ex)
					{
						_log.Error($"하위 디렉토리 처리 중 오류 발생: {currentDirectory}", ex);
					}
				}
				catch (Exception ex)
				{
					_log.Error($"디렉토리 처리 중 오류 발생: {currentDirectory}", ex);
				}
			}

			_log.Debug($"GetFilesWithTaskData 완료: 경로={directoryPath}, 작업유형={taskType}, 총 항목 수={taskList.Count}");
			if (taskList.Count > 0)
			{
				_log.Debug($"첫 번째 항목: 경로={taskList[0].FilePath}, 유형={taskList[0].TaskType}");
			}
			else
			{
				_log.Warn($"GetFilesWithTaskData 작업 결과가 비어 있음: 경로={directoryPath}, 작업유형={taskType}");
			}

			return taskList;
		}

		/// <summary>
		/// 현재 시간을 표준 형식으로 반환합니다.
		/// </summary>
		private static string GetCurrentTime()
		{
			return DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss");
		}

		/// <summary>
		/// 특수 파일(임시 파일 등)인지 여부를 확인합니다.
		/// </summary>
		/// <param name="fileName">파일 이름</param>
		/// <returns>특수 파일이면 true, 아니면 false</returns>
		private static bool IsSpecialFile(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				return true;

			// 임시 파일 확인
			if (fileName.StartsWith('~'))
				return true;

			// 확장자가 tmp인 파일
			if (fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
				return true;

			// $ 포함 파일
			if (fileName.Contains('$'))
				return true;

			return false;
		}

		/// <summary>
		/// 디렉토리 경로가 구분자로 끝나도록 보장합니다.
		/// </summary>
		/// <param name="directoryPath">디렉토리 경로</param>
		/// <returns>끝에 구분자가 있는 디렉토리 경로</returns>
		public static string EnsureDirectoryEndsWithSeparator(string directoryPath)
		{
			if (string.IsNullOrEmpty(directoryPath))
				return directoryPath;

			// 이미 구분자로 끝나는 경우 그대로 반환
			if (directoryPath.EndsWith(Path.DirectorySeparatorChar.ToString()) || 
				directoryPath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
				return directoryPath;

			// 시스템 기본 구분자 추가
			return directoryPath + Path.DirectorySeparatorChar;
		}
	}
}
