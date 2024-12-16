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
using log4net;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using IfsSync2Data;
using System.Collections.Generic;
using System.Diagnostics;
using Amazon.S3;
using System.Net;

namespace IfsSync2Sender
{
	public class SenderThread
	{
		const string ERR_NETWORK_CONNECTION_FAILED = "The network connection has been lost.";

		static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		public int Delay { get; set; }

		readonly TaskDbManager TaskDB = null;
		readonly JobDbManager JobDB = null;

		public JobData Job { get; set; }
		readonly Thread _sender = null;
		readonly JobState State;

		InstantData Instant = null;
		/*************************AmazonS3*****************************/
		const string CONNECT_FAILURE = "ConnectFailure";
		S3Client Client { get; set; }
		readonly UserData User;
		/****************************ETC*******************************/
		public bool Login_OK { get; set; }
		public bool Quit { get; set; }
		public bool Stop { get; set; }
		public bool IsAlive { get; set; }
		public int FetchCount { get; set; }
		public readonly bool IsGlobal = false;

		public SenderThread(JobData jobData, UserData userData, int FetchCount, int DelayTime, bool IsGlobal)
		{
			Login_OK = false;
			Quit = false;
			Stop = false;
			Job = jobData;
			User = userData;
			Delay = DelayTime;
			this.FetchCount = FetchCount;
			this.IsGlobal = IsGlobal;
			TaskDB = new TaskDbManager(jobData.JobName);
			JobDB = new JobDbManager();
			State = new JobState(Job.HostName, Job.JobName, true);

			IsAlive = true;
			_sender = new Thread(() => Start());
			_sender.Start();
		}

		public bool Login()
		{
			const string LOGIN_FAIL_MESSAGE = "로그인실패! 네트워크 연결이 끊어졌거나 관리자의 접속허가가 필요할 수 있습니다.";
			try
			{
				if (User == null)
				{
					log.Fatal("User Data is not exist");
					End();
					return false;
				}
				Client = new S3Client(User);
				if (CheckConnect(Client))
				{
					log.Info($"Login Success : {Job.JobName}");
					if (State.Error) TaskDB.InsertLog("S3 Login Success!");
					Login_OK = true;
					State.Error = false;
				}
				else
				{
					log.Fatal($"Login fail : {User.URL}");
					Login_OK = false;
					if (!State.Error) TaskDB.InsertLog(LOGIN_FAIL_MESSAGE);
					State.Error = true;
					End();
				}
				return Login_OK;
			}
			catch (AmazonS3Exception e)
			{
				log.Fatal($"Login fail.", e);
				if (!State.Error) TaskDB.InsertLog(LOGIN_FAIL_MESSAGE);
				State.Error = true;
				return false;
			}
			catch (Exception e)
			{
				string Message;
				if (e.Message.Contains(CONNECT_FAILURE)) Message = ERR_NETWORK_CONNECTION_FAILED;
				else Message = "S3 Login Fail!";

				log.Fatal(Message, e);
				if (!State.Error) TaskDB.InsertLog(Message);

				State.Error = true;
				return false;
			}
		}

		public void Start()
		{
			log.Debug("Start");

			if (!Login())
			{
				End();
				return;
			}

			switch (Job.Policy)
			{
				case JobPolicyType.Now:
					{
						//Analysis and Backup
						InstantBackup();
						break;
					}
				case JobPolicyType.Schedule:
					{
						if (Job.IsInit)
						{
							Analysis();
							JobDB.UpdateIsInit(Job, false, IsGlobal);
						}
						while (!State.Quit)
						{
							if (!Job.CheckToSchedules()) break;

							if (!Stop) RunOnce();
							Thread.Sleep(Delay);
						}
						break;
					}
				case JobPolicyType.RealTime:
					{
						if (Job.IsInit)
						{
							Analysis();
							JobDB.UpdateIsInit(Job, false, IsGlobal);
						}
						while (!State.Quit)
						{
							if (!Stop) RealTimeRunOnce();
							Thread.Sleep(Delay);
						}
						break;
					}
			}
			End();
		}
		public void End()
		{
			IsAlive = false;
			Stop = true;
			Quit = true;
			State.Sender = false;
		}
		public void Close()
		{
			End();
			log.Debug("Close");
		}

		bool CheckNeedVSS()
		{
			if (TaskDB.Tasks.Count == 0) return false;
			if (Job.VSSFileExt.Count == 0) return false;

			foreach (TaskData Data in TaskDB.Tasks)
			{
				foreach (string Ext in Job.VSSFileExt)
				{
					if (Data.FilePath.EndsWith(Ext)) return true;
				}
			}

			return false;
		}

