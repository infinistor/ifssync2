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
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Threading;

namespace IfsSync2Data
{
	public class JobDbManager
	{
		#region Define
		/******************** Global Job List Attribute *************************/
		const string SQLITE_SEQUENCE = "sqlite_sequence";
		const string SQLITE_SEQUENCE_SEQ = "seq";

		/******************** Job List Attribute ********************************/
		const string STR_JOB_TABLE_NAME = "JobList";
		const string STR_JOB_ID = "Id";
		const string STR_JOB_HOSTNAME = "HostName";
		const string STR_JOB_NAME = "JobName";
		const string STR_JOB_GLOBAL = "Global";
		const string STR_JOB_IS_GLOBAL_USER = "IsGlobalUser";
		const string STR_JOB_USER_ID = "UserID";
		const string STR_JOB_POLICY_NAME = "PolicyName";
		const string STR_JOB_PATH = "Path";
		const string STR_JOB_BLACK_PATH = "BlackPath";
		const string STR_JOB_BLACK_FILE = "BlackFile";
		const string STR_JOB_BLACK_FILE_EXT = "BlackFileExt";
		const string STR_JOB_WHITE_FILE = "WhiteFile";
		const string STR_JOB_WHITE_FILE_EXT = "WhiteFileExt";
		const string STR_JOB_VSS_FILE_EXT = "VSSFileExt";
		const string STR_JOB_REMOVE = "Remove";
		const string STR_JOB_IS_INIT = "IsInit";
		const string STR_JOB_FILTER_UPDATE = "FilterUpdate";
		const string STR_JOB_SENDER_UPDATE = "SenderUpdate";
		/***************** Schedule List Attribute ******************************/
		const string STR_SCHEDULE_TABLE_NAME = "ScheduleList";
		const string STR_SCHEDULE_ID = "Id";
		const string STR_SCHEDULE_JOB_ID = "JobID";
		const string STR_SCHEDULE_WEEKS = "Weeks";
		const string STR_SCHEDULE_AT_TIME = "AtTime";
		const string STR_SCHEDULE_FOR_HOURS = "ForHours";
		#endregion

		readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		readonly string _filePath = MainData.GetDBFilePath(MainData.JOB_DB_FILE_NAME);
		readonly Mutex _mutex;

		public JobDbManager()
		{
			try
			{
				_mutex = new Mutex(false, MainData.MUTEX_NAME_JOB_SQL, out bool CreatedNew);

				if (!CreatedNew) _log.Debug($"Mutex({MainData.MUTEX_NAME_JOB_SQL})");
				else _log.Debug($"Mutex({MainData.MUTEX_NAME_JOB_SQL}) create");
			}
			catch (Exception e)
			{
				_log.Error($"Mutex({MainData.MUTEX_NAME_JOB_SQL}) fail", e);
			}
		}

