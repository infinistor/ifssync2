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
using System;
using System.IO;
using System.Threading;
using IfsSync2Common;
using System.Collections.Generic;
using System.Diagnostics;
using Amazon.S3;
using System.Net;
using System.Linq;
using Amazon.S3.Model;

namespace IfsSync2Sender
{
	public class Sender
	{
		protected static readonly ILog log = LogManager.GetLogger(typeof(Sender));
		const string CONNECT_FAILURE = "ConnectFailure";
		const string DEFAULT_ERROR_MESSAGE = "네트워크 연결이 끊어졌습니다.";

		internal readonly TaskDbManager _taskManager;
		protected readonly JobDbManager _jobManager;
		protected readonly JobStatus _status;
		protected readonly string _bucketName;

		protected readonly ShadowCopyManager _shadowCopyManager;

		public JobData Job { get; protected set; }

		public int Delay { get; protected set; }
		public bool Pause { get; protected set; }

		protected Thread SenderThread;
		public bool IsAlive => SenderThread != null && SenderThread.IsAlive;
		public bool Quit => _status.Quit;

		protected UserData _user;
		protected S3Client _client;
		protected int _fetchCount;
		protected int _threadCount;
		protected long _multipartUploadFileSize;
		protected long _partSize;
		protected int _logRetention;

		public Sender(JobData jobData, UserData userData, int fetchCount, int delayTime, int threadCount, long multipartUploadFileSize, long multipartUploadPartSize, int logRetention)
		{
			Pause = false;
			Job = jobData;
			_bucketName = userData.UserName.ToLower().Replace("_", "-");
			_user = userData;
			_client = new S3Client(_user);
			Delay = delayTime;
			_fetchCount = fetchCount;
			_threadCount = threadCount;
			_multipartUploadFileSize = multipartUploadFileSize;
			_partSize = multipartUploadPartSize;
			_logRetention = logRetention;
			_taskManager = new TaskDbManager(jobData.JobName);
			_jobManager = new JobDbManager();
			_status = new JobStatus(Job.HostName, Job.JobName, true);

			_shadowCopyManager = new ShadowCopyManager();
		}

		#region Setup
		public void Run()
		{
			SenderThread = new Thread(Start);
			SenderThread.Start();
		}
		protected void End()
		{
			Pause = true;
			_status.Clear();
		}

		public void UpdateUser(UserData userData)
		{
			// 기존 작업 중지
			Pause = true;

			// 사용자 정보 업데이트
			_user = userData;
			_client = new S3Client(_user);

			// 작업 재개
			Pause = false;
		}
		public void UpdateJob(JobData jobData)
		{
			// 기존 작업 중지
			Pause = true;

			// 작업 정보 업데이트
			Job.CopyTo(jobData);

			// 작업 재개
			Pause = false;
		}
		public void Update(int fetchCount, int delayTime, int threadCount, long multipartUploadFileSize, long multipartUploadPartSize, int logRetention)
		{
			_fetchCount = fetchCount;
			Delay = delayTime;
			_threadCount = threadCount;
			_multipartUploadFileSize = multipartUploadFileSize;
			_partSize = multipartUploadPartSize;
			_logRetention = logRetention;
		}

		public void Close()
		{
			End();
			log.Debug("Close");
		}
		#endregion

		public virtual void Start()
		{
			log.Debug("Start");

			if (!Login())
			{
				End();
				return;
			}

			switch (Job.Policy)
			{
				case JobData.PolicyType.Now:
					{
						log.Debug("Now policy is not supported in base Sender class");
						break;
					}
				case JobData.PolicyType.Schedule:
					{
						if (Job.IsInit)
						{
							Analysis();
							_jobManager.UpdateIsInit(Job, false);
						}
						while (!_status.Quit)
						{
							if (!Pause && Job.CheckToSchedules()) RunOnce();
							if (_logRetention > 0) _taskManager.DeleteOldLogs(_logRetention);
							Thread.Sleep(Delay);
						}
						break;
					}
				case JobData.PolicyType.RealTime:
					{
						if (Job.IsInit)
						{
							Analysis();
							_jobManager.UpdateIsInit(Job, false);
						}
						while (!_status.Quit)
						{
							if (!Pause) RealTimeRunOnce();
							if (_logRetention > 0) _taskManager.DeleteOldLogs(_logRetention);
							Thread.Sleep(Delay);
						}
						break;
					}
			}
			End();
		}