		public void RealTimeRunOnce()
		{
			State.Sender = true;
			string BucketName = User.UserName.ToLower().Replace("_", "-");
			List<ShadowCopy> ShadowCopyList = null;

			while (!State.Quit)
			{
				TaskDB.GetList(FetchCount);
				if (TaskDB.TaskCount == 0) break;
				State.RenameUpdate(TaskDB.TaskCount, TaskDB.TaskSize);

				if (CheckNeedVSS())
				{
					ShadowCopyList ??= GetShadowCopies();
				}

				foreach (TaskData item in TaskDB.Tasks)
				{
					if (State.Quit || Stop) { break; }

					//Get Snapshot Path
					if (State.VSS && item.TaskName == TaskNameList.Upload)
					{
						foreach (ShadowCopy Shadow in ShadowCopyList)
							if (item.FilePath.StartsWith(Shadow.VolumeName)) item.SnapshotPath = Shadow.GetSnapshotPath(item.FilePath);
					}

					if (!BackupStart(BucketName, item))
					{
						End();
						break;
					}
				}

				if (State.Quit || Stop) break;
				else Thread.Sleep(Delay);
			}
			ReleaseShadowCopies(ShadowCopyList);
			State.Sender = false;
		}
		void InstantRunOnce(List<ShadowCopy> ShadowCopyList = null)
		{
			State.Sender = true;
			string BucketName = User.UserName.ToLower().Replace("_", "-");

			while (!State.Quit)
			{
				TaskDB.GetList(FetchCount);
				if (TaskDB.TaskCount == 0) break;
				State.RenameUpdate(TaskDB.TaskCount, TaskDB.TaskSize);

				if (ShadowCopyList == null)
				{
					ShadowCopyList = GetShadowCopies();
				}

				foreach (TaskData item in TaskDB.Tasks)
				{
					if (State.Quit || Stop || !Job.CheckToSchedules()) { break; }

					//Get Snapshot Path Only Upload
					if (item.TaskName == TaskNameList.Upload)
					{
						foreach (ShadowCopy Shadow in ShadowCopyList)
						{
							if (item.FilePath.StartsWith(Shadow.VolumeName))
							{
								item.SnapshotPath = Shadow.GetSnapshotPath(item.FilePath);
								break;
							}
						}
					}

					if (!BackupStart(BucketName, item))
					{
						End();
						break;
					}
					Instant.Upload++;
					//몫
					var Percent = (double)Instant.Upload / (double)Instant.Total;
					var quotient = Math.Truncate(Percent);

					if (Instant.Percent < quotient)
					{
						Instant.Percent = (long)quotient;
						TaskDB.InsertLog(string.Format("Instant Backup : {0:0.##}%", Percent * 100));
					}
				}

				if (State.Quit || Stop) break;
				else Thread.Sleep(Delay);
			}
			ReleaseShadowCopies(ShadowCopyList);
			State.Sender = false;
		}
		void RunOnce(List<ShadowCopy> ShadowCopyList = null)
		{
			State.Sender = true;
			string BucketName = User.UserName.ToLower().Replace("_", "-");

			while (!State.Quit)
			{
				TaskDB.GetList(FetchCount);
				if (TaskDB.TaskCount == 0) break;
				State.RenameUpdate(TaskDB.TaskCount, TaskDB.TaskSize);

				if (ShadowCopyList == null) ShadowCopyList = GetShadowCopies();

				foreach (TaskData item in TaskDB.Tasks)
				{
					if (State.Quit || Stop || !Job.CheckToSchedules()) { break; }

					//Get Snapshot Path Only Upload
					if (item.TaskName == TaskNameList.Upload)
					{
						foreach (ShadowCopy Shadow in ShadowCopyList)
						{
							if (item.FilePath.StartsWith(Shadow.VolumeName))
							{
								item.SnapshotPath = Shadow.GetSnapshotPath(item.FilePath);
								break;
							}
						}
					}

					if (!BackupStart(BucketName, item))
					{
						End();
						break;
					}
				}

				if (State.Quit || Stop) break;
				else Thread.Sleep(Delay);
			}
			ReleaseShadowCopies(ShadowCopyList);
			State.Sender = false;
		}