		bool CreateDBFile()
		{
			try
			{
				MainData.CreateFile(_filePath);

				_mutex.WaitOne();
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{
					CommandText =
					//JobList
					$"Create Table IF NOT EXISTS '{STR_JOB_TABLE_NAME}'(" +
								 $"'{STR_JOB_ID}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT," +
								 $"'{STR_JOB_HOSTNAME}' TEXT, " +
								 $"'{STR_JOB_NAME}' TEXT NOT NULL, " +
								 $"'{STR_JOB_GLOBAL}' BOOL NOT NULL DEFAULT FALSE, " +
								 $"'{STR_JOB_IS_GLOBAL_USER}' BOOL NOT NULL, " +
								 $"'{STR_JOB_USER_ID}' INTEGER NOT NULL, " +
								 $"'{STR_JOB_POLICY_NAME}' TEXT NOT NULL, " +
								 $"'{STR_JOB_PATH}' TEXT NULL, " +
								 $"'{STR_JOB_BLACK_PATH}' TEXT NULL, " +
								 $"'{STR_JOB_BLACK_FILE}' TEXT NULL, " +
								 $"'{STR_JOB_BLACK_FILE_EXT}' TEXT NULL, " +
								 $"'{STR_JOB_WHITE_FILE}' TEXT NOT NULL, " +
								 $"'{STR_JOB_WHITE_FILE_EXT}' TEXT NOT NULL, " +
								 $"'{STR_JOB_VSS_FILE_EXT}' TEXT NULL, " +
								 $"'{STR_JOB_REMOVE}' BOOL NOT NULL DEFAULT TRUE," +
								 $"'{STR_JOB_IS_INIT}' BOOL NOT NULL DEFAULT TRUE," +
								 $"'{STR_JOB_FILTER_UPDATE}' BOOL NOT NULL DEFAULT FALSE," +
								 $"'{STR_JOB_SENDER_UPDATE}' BOOL NOT NULL DEFAULT FALSE);" +
					//ScheduleList
					$"Create Table IF NOT EXISTS '{STR_SCHEDULE_TABLE_NAME}'(" +
								 $"'{STR_SCHEDULE_ID}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT," +
								 $"'{STR_SCHEDULE_JOB_ID}' INTEGER NOT NULL, " +
								 $"'{STR_SCHEDULE_WEEKS}' INTEGER NOT NULL, " +
								 $"'{STR_SCHEDULE_AT_TIME}' INTEGER NOT NULL, " +
								 $"'{STR_SCHEDULE_FOR_HOURS}' INTEGER NOT NULL, " +
								 $"FOREIGN KEY('{STR_SCHEDULE_JOB_ID}') REFERENCES '{STR_JOB_TABLE_NAME}'('{STR_JOB_ID}') ON DELETE CASCADE);" +
								 "PRAGMA foreign_keys = On;" //Delete Cascade : on
				};
				cmd.ExecuteNonQuery();
				_log.Debug($"Create Table: {cmd.CommandText}");
				return true;
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}

		public bool Insert(JobData job)
		{
			if (!File.Exists(_filePath) && !CreateDBFile())
				return false;

			try
			{
				_mutex.WaitOne();
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{
					CommandText =
					$"INSERT INTO '{STR_JOB_TABLE_NAME}' ({STR_JOB_GLOBAL}, {STR_JOB_HOSTNAME}, {STR_JOB_NAME}, {STR_JOB_IS_GLOBAL_USER}, {STR_JOB_USER_ID}, {STR_JOB_PATH}, {STR_JOB_BLACK_PATH}, {STR_JOB_BLACK_FILE}, {STR_JOB_BLACK_FILE_EXT}, {STR_JOB_WHITE_FILE}, {STR_JOB_WHITE_FILE_EXT}, {STR_JOB_VSS_FILE_EXT}, {STR_JOB_POLICY_NAME})" +
					$" VALUES('{job.Global}', '{job.HostName}', '{job.JobName}', {job.IsGlobalUser}, {job.UserId}, '{job.StrPath}', '{job.StrBlackPath}', '{job.StrBlackFile}', '{job.StrBlackFileExt}', '{job.StrWhiteFile}', '{job.StrWhiteFileExt}', '{job.StrVSSFileExt}', '{job.StrPolicy}');",
				};

				int result = cmd.ExecuteNonQuery();
				if (result > 0) { _log.Debug($"Insert : {cmd.CommandText} : {result}"); return true; }
				else { _log.Error($"Insert failed : {cmd.CommandText} : {result}"); return false; }
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}
		public bool Update(JobData job)
		{
			if (!File.Exists(_filePath)) return false;

			try
			{
				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{
					CommandText = $"UPDATE '{STR_JOB_TABLE_NAME}' SET {STR_JOB_IS_GLOBAL_USER} = {job.IsGlobalUser} , " +
									$"{STR_JOB_USER_ID} = {job.UserId} , " +
									$"{STR_JOB_POLICY_NAME} = '{job.StrPolicy}', " +
									$"{STR_JOB_PATH} = '{job.StrPath}', " +
									$"{STR_JOB_BLACK_PATH} = '{job.StrBlackPath}', " +
									$"{STR_JOB_BLACK_FILE} = '{job.StrBlackFile}', " +
									$"{STR_JOB_BLACK_FILE_EXT} = '{job.StrBlackFileExt}', " +
									$"{STR_JOB_WHITE_FILE} = '{job.StrWhiteFile}', " +
									$"{STR_JOB_WHITE_FILE_EXT} = '{job.StrWhiteFileExt}', " +
									$"{STR_JOB_VSS_FILE_EXT} = '{job.StrVSSFileExt}', " +
									$"{STR_JOB_REMOVE} = {job.Remove} , " +
									$"{STR_JOB_IS_INIT} = {job.IsInit} , " +
									$"{STR_JOB_FILTER_UPDATE} = {true} , " +
									$"{STR_JOB_SENDER_UPDATE} = {true} " +
									$"WHERE {STR_JOB_ID} = {job.Id}; ",
				};
				int result = cmd.ExecuteNonQuery();
				if (result > 0) { _log.Debug($"Update : {cmd.CommandText} : {result}"); return true; }
				else { _log.Error($"Update failed : {cmd.CommandText} : {result}"); return false; }
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}
		public bool Delete(int jobId)
		{
			if (!File.Exists(_filePath)) return false;

			try
			{
				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"DELETE FROM '{STR_JOB_TABLE_NAME}' WHERE {STR_JOB_ID} = {jobId}" };
				int result = cmd.ExecuteNonQuery();

				if (result > 0) { _log.Debug($"Delete : {cmd.CommandText} : {result}"); return true; }
				else { _log.Error($"Delete failed : {cmd.CommandText} : {result}"); return false; }
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}

		public bool DeleteCascadeForUser(int userId, bool isGlobalUser)
		{
			if (!File.Exists(_filePath)) return false;

			try
			{
				_mutex.WaitOne();
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{ CommandText = $"DELETE FROM '{STR_JOB_TABLE_NAME}' WHERE {STR_JOB_USER_ID} = {userId} AND {STR_JOB_IS_GLOBAL_USER} = {isGlobalUser};" };

				int result = cmd.ExecuteNonQuery();
				if (result > 0) { _log.Debug($"DeleteCascadeForUser : {cmd.CommandText} : {result}"); return true; }
				else { _log.Error($"DeleteCascadeForUser failed : {cmd.CommandText} : {result}"); return false; }
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}

		bool InsertScheduleList(JobData job)
		{
			if (!File.Exists(_filePath)) return false;
			if (job.Id < 1) job.Id = GetJobDataId(job.HostName, job.JobName);
			if (job.ScheduleList.Count == 0)
			{
				_log.Error("ScheduleList is Empty");
				return false;
			}

			try
			{
				_mutex.WaitOne();
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");

				conn.Open();

				using (var cmd = new SQLiteCommand(conn))
				{
					cmd.CommandText = $"DELETE FROM '{STR_SCHEDULE_TABLE_NAME}' WHERE {STR_SCHEDULE_JOB_ID} = {job.Id}";
					cmd.ExecuteNonQuery();
				}

				//Insert
				foreach (var item in job.ScheduleList)
				{
					using var cmd = new SQLiteCommand(conn)
					{
						CommandText = $"INSERT INTO '{STR_SCHEDULE_TABLE_NAME}' ( {STR_SCHEDULE_JOB_ID} , {STR_SCHEDULE_WEEKS} , {STR_SCHEDULE_AT_TIME} , {STR_SCHEDULE_FOR_HOURS} )" +
														$" VALUES ('{job.Id}', '{item.Weeks}', '{item.AtTime}', '{item.ForHours}')"
					};
					if (cmd.ExecuteNonQuery() > 0)
						_log.Debug($"InsertScheduleList : {cmd.CommandText}");
					else
						_log.Error($"InsertScheduleList failed : {cmd.CommandText}");
				}

				return true;
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}

		public bool UpdateIsInit(JobData job, bool flag)
		{
			if (!File.Exists(_filePath)) return false;

			try
			{
				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"UPDATE '{STR_JOB_TABLE_NAME}' SET {STR_JOB_IS_INIT} = {flag} WHERE {STR_JOB_ID} = {job.Id};" };
				int result = cmd.ExecuteNonQuery();

				if (result > 0) { _log.Debug($"UpdateIsInit : {cmd.CommandText} : {result}"); return true; }
				else { _log.Error($"UpdateIsInit failed : {cmd.CommandText} : {result}"); return false; }
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}
		public bool UpdateFilterCheck(JobData job)
		{
			try
			{
				if (job == null || job.Id < 1) return false;
				if (!File.Exists(_filePath)) return false;

				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"UPDATE '{STR_JOB_TABLE_NAME}' SET {STR_JOB_FILTER_UPDATE} = {false} WHERE {STR_JOB_ID} = {job.Id};" };
				int result = cmd.ExecuteNonQuery();

				if (result > 0) { _log.Debug($"UpdateFilterCheck : {cmd.CommandText} : {result}"); return true; }
				else { _log.Error($"UpdateFilterCheck failed : {cmd.CommandText} : {result}"); return false; }
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}
		public bool UpdateSenderCheck(JobData job)
		{
			if (!File.Exists(_filePath)) return false;

			try
			{
				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"UPDATE '{STR_JOB_TABLE_NAME}' SET {STR_JOB_SENDER_UPDATE} = {false} WHERE {STR_JOB_ID} = {job.Id};" };
				int result = cmd.ExecuteNonQuery();

				if (result > 0) { _log.Debug($"UpdateSenderCheck : {cmd.CommandText} : {result}"); return true; }
				else { _log.Error($"UpdateSenderCheck failed : {cmd.CommandText} : {result}"); return false; }
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}
		public bool UpdateAllCheck(JobData job)
		{
			try
			{
				if (job == null || job.Id < 1) return false;
				if (!File.Exists(_filePath)) return false;

				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"UPDATE '{STR_JOB_TABLE_NAME}' SET {STR_JOB_FILTER_UPDATE} = {false} , {STR_JOB_SENDER_UPDATE} = {false} WHERE {STR_JOB_ID} = {job.Id};" };
				int result = cmd.ExecuteNonQuery();

				if (result > 0) { _log.Debug($"UpdateFilterCheck : {cmd.CommandText} : {result}"); return true; }
				else { _log.Error($"UpdateFilterCheck failed : {cmd.CommandText} : {result}"); return false; }
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}

		public int GetJobDataId(string hostName, string jobName)
		{
			int id = 0;
			try
			{
				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM '{STR_JOB_TABLE_NAME}' WHERE {STR_JOB_HOSTNAME} = '{hostName}' AND {STR_JOB_NAME} = '{jobName}';" };
				using var rdr = cmd.ExecuteReader();

				while (rdr.Read()) id = Convert.ToInt32(rdr[STR_JOB_ID]);

				if (id > 0) _log.Debug($"GetJobDataId : {cmd.CommandText} : {id}");
				else _log.Error($"GetJobDataId failed : {cmd.CommandText} : {id}");
			}
			catch (Exception e) { _log.Error(e); }
			finally { _mutex.ReleaseMutex(); }
			return id;
		}

		public bool IsJobName(string hostName, string jobName)
		{
			try
			{
				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"SELECT count(*) FROM '{STR_JOB_TABLE_NAME}' WHERE {STR_JOB_HOSTNAME} = '{hostName}' AND {STR_JOB_NAME} = '{jobName}';" };
				int result = Convert.ToInt32(cmd.ExecuteScalar());

				if (result > 0) { _log.Debug($"IsJobName : {cmd.CommandText} : {result}"); return true; }
				else { _log.Debug($"IsJobName failed : {cmd.CommandText} : {result}"); return false; }
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}

		public List<JobData> GetJobs()
		{
			var items = new List<JobData>();
			try
			{
				if (!File.Exists(_filePath)) CreateDBFile();

				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var jobCmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM '{STR_JOB_TABLE_NAME}'" };
				using var jobRdr = jobCmd.ExecuteReader();

				while (jobRdr.Read())
				{
					var Data = new JobData
					{
						Id = Convert.ToInt32(jobRdr[STR_JOB_ID]),
						Global = Convert.ToBoolean(jobRdr[STR_JOB_GLOBAL]),
						HostName = jobRdr[STR_JOB_HOSTNAME].ToString(),
						JobName = jobRdr[STR_JOB_NAME].ToString(),
						IsGlobalUser = Convert.ToBoolean(jobRdr[STR_JOB_IS_GLOBAL_USER]),
						UserId = Convert.ToInt32(jobRdr[STR_JOB_USER_ID]),
						StrPolicy = jobRdr[STR_JOB_POLICY_NAME].ToString(),
						StrPath = jobRdr[STR_JOB_PATH].ToString(),
						StrBlackPath = jobRdr[STR_JOB_BLACK_PATH].ToString(),
						StrBlackFile = jobRdr[STR_JOB_BLACK_FILE].ToString(),
						StrBlackFileExt = jobRdr[STR_JOB_BLACK_FILE_EXT].ToString(),
						StrWhiteFile = jobRdr[STR_JOB_WHITE_FILE].ToString(),
						StrWhiteFileExt = jobRdr[STR_JOB_WHITE_FILE_EXT].ToString(),
						StrVSSFileExt = jobRdr[STR_JOB_VSS_FILE_EXT].ToString(),
						Remove = Convert.ToBoolean(jobRdr[STR_JOB_REMOVE]),
						IsInit = Convert.ToBoolean(jobRdr[STR_JOB_IS_INIT]),
						FilterUpdate = Convert.ToBoolean(jobRdr[STR_JOB_FILTER_UPDATE]),
						SenderUpdate = Convert.ToBoolean(jobRdr[STR_JOB_SENDER_UPDATE]),
					};

					//Get Schedule
					if (Data.Policy == JobData.PolicyType.Schedule)
					{
						using var scheduleCmd = new SQLiteCommand(conn);
						scheduleCmd.CommandText = string.Format("SELECT * FROM {0} WHERE {1} = {2};", STR_SCHEDULE_TABLE_NAME, STR_SCHEDULE_JOB_ID, Data.Id);
						using var ScheduleRdr = scheduleCmd.ExecuteReader();

						while (ScheduleRdr.Read())
						{
							Data.ScheduleList.Add(new Schedule()
							{
								Id = Convert.ToInt32(ScheduleRdr[STR_SCHEDULE_ID]),
								JobId = Convert.ToInt32(ScheduleRdr[STR_SCHEDULE_JOB_ID]),
								Weeks = Convert.ToInt32(ScheduleRdr[STR_SCHEDULE_WEEKS]),
								AtTime = Convert.ToInt32(ScheduleRdr[STR_SCHEDULE_AT_TIME]),
								ForHours = Convert.ToInt32(ScheduleRdr[STR_SCHEDULE_FOR_HOURS]),
							});
						}
					}

					items.Add(Data);
				}

				_log.Debug($"GetJobs : {jobCmd.CommandText} : {items.Count}");
				return items;
			}
			catch (Exception e)
			{
				_log.Error(e);
				return [];
			}
			finally { _mutex.ReleaseMutex(); }
		}
		public List<JobData> GetJobs(string HostName)
		{
			if (!File.Exists(_filePath)) CreateDBFile();
			try
			{
				var items = new List<JobData>();

				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var jobCmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM '{STR_JOB_TABLE_NAME}' Where {STR_JOB_HOSTNAME} = '{HostName}';" };
				using var jobRdr = jobCmd.ExecuteReader();

				while (jobRdr.Read())
				{
					var Data = new JobData
					{
						Id = Convert.ToInt32(jobRdr[STR_JOB_ID]),
						Global = Convert.ToBoolean(jobRdr[STR_JOB_GLOBAL]),
						HostName = jobRdr[STR_JOB_HOSTNAME].ToString(),
						JobName = jobRdr[STR_JOB_NAME].ToString(),
						IsGlobalUser = Convert.ToBoolean(jobRdr[STR_JOB_IS_GLOBAL_USER]),
						UserId = Convert.ToInt32(jobRdr[STR_JOB_USER_ID]),
						StrPolicy = jobRdr[STR_JOB_POLICY_NAME].ToString(),
						StrPath = jobRdr[STR_JOB_PATH].ToString(),
						StrBlackPath = jobRdr[STR_JOB_BLACK_PATH].ToString(),
						StrBlackFile = jobRdr[STR_JOB_BLACK_FILE].ToString(),
						StrBlackFileExt = jobRdr[STR_JOB_BLACK_FILE_EXT].ToString(),
						StrWhiteFile = jobRdr[STR_JOB_WHITE_FILE].ToString(),
						StrWhiteFileExt = jobRdr[STR_JOB_WHITE_FILE_EXT].ToString(),
						StrVSSFileExt = jobRdr[STR_JOB_VSS_FILE_EXT].ToString(),
						Remove = Convert.ToBoolean(jobRdr[STR_JOB_REMOVE]),
						IsInit = Convert.ToBoolean(jobRdr[STR_JOB_IS_INIT]),
						FilterUpdate = Convert.ToBoolean(jobRdr[STR_JOB_FILTER_UPDATE]),
						SenderUpdate = Convert.ToBoolean(jobRdr[STR_JOB_SENDER_UPDATE]),
					};

					//Get Schedule
					if (Data.Policy == JobData.PolicyType.Schedule)
					{
						using var scheduleCmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM {STR_SCHEDULE_TABLE_NAME} WHERE {STR_SCHEDULE_JOB_ID} = {Data.Id};" };
						using var scheduleRdr = scheduleCmd.ExecuteReader();

						while (scheduleRdr.Read())
						{
							var Item = new Schedule()
							{
								Id = Convert.ToInt32(scheduleRdr[STR_SCHEDULE_ID]),
								JobId = Convert.ToInt32(scheduleRdr[STR_SCHEDULE_JOB_ID]),
								Weeks = Convert.ToInt32(scheduleRdr[STR_SCHEDULE_WEEKS]),
								AtTime = Convert.ToInt32(scheduleRdr[STR_SCHEDULE_AT_TIME]),
								ForHours = Convert.ToInt32(scheduleRdr[STR_SCHEDULE_FOR_HOURS]),
							};
							Data.ScheduleList.Add(Item);
						}
					}

					items.Add(Data);
				}

				_log.Debug($"GetJobs : {jobCmd.CommandText} : {items.Count}");
				return items;
			}
			catch (Exception e)
			{
				_log.Error(e);
				return [];
			}
			finally { _mutex.ReleaseMutex(); }
		}
		public JobData GetJob(int Id)
		{
			if (!File.Exists(_filePath)) CreateDBFile();
			try
			{
				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM '{STR_JOB_TABLE_NAME}' WHERE {STR_JOB_ID}={Id}" };
				using var rdr = cmd.ExecuteReader();

				rdr.Read();

				var Data = new JobData
				{
					Id = Convert.ToInt32(rdr[STR_JOB_ID]),
					Global = Convert.ToBoolean(rdr[STR_JOB_GLOBAL]),
					HostName = rdr[STR_JOB_HOSTNAME].ToString(),
					JobName = rdr[STR_JOB_NAME].ToString(),
					IsGlobalUser = Convert.ToBoolean(rdr[STR_JOB_IS_GLOBAL_USER]),
					UserId = Convert.ToInt32(rdr[STR_JOB_USER_ID]),
					StrPolicy = rdr[STR_JOB_POLICY_NAME].ToString(),
					StrPath = rdr[STR_JOB_PATH].ToString(),
					StrBlackPath = rdr[STR_JOB_BLACK_PATH].ToString(),
					StrBlackFile = rdr[STR_JOB_BLACK_FILE].ToString(),
					StrBlackFileExt = rdr[STR_JOB_BLACK_FILE_EXT].ToString(),
					StrWhiteFile = rdr[STR_JOB_WHITE_FILE].ToString(),
					StrWhiteFileExt = rdr[STR_JOB_WHITE_FILE_EXT].ToString(),
					StrVSSFileExt = rdr[STR_JOB_VSS_FILE_EXT].ToString(),
					Remove = Convert.ToBoolean(rdr[STR_JOB_REMOVE]),
					IsInit = Convert.ToBoolean(rdr[STR_JOB_IS_INIT]),
					FilterUpdate = Convert.ToBoolean(rdr[STR_JOB_FILTER_UPDATE]),
					SenderUpdate = Convert.ToBoolean(rdr[STR_JOB_SENDER_UPDATE]),
				};

				//Get Schedule
				if (Data.Policy == JobData.PolicyType.Schedule)
				{
					using var scheduleCmd = new SQLiteCommand(conn);
					scheduleCmd.CommandText = string.Format(
									"SELECT * FROM {0} WHERE {1} = {2};",
									STR_SCHEDULE_TABLE_NAME, STR_SCHEDULE_JOB_ID, Data.Id);
					using var scheduleRdr = scheduleCmd.ExecuteReader();

					while (scheduleRdr.Read())
					{
						Data.ScheduleList.Add(new Schedule()
						{
							Id = Convert.ToInt32(scheduleRdr[STR_SCHEDULE_ID]),
							JobId = Convert.ToInt32(scheduleRdr[STR_SCHEDULE_JOB_ID]),
							Weeks = Convert.ToInt32(scheduleRdr[STR_SCHEDULE_WEEKS]),
							AtTime = Convert.ToInt32(scheduleRdr[STR_SCHEDULE_AT_TIME]),
							ForHours = Convert.ToInt32(scheduleRdr[STR_SCHEDULE_FOR_HOURS]),
						});
					}
				}

				_log.Debug($"GetJob : {cmd.CommandText} : {Data.JobName}");
				return Data;
			}
			catch (Exception e)
			{
				_log.Error(e);
				return null;
			}
			finally { _mutex.ReleaseMutex(); }
		}

		public bool PutJobData(JobData data)
		{
			if (data.Id > 0) { if (!Update(data)) return false; }
			else { if (!Insert(data)) return false; }

			if (data.Policy == JobData.PolicyType.Schedule) return InsertScheduleList(data);
			return true;
		}
		public int NextJobIndex()
		{
			int Index = 0;
			try
			{
				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM '{SQLITE_SEQUENCE}' WHERE name = '{STR_JOB_TABLE_NAME}'" };
				using var rdr = cmd.ExecuteReader();
				while (rdr.Read())
				{
					Index = Convert.ToInt32(rdr[SQLITE_SEQUENCE_SEQ]);
				}

				_log.Debug($"NextJobIndex : {cmd.CommandText} : {Index}");
			}
			catch (Exception e) { _log.Error(e); }
			finally { _mutex.ReleaseMutex(); }
			return Index + 1;
		}
	}
}