		#region Login
		protected bool Login()
		{
			const string LOGIN_FAIL_MESSAGE = "로그인실패! 네트워크 연결이 끊어졌거나 관리자의 접속허가가 필요할 수 있습니다.";
			try
			{
				if (CheckConnect(_client))
				{
					log.Info($"Login Success : {Job.JobName}");
					if (_status.Error) _taskManager.InsertLog("S3 Login Success!");
					_status.Error = false;
					return true;
				}
				else
				{
					log.Fatal($"Login fail : {_user.URL}");
					if (!_status.Error) _taskManager.InsertLog(LOGIN_FAIL_MESSAGE);
					_status.Error = true;
					End();
					return false;
				}
			}
			catch (AmazonS3Exception e)
			{
				log.Fatal($"Login fail.", e);
				if (!_status.Error) _taskManager.InsertLog(LOGIN_FAIL_MESSAGE);
				_status.Error = true;
				return false;
			}
			catch (Exception e)
			{
				string Message;
				if (e.Message.Contains(CONNECT_FAILURE)) Message = DEFAULT_ERROR_MESSAGE;
				else Message = "S3 로그인 실패!";

				log.Fatal(Message, e);
				if (!_status.Error) _taskManager.InsertLog(Message);

				_status.Error = true;
				return false;
			}
		}
		#endregion

		#region Amazon S3
		static bool CheckConnect(S3Client client)
		{
			try
			{
				client.ListBuckets();
				return true;
			}
			catch (AggregateException e)
			{
				log.Error($"CheckConnect Failed. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}");
				return false;
			}
			catch (AmazonS3Exception e)
			{
				log.Error($"CheckConnect Failed. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}");
				return false;
			}
			catch (Exception e) { log.Error(e); return false; }
		}

		static bool ExistBucket(S3Client Client, string bucketName)
		{
			try
			{
				var isBucket = Client.DoesS3BucketExist(bucketName);
				log.Debug($"BucketName : {bucketName}, Result : {isBucket}");
				return isBucket;
			}
			catch (AggregateException e)
			{
				log.Error($"ExistBucket({bucketName}) Failed. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}");
				return false;
			}
			catch (AmazonS3Exception e)
			{
				log.Error($"ExistBucket({bucketName}) Failed. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}");
				return false;
			}
			catch (Exception e) { log.Error(e); return false; }
		}

		static bool ExistObject(S3Client Client, string bucketName, string objectName)
		{
			try
			{
				Client.GetObjectMetadata(bucketName, objectName);
				log.Debug($"ExistObject({bucketName}, {objectName}) : True");
				return true;
			}
			catch (AggregateException e)
			{
				log.Error($"ExistObject({bucketName}, {objectName}) Failed. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}");
				return false;
			}
			catch (AmazonS3Exception e)
			{
				if (e.StatusCode == HttpStatusCode.NotFound) return false;
				log.Error($"ExistObject({bucketName}, {objectName}) Failed. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}");
				return false;
			}
			catch (Exception e) { log.Error(e); return false; }

		}

		static bool PutBucket(S3Client client, string bucketName)
		{
			try
			{
				if (client.DoesS3BucketExist(bucketName)) return false;
				var response = client.PutBucket(bucketName);

				if (response.HttpStatusCode == HttpStatusCode.OK)
				{
					log.Info($"PutBucket({bucketName}) : Create!!");
					return true;
				}
				else log.Error($"PutBucket({bucketName}) : Create failed");
			}
			catch (AggregateException e)
			{
				log.Error($"PutBucket({bucketName}) Failed. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}");
				return false;
			}
			catch (AmazonS3Exception e)
			{
				log.Error($"PutBucket({bucketName}) Failed. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}");
				return false;
			}
			catch (Exception e) { log.Error(e); }
			return false;
		}