		List<ShadowCopy> GetShadowCopies()
		{
			//find volume
			List<string> VolumeList = new List<string>();
			foreach (var Directory in Job.Path)
			{
				string root = Path.GetPathRoot(Directory);
				if (MainData.CheckUNCFolder(root)) continue;
				if (!VolumeList.Contains(root)) VolumeList.Add(root);
			}

			// NTFS and Local Disk
			var ShadowCopyList = new List<ShadowCopy>();
			foreach (string Item in VolumeList)
			{
				try
				{
					var drive = new DriveInfo(Item);
					if (drive.DriveFormat == "NTFS" && drive.DriveType == DriveType.Fixed)
					{
						var Shadow = new ShadowCopy();
						Shadow.Init();
						Shadow.Setup(Item);
						ShadowCopyList.Add(Shadow);
					}

					log.Debug($"Shadow Copy Directory : {Item}");
				}
				catch (Exception e) { log.Error(e); }
			}
			if (ShadowCopyList.Count > 0)
			{
				State.VSS = true;
				TaskDB.InsertLog("VSS Activation");
			}

			return ShadowCopyList;
		}
		void ReleaseShadowCopies(List<ShadowCopy> ShadowCopies)
		{
			if (ShadowCopies != null && ShadowCopies.Count > 0)
			{
				foreach (var Shadow in ShadowCopies) Shadow.Dispose();
				ShadowCopies.Clear();

				State.VSS = false;
				TaskDB.InsertLog("VSS Deactivation and deletion");
			}
		}

		bool BackupStart(string BucketName, TaskData task)
		{
			if (!ExistBucket(Client, BucketName))
			{
				log.Info($"Not exists Bucket : {BucketName}");
				if (PutBucket(Client, BucketName)) log.Info($"Create Bucket : {BucketName}");
				else
				{
					log.Error($"Create Fail Bucket {BucketName}");
					if (!State.Error)
					{
						log.Error($"The network connection has been lost. {BucketName}");
						TaskDB.InsertLog("The network connection has been lost.");
					}
					State.Error = true;
					return false;
				}
			}

			switch (task.TaskName)
			{
				case TaskNameList.Upload:
					{
						if (string.IsNullOrWhiteSpace(task.SnapshotPath)) task.SnapshotPath = task.FilePath;

						//md5 Check
						string S3MD5 = GetObjectMD5(Client, BucketName, task.FilePath);
						if (!string.IsNullOrWhiteSpace(S3MD5))
						{
							string MD5 = MainData.CalculateMD5(task.SnapshotPath);
							if (S3MD5.Equals(MD5, StringComparison.OrdinalIgnoreCase))
							{
								TaskDB.Delete(task);
								log.Debug($"Duplicate file : {task.FilePath}");
								return true;
							}
						}

						if (PutObject(Client, BucketName, task.FilePath, task.SnapshotPath, out string ErrorMsg)) task.UploadFlag = true;
						else
						{
							if (State.Error) return false;
							task.UploadFlag = false;
							task.Result = ErrorMsg;
						}
						task.UploadTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
						TaskDB.Update(task);
						break;
					}
				case TaskNameList.Rename:
					{
						if (RenameObject(Client, BucketName, task.FilePath, task.NewFilePath, out string ErrorMsg)) task.UploadFlag = true;
						else
						{
							if (State.Error) return false;
							task.UploadFlag = false;
							task.Result = ErrorMsg;
						}
						task.UploadTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
						TaskDB.Update(task);
						break;
					}
				case TaskNameList.Delete:
					{
						if (DeleteObject(Client, BucketName, task.FilePath, out string ErrorMsg)) task.UploadFlag = true;
						else
						{
							if (State.Error) return false;
							task.UploadFlag = false;
							task.Result = ErrorMsg;
						}

						task.UploadTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
						TaskDB.Update(task);
						break;
					}
			}

			if (task.UploadFlag) State.UploadSuccess(task.FileSize);
			else State.UploadFail();

			return true;
		}

		#region Amazon S3
		static string FileNameChangeToS3ObjectName(string DirectoryName)
		{
			return DirectoryName.Replace("\\", "/").Replace(":", "");
		}