		static S3Metadata HeadObject(S3Client client, string bucketName, string filePath)
		{
			try
			{
				string objectName = FileNameChangeToS3ObjectName(filePath);

				var response = client.GetObjectMetadata(bucketName, objectName); //Get Bucket request

				string MD5Hash = response.ETag.Replace("\"", string.Empty);

				log.Debug($"HeadObject({bucketName}) {objectName} / {MD5Hash}");
				return new S3Metadata(objectName, response);
			}
			catch (AggregateException e)
			{
				var statusCode = GetStatus(e);
				var errorCode = GetErrorCode(e);
				if (statusCode == HttpStatusCode.NotFound) return null;
				log.Error($"HeadObject failed. S3://{bucketName}/{filePath}. StatusCode : {statusCode}, ErrorCode : {errorCode}");
				return null;
			}
			catch (AmazonS3Exception e)
			{
				if (e.StatusCode == HttpStatusCode.NotFound) return null;
				log.Error($"HeadObject failed. S3://{bucketName}/{filePath}. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}");
				return null;
			}
			catch (Exception e)
			{
				log.Error(e);
				return null;
			}
		}

		static string FileNameChangeToS3ObjectName(string directoryName) => directoryName.Replace("\\", "/").Replace(":", "");
		public static HttpStatusCode GetStatus(AggregateException e) => (e.InnerException is AmazonS3Exception e2) ? e2.StatusCode : HttpStatusCode.OK;
		public static string GetErrorCode(AggregateException e) => (e.InnerException is AmazonS3Exception e2) ? e2.ErrorCode : null;

		bool Upload(string filePath, string snapshotPath, out string errorMsg)
		{
			errorMsg = string.Empty;
			try
			{
				if (string.IsNullOrWhiteSpace(snapshotPath)) snapshotPath = filePath;
				string objectName = FileNameChangeToS3ObjectName(filePath);

				// 경로가 폴더일경우 빈 파일 업로드
				if (Directory.Exists(snapshotPath.TrimEnd('\\', '/')))
				{
					if (!PutObjectZeroSize(objectName, out errorMsg)) return false;
					log.Debug($"{Job.JobName} Empty file upload: {snapshotPath}");
					return true;
				}
				// 경로가 파일일경우 파일 업로드
				if (File.Exists(snapshotPath))
				{
					_client.Upload(_bucketName, objectName, snapshotPath, threadCount: _threadCount, minSizeBeforePartUpload: _multipartUploadFileSize, partSize: _partSize);
					log.Debug($"BucketName : {_bucketName} ObjectName : {objectName}");
					return true;
				}
				else
				{
					errorMsg = $"Path not found: {snapshotPath}";
					log.Debug($"{Job.JobName} Path not found: {snapshotPath}");
					return false;
				}


			}
			catch (AggregateException e)
			{
				errorMsg = $"DeleteObject Failed. S3://{_bucketName}/{filePath}. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}";
				log.Error(errorMsg);
				return false;
			}
			catch (AmazonS3Exception e)
			{
				errorMsg = $"DeleteObject Failed. S3://{_bucketName}/{filePath}. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}";
				log.Error(e);
				return false;
			}
			catch (Exception e)
			{
				errorMsg = e.Message;
				log.Error(e);
				if (e.Message.Contains(CONNECT_FAILURE))
				{
					log.Error(DEFAULT_ERROR_MESSAGE);
					if (!_status.Error) _taskManager.InsertLog(DEFAULT_ERROR_MESSAGE);
					_status.Error = true;
				}
				return false;
			}
		}

		bool PutObjectZeroSize(string objectName, out string errorMsg)
		{
			errorMsg = string.Empty;
			try
			{
				_client.PutObject(_bucketName, objectName);
				log.Debug($"BucketName : {_bucketName} ObjectName : {objectName}");
				return true;
			}
			catch (AggregateException e)
			{
				errorMsg = $"PutObject Failed. S3://{_bucketName}/{objectName}. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}";
				log.Error(errorMsg);
				return false;
			}
			catch (AmazonS3Exception e)
			{
				errorMsg = $"PutObject Failed. S3://{_bucketName}/{objectName}. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}";
				log.Error(e);
				return false;
			}
			catch (Exception e)
			{
				errorMsg = e.Message;
				log.Error(e);
				if (e.Message.Contains(CONNECT_FAILURE))
				{
					log.Error(DEFAULT_ERROR_MESSAGE);
					if (!_status.Error) _taskManager.InsertLog(DEFAULT_ERROR_MESSAGE);
					_status.Error = true;
				}
				return false;
			}
		}

		bool RenameObject(string filePath, string newFilePath, out string errorMsg)
		{
			try
			{
				string objectName = FileNameChangeToS3ObjectName(filePath);
				string newObjectName = FileNameChangeToS3ObjectName(newFilePath);

				// 원본 파일이 존재하는지 확인
				if (!ExistObject(_client, _bucketName, objectName))
				{
					//원본 파일이 존재 하지 않는다면 원본 파일을 변경할 파일명으로 업로드
					log.Debug($"RenameObject({filePath}, {newFilePath}) : {objectName} is not exists. change to {newFilePath}");
					return Upload(newFilePath, "", out errorMsg);
				}
				// 원본 파일이 존재하는 경우 복사 후 삭제
				else
				{
					_client.CopyObject(_bucketName, objectName, _bucketName, newObjectName);
					_client.DeleteObject(_bucketName, objectName);
					log.Debug($"BucketName : {_bucketName} ObjectName : {objectName} : {newObjectName}");
				}
				errorMsg = string.Empty;
				return true;
			}
			catch (AggregateException e)
			{
				errorMsg = $"DeleteObject Failed. path : {filePath}. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}";
				log.Error(errorMsg);
				return false;
			}
			catch (AmazonS3Exception e)
			{
				errorMsg = $"DeleteObject Failed. path : {filePath}. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}";
				log.Error(e);
				return false;
			}
			catch (Exception e)
			{
				errorMsg = e.Message;
				log.Error(e);
				if (e.Message.Contains(CONNECT_FAILURE))
				{
					log.Error(DEFAULT_ERROR_MESSAGE);
					if (!_status.Error) _taskManager.InsertLog(DEFAULT_ERROR_MESSAGE);
					_status.Error = true;
				}
				return false;
			}
		}

		bool DeleteObjects(string folderPath, out string errorMsg)
		{
			try
			{
				// s3 모든 파일 삭제
				var prefix = FileNameChangeToS3ObjectName(folderPath);
				var nextMarker = string.Empty;
				while (true)
				{
					var response = _client.ListObjects(_bucketName, prefix: prefix, marker: nextMarker);
					var deleteList = response.S3Objects.Select(x => new KeyVersion() { Key = x.Key }).ToList();
					_client.DeleteObjects(new DeleteObjectsRequest() { BucketName = _bucketName, Objects = deleteList });

					if (response.IsTruncated ?? false) nextMarker = response.NextMarker;
					else break;
				}

				errorMsg = string.Empty;
				return true;
			}
			catch (AggregateException e)
			{
				errorMsg = $"DeleteObjects Failed. path : {folderPath}. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}";
				log.Error(errorMsg);
				return false;
			}
			catch (AmazonS3Exception e)
			{
				errorMsg = $"DeleteObjects Failed. path : {folderPath}. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}";
				log.Error(e);
				return false;
			}
			catch (Exception e)
			{
				errorMsg = e.Message;
				log.Error(e);
				if (e.Message.Contains(CONNECT_FAILURE))
				{
					log.Error(DEFAULT_ERROR_MESSAGE);
					if (!_status.Error) _taskManager.InsertLog(DEFAULT_ERROR_MESSAGE);
					_status.Error = true;
				}
				return false;
			}
		}
		bool DeleteObject(string filePath, out string errorMsg)
		{
			try
			{
				string objectName = FileNameChangeToS3ObjectName(filePath);
				_client.DeleteObject(_bucketName, objectName);
				log.Debug($"BucketName : {_bucketName} ObjectName : {objectName}");
				errorMsg = string.Empty;
				return true;
			}
			catch (AggregateException e)
			{
				errorMsg = $"DeleteObject Failed. path : {filePath}. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}";
				log.Error(errorMsg);
				return false;
			}
			catch (AmazonS3Exception e)
			{
				errorMsg = $"DeleteObject Failed. path : {filePath}. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}";
				log.Error(e);
				return false;
			}
			catch (Exception e)
			{
				errorMsg = e.Message;
				log.Error(e);
				if (e.Message.Contains(CONNECT_FAILURE))
				{
					log.Error(DEFAULT_ERROR_MESSAGE);
					if (!_status.Error) _taskManager.InsertLog(DEFAULT_ERROR_MESSAGE);
					_status.Error = true;
				}
				return false;
			}
		}
		#endregion

		#region ShadowCopy
		#endregion