		static bool CheckConnect(S3Client Client)
		{
			try
			{
				Client.ListBuckets();
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

		static bool PutBucket(S3Client Client, string bucketName)
		{
			try
			{
				if (Client.DoesS3BucketExist(bucketName)) return false;
				var Response = Client.PutBucket(bucketName);

				if (Response.HttpStatusCode == System.Net.HttpStatusCode.OK)
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
		bool PutObject(S3Client Client, string BucketName, string FilePath, string SnapshotPath, out string ErrorMsg)
		{
			try
			{
				if (string.IsNullOrEmpty(SnapshotPath)) SnapshotPath = FilePath;
				if (!File.Exists(SnapshotPath))
				{
					ErrorMsg = $"File is not exists : {SnapshotPath}";
					log.Error(ErrorMsg);
					return false;
				}

				string ObjectName = FileNameChangeToS3ObjectName(FilePath);

				if (new FileInfo(SnapshotPath).Length > MainData.UPLOAD_CHANGE_FILE_SIZE)
					Client.Upload(BucketName, ObjectName, SnapshotPath, PartSize: MainData.UPLOAD_PART_SIZE);
				else
					Client.PutObject(BucketName, ObjectName, FilePath: SnapshotPath);
				log.Debug($"BucketName : {BucketName} ObjectName : {ObjectName}");
				ErrorMsg = string.Empty;
				return true;
			}
			catch (AggregateException e)
			{
				ErrorMsg = $"DeleteObject Failed. S3://{BucketName}/{FilePath}. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}";
				log.Error(ErrorMsg);
				return false;
			}
			catch (AmazonS3Exception e)
			{
				ErrorMsg = $"DeleteObject Failed. S3://{BucketName}/{FilePath}. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}";
				log.Error(e);
				return false;
			}
			catch (Exception e)
			{
				ErrorMsg = e.Message;
				log.Error(e);
				if (e.Message.Contains(CONNECT_FAILURE))
				{
					string Message = ERR_NETWORK_CONNECTION_FAILED;
					log.Error(Message);
					if (!State.Error) TaskDB.InsertLog(Message);
					State.Error = true;
				}
				return false;
			}
		}
		bool RenameObject(S3Client client, string BucketName, string FilePath, string NewFilePath, out string ErrorMsg)
		{
			try
			{
				string ObjectName = FileNameChangeToS3ObjectName(FilePath);
				string NewObjectName = FileNameChangeToS3ObjectName(NewFilePath);

				client.CopyObject(BucketName, ObjectName, BucketName, NewObjectName);
				client.DeleteObject(BucketName, ObjectName);
				log.Debug($"BucketName : {BucketName} ObjectName : {ObjectName} : {NewObjectName}");
				ErrorMsg = string.Empty;
				return true;
			}
			catch (AggregateException e)
			{
				ErrorMsg = $"DeleteObject Failed. path : {FilePath}. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}";
				log.Error(ErrorMsg);
				return false;
			}
			catch (AmazonS3Exception e)
			{
				ErrorMsg = $"DeleteObject Failed. path : {FilePath}. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}";
				log.Error(e);
				return false;
			}
			catch (Exception e)
			{
				ErrorMsg = e.Message;
				log.Error(e);
				if (e.Message.Contains(CONNECT_FAILURE))
				{
					string Message = ERR_NETWORK_CONNECTION_FAILED;
					log.Error(Message);
					if (!State.Error) TaskDB.InsertLog(Message);
					State.Error = true;
				}
				return false;
			}
		}
		bool DeleteObject(S3Client client, string BucketName, string FilePath, out string ErrorMsg)
		{
			try
			{
				string ObjectName = FileNameChangeToS3ObjectName(FilePath);
				client.DeleteObject(BucketName, ObjectName);
				log.Debug($"BucketName : {BucketName} ObjectName : {ObjectName}");
				ErrorMsg = string.Empty;
				return true;
			}
			catch (AggregateException e)
			{
				ErrorMsg = $"DeleteObject Failed. path : {FilePath}. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}";
				log.Error(ErrorMsg);
				return false;
			}
			catch (AmazonS3Exception e)
			{
				ErrorMsg = $"DeleteObject Failed. path : {FilePath}. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}";
				log.Error(e);
				return false;
			}
			catch (Exception e)
			{
				ErrorMsg = e.Message;
				log.Error(e);
				if (e.Message.Contains(CONNECT_FAILURE))
				{
					string Message = ERR_NETWORK_CONNECTION_FAILED;
					log.Error(Message);
					if (!State.Error) TaskDB.InsertLog(Message);
					State.Error = true;
				}
				return false;
			}
		}

		static string GetObjectMD5(S3Client client, string BucketName, string FilePath)
		{
			try
			{
				string ObjectName = FileNameChangeToS3ObjectName(FilePath);

				var response = client.GetObjectMetadata(BucketName, ObjectName); //Get Bucket request

				string MD5Hash = response.ETag.Replace("\"", string.Empty);

				log.Debug($"HeadObject({BucketName}) {ObjectName} / {MD5Hash}");
				return MD5Hash;
			}
			catch (AggregateException e) { log.Error($"HeadObject failed. S3://{BucketName}/{FilePath}. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}"); return string.Empty; }
			catch (AmazonS3Exception e) { log.Error($"HeadObject failed. S3://{BucketName}/{FilePath}. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}"); return string.Empty; }
			catch (Exception e) { log.Error(e); return string.Empty; }
		}

		public static HttpStatusCode GetStatus(AggregateException e) => (e.InnerException is AmazonS3Exception e2) ? e2.StatusCode : HttpStatusCode.OK;
		public static string GetErrorCode(AggregateException e) => (e.InnerException is AmazonS3Exception e2) ? e2.ErrorCode : null;
		#endregion Amazon S3

		#region Analysis
		int Analysis()
		{
			log.Info("Start");
			var sw = new Stopwatch();
			sw.Start();
			if (Job.Policy == JobPolicyType.Now) State.Filter = true;
			TaskDB.Clear();

			//Analysis
			var ExtensionList = new List<string>(Job.WhiteFileExt);
			var DirectoryList = new List<string>(Job.Path);

			int FileCount = 0;
			//Directory Search
			foreach (var Directory in DirectoryList)
			{
				if (State.Quit) break;
				try { FileCount += SubDirectory(Directory, ExtensionList); } catch { }
			}

			if (Job.Policy == JobPolicyType.Now) State.Filter = false;
			sw.Stop();
			log.Debug($"End. Time : {sw.ElapsedMilliseconds.ToString()}ms / FileCount:{FileCount}");
			return FileCount;
		}

		int SubDirectory(string ParentDirectory, List<string> ExtensionList)
		{
			DirectoryInfo dInfoParent = new DirectoryInfo(ParentDirectory);

			if (!dInfoParent.Exists) return 0;

			int FileCount = 0;
			//add Folder List
			foreach (DirectoryInfo dInfo in dInfoParent.GetDirectories())
			{
				if (State.Quit) break;
				FileCount += SubDirectory(dInfo.FullName, ExtensionList);
			}
			FileCount += AddBackupFile(ParentDirectory, ExtensionList);

			return FileCount;
		}
		int AddBackupFile(string ParentDirectory, List<string> ExtensionList)
		{
			//add File List
			var files = Directory.GetFiles(ParentDirectory);
			var taskName = TaskNameList.Upload;

			int FileCount = 0;

			// 파일 나열
			foreach (string file in files)
			{
				if (State.Quit) break;
				var info = new FileInfo(file);
				if (!info.Exists) continue;

				if ((info.Attributes & FileAttributes.System) == FileAttributes.System) { /*empty*/ }
				else if (ExtensionList.Contains(info.Extension.Replace(".", "").ToLower()) && info.FullName.IndexOf('$') < 0)
				{
					var item = new TaskData(taskName, info.FullName, DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss"), info.Length);
					TaskDB.Insert(item);
					FileCount++;
				}
			}
			return FileCount;
		}
		#endregion Analysis

		void InstantBackup()
		{
			Instant = new InstantData();

			if (Job.SenderUpdate || !Instant.Analysis)
			{
				TaskDB.InsertLog("Instant Backup Start");
				log.Debug("Instant Backup Start");

				List<ShadowCopy> ShadowCopyList = GetShadowCopies();

				Instant.Analysis = false;
				Instant.Clear();
				State.UploadClear();

				Instant.Total = Analysis();
				TaskDB.InsertLog("Analysis File count = " + Instant.Total.ToString());
				Instant.Analysis = true;
				if (State.Quit)
				{
					TaskDB.Clear();
					log.Debug("Analysis Stop!");
					TaskDB.InsertLog("Analysis Stop!");
				}
				else if (Instant.Total != 0) InstantRunOnce(ShadowCopyList);
				else
				{
					log.Debug("Analysis Zero. No Backup");
					TaskDB.InsertLog("Analysis Zero.");
				}

				ReleaseShadowCopies(ShadowCopyList);

				State.RemainingCount = 0;
				State.RemainingSize = 0;
				Instant.Analysis = true;
				Instant.Total = 0;
				TaskDB.Clear();

				if (State.Quit || State.Error)
				{
					log.Debug("Backup Stop!");
					TaskDB.InsertLog("Backup Stop!");
				}
				else
				{
					log.Debug("Backup Finish!");
					TaskDB.InsertLog("Backup Finish!");
				}
				State.Quit = true;
			}
			else if (Instant.Total != 0)
			{
				InstantRunOnce();
			}
		}
	}
}