		#region Analysis
		protected int Analysis()
		{
			log.Info("Start");
			var sw = new Stopwatch();
			sw.Start();
			if (Job.Policy == JobData.PolicyType.Now) _status.Filter = true;
			_taskManager.Clear();

			try
			{
				//Analysis
				var extensionList = new List<string>(Job.WhiteFileExt);
				var directoryList = new List<string>(Job.Path);

				if (directoryList?.Count == 0)
				{
					log.Error("디렉토리 목록이 비어있습니다.");
					return 0;
				}

				int FileCount = 0;
				//Directory Search
				foreach (var directory in directoryList)
				{
					if (_status.Quit) break;
					if (string.IsNullOrWhiteSpace(directory))
					{
						log.Warn("빈 디렉토리 경로가 감지되었습니다.");
						continue;
					}

					try
					{
						// 각 디렉토리에서 TaskData 객체 목록 가져오기
						var taskList = MainUtility.GetFilesWithTaskData(
							directory,
							extensionList,
							EnumTaskType.Upload);

						log.Debug($"Directory {directory}: {taskList.Count} files found");

						// 모든 TaskData를 작업 관리자에 추가
						foreach (var task in taskList)
						{
							if (_status.Quit) break;
							_taskManager.Insert(task);
						}

						FileCount += taskList.Count;
					}
					catch (UnauthorizedAccessException ex)
					{
						log.Error($"디렉토리 접근 권한이 없습니다: {directory}", ex);
					}
					catch (DirectoryNotFoundException ex)
					{
						log.Error($"디렉토리를 찾을 수 없습니다: {directory}", ex);
					}
					catch (Exception ex)
					{
						log.Error($"디렉토리 처리 중 오류 발생: {directory}", ex);
					}
				}

				if (Job.Policy == JobData.PolicyType.Now) _status.Filter = false;
				sw.Stop();
				log.Debug($"End. Time : {sw.ElapsedMilliseconds}ms / FileCount:{FileCount}");
				return FileCount;
			}
			catch (Exception ex)
			{
				log.Error("Analysis 처리 중 예기치 않은 오류가 발생했습니다.", ex);
				return 0;
			}
		}
		#endregion Analysis

		/// <summary>
		/// 백업 실행 공통 로직
		/// </summary>
		/// <param name="shadowCopyList">스냅샷 목록</param>
		/// <param name="processTask">작업 처리 후 실행할 콜백</param>
		protected void ExecuteBackup(List<ShadowCopy> shadowCopyList, Action<TaskData> processTask = null)
		{
			log.Debug($"{Job.JobName} ExecuteBackup");
			_status.Sender = true;

			while (!_status.Quit)
			{
				_taskManager.GetList(_fetchCount);
				log.Debug($"{Job.JobName} TaskCount: {_taskManager.TaskCount}");

				if (_taskManager.TaskCount == 0) break;
				_status.RenameUpdate(_taskManager.TaskCount, _taskManager.TaskSize);

				// VSS 필요 여부 확인 후 스냅샷 생성
				bool needVSS = _shadowCopyManager.CheckNeedVSS(_taskManager.TaskList, Job.VSSFileExt);
				if (needVSS)
				{
					shadowCopyList ??= _shadowCopyManager.CreateShadowCopies(Job.Path);
					if (shadowCopyList.Count > 0) _status.VSS = true;
					log.Debug($"{Job.JobName} VSS: {_status.VSS}");
				}

				foreach (TaskData item in _taskManager.TaskList)
				{
					if (_status.Quit || Pause || !Job.CheckToSchedules()) { break; }

					// 스냅샷 경로 적용
					if (_status.VSS)
					{
						_shadowCopyManager.ApplySnapshotPathToTask(item, shadowCopyList);
						log.Debug($"{Job.JobName} ApplySnapshotPathToTask: {item.FilePath}");
					}

					if (!Backup(item))
					{
						End();
						break;
					}

					// 작업 처리 후 콜백 실행
					processTask?.Invoke(item);
				}

				if (_status.Quit || Pause) break;
				else Thread.Sleep(Delay);
			}

			// 스냅샷 해제
			_shadowCopyManager.ReleaseShadowCopies(shadowCopyList);
			if (shadowCopyList != null && shadowCopyList.Count > 0) _status.VSS = false;
			_status.Sender = false;
		}

		void RunOnce(List<ShadowCopy> shadowCopyList = null)
		{
			ExecuteBackup(shadowCopyList);
		}

		public void RealTimeRunOnce()
		{
			ExecuteBackup(null);
		}

		protected bool Backup(TaskData task)
		{
			log.Debug($"{Job.JobName} Backup {task.FilePath}");
			if (!ExistBucket(_client, _bucketName))
			{
				log.Info($"{Job.JobName} Not exists Bucket : {_bucketName}");
				if (PutBucket(_client, _bucketName)) log.Info($"Create Bucket : {_bucketName}");
				else
				{
					log.Error($"{Job.JobName} Create Fail Bucket {_bucketName}");
					if (!_status.Error)
					{
						log.Error($"{DEFAULT_ERROR_MESSAGE} {_bucketName}");
						_taskManager.InsertLog(DEFAULT_ERROR_MESSAGE);
					}
					_status.Error = true;
					return false;
				}
			}

			switch (task.TaskType)
			{
				case EnumTaskType.Upload:
					{
						string uploadPath = string.IsNullOrWhiteSpace(task.SnapshotPath) ? task.FilePath : task.SnapshotPath;

						//File Check
						var meta = HeadObject(_client, _bucketName, task.FilePath);

						// 중복 파일 처리
						if (meta != null)
						{
							// checksum 이 있을 경우 비교
							if (meta.ChecksumAlgorithm != S3ChecksumAlgorithm.None)
							{
								try
								{
									var checksum = ChecksumCalculator.CalculateChecksum(uploadPath, meta.ChecksumAlgorithm);
									if (checksum.Equals(meta.Checksum, StringComparison.OrdinalIgnoreCase))
									{
										log.Debug($"{Job.JobName} Duplicate file : {uploadPath}");
										_taskManager.Delete(task);
										return true;
									}
								}
								catch (Exception ex)
								{
									log.Error($"{Job.JobName} Checksum 계산 중 오류 발생: {uploadPath}", ex);
								}
							}
							// checksum 이 없을 경우 MD5Sum에 -가 없을 경우 비교
							else if (!string.IsNullOrWhiteSpace(meta.MD5) && !meta.MD5.Contains('-'))
							{
								var md5sum = IfsSync2Utilities.CalculateMD5(uploadPath);
								if (meta.MD5.Equals(md5sum, StringComparison.OrdinalIgnoreCase))
								{
									log.Debug($"{Job.JobName} Duplicate file : {uploadPath}");
									_taskManager.Delete(task);
									return true;
								}
								else
								{
									log.Debug($"{Job.JobName} {uploadPath} mismatch : {meta.MD5} != {md5sum}");
								}
							}
						}

						if (Upload(task.FilePath, uploadPath, out string ErrorMsg)) task.UploadFlag = true;
						else
						{
							if (_status.Error) return false;
							task.UploadFlag = false;
							task.Result = ErrorMsg;
						}
						task.UploadTime = IfsSync2Utilities.GetCurrentTime();
						_taskManager.Update(task);
						break;
					}
				case EnumTaskType.Rename:
					{
						if (RenameObject(task.FilePath, task.NewFilePath, out string ErrorMsg)) task.UploadFlag = true;
						else
						{
							if (_status.Error) return false;
							task.UploadFlag = false;
							task.Result = ErrorMsg;
						}
						task.UploadTime = IfsSync2Utilities.GetCurrentTime();
						_taskManager.Update(task);
						break;
					}
				case EnumTaskType.Delete:
					{
						// 폴더일 경우 폴더 내 모든 파일 삭제
						if (task.FilePath.EndsWith('\\'))
						{
							log.Debug($"{Job.JobName} Delete Folder: {task.FilePath}");
							if (DeleteObjects(task.FilePath, out string ErrorMsg)) task.UploadFlag = true;
							else
							{
								if (_status.Error) return false;
								task.UploadFlag = false;
								task.Result = ErrorMsg;
							}
						}
						else
						{
							log.Debug($"{Job.JobName} Delete File: {task.FilePath}");
							if (DeleteObject(task.FilePath, out string ErrorMsg)) task.UploadFlag = true;
							else
							{
								if (_status.Error) return false;
								task.UploadFlag = false;
								task.Result = ErrorMsg;
							}
						}

						task.UploadTime = IfsSync2Utilities.GetCurrentTime();
						_taskManager.Update(task);
						break;
					}
			}

			if (task.UploadFlag) _status.UploadSuccess(task.FileSize);
			else _status.UploadFail();

			return true;
		}
	}
}

