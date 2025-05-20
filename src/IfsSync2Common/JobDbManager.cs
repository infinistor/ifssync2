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
using System.Data.SQLite;

namespace IfsSync2Common
{
	public class JobDbManager
	{
		readonly ILog _log = LogManager.GetLogger(typeof(JobDbManager));
		readonly string _filePath = IfsSync2Utilities.GetDBFilePath(IfsSync2Constants.JOB_DB_FILE_NAME);
		readonly Mutex _mutex;

		public JobDbManager()
		{
			try
			{
				_mutex = new Mutex(false, IfsSync2Constants.MUTEX_NAME_JOB_SQL, out bool createdNew);

				if (!createdNew) _log.Debug($"Mutex({IfsSync2Constants.MUTEX_NAME_JOB_SQL})");
				else _log.Debug($"Mutex({IfsSync2Constants.MUTEX_NAME_JOB_SQL}) create");
			}
			catch (Exception e)
			{
				_log.Error($"Mutex({IfsSync2Constants.MUTEX_NAME_JOB_SQL}) fail", e);
				throw new InvalidOperationException($"Mutex({IfsSync2Constants.MUTEX_NAME_JOB_SQL}) fail", e);
			}
		}

		bool CreateDBFile()
		{
			try
			{
				IfsSync2Utilities.CreateFile(_filePath);

				_mutex.WaitOne();
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{
					CommandText =
					//JobList
					$"Create Table IF NOT EXISTS '{IfsSync2Constants.DB_TABLE_JOB_LIST}'(" +
								 $"'{IfsSync2Constants.DB_FIELD_ID}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT," +
								 $"'{IfsSync2Constants.DB_FIELD_HOSTNAME}' TEXT, " +
								 $"'{IfsSync2Constants.DB_FIELD_JOBNAME}' TEXT NOT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_GLOBAL}' BOOL NOT NULL DEFAULT FALSE, " +
								 $"'{IfsSync2Constants.DB_FIELD_IS_GLOBAL_USER}' BOOL NOT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_USER_ID}' INTEGER NOT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_POLICY_NAME}' TEXT NOT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_PATH}' TEXT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_BLACK_PATH}' TEXT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_BLACK_FILE}' TEXT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_BLACK_FILE_EXT}' TEXT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_WHITE_FILE}' TEXT NOT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_WHITE_FILE_EXT}' TEXT NOT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_VSS_FILE_EXT}' TEXT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_REMOVE}' BOOL NOT NULL DEFAULT TRUE," +
								 $"'{IfsSync2Constants.DB_FIELD_IS_INIT}' BOOL NOT NULL DEFAULT TRUE," +
								 $"'{IfsSync2Constants.DB_FIELD_FILTER_UPDATE}' BOOL NOT NULL DEFAULT FALSE," +
								 $"'{IfsSync2Constants.DB_FIELD_SENDER_UPDATE}' BOOL NOT NULL DEFAULT FALSE);" +
					//ScheduleList
					$"Create Table IF NOT EXISTS '{IfsSync2Constants.DB_TABLE_SCHEDULE_LIST}'(" +
								 $"'{IfsSync2Constants.DB_FIELD_ID}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT," +
								 $"'{IfsSync2Constants.DB_FIELD_SCHEDULE_JOB_ID}' INTEGER NOT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_SCHEDULE_WEEKS}' INTEGER NOT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_SCHEDULE_AT_TIME}' INTEGER NOT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_SCHEDULE_FOR_HOURS}' INTEGER NOT NULL, " +
								 $"FOREIGN KEY('{IfsSync2Constants.DB_FIELD_SCHEDULE_JOB_ID}') REFERENCES '{IfsSync2Constants.DB_TABLE_JOB_LIST}'('{IfsSync2Constants.DB_FIELD_ID}') ON DELETE CASCADE);" +
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
					$"INSERT INTO '{IfsSync2Constants.DB_TABLE_JOB_LIST}' ({IfsSync2Constants.DB_FIELD_GLOBAL}, {IfsSync2Constants.DB_FIELD_HOSTNAME}, {IfsSync2Constants.DB_FIELD_JOBNAME}, {IfsSync2Constants.DB_FIELD_IS_GLOBAL_USER}, {IfsSync2Constants.DB_FIELD_USER_ID}, {IfsSync2Constants.DB_FIELD_PATH}, {IfsSync2Constants.DB_FIELD_BLACK_PATH}, {IfsSync2Constants.DB_FIELD_BLACK_FILE}, {IfsSync2Constants.DB_FIELD_BLACK_FILE_EXT}, {IfsSync2Constants.DB_FIELD_WHITE_FILE}, {IfsSync2Constants.DB_FIELD_WHITE_FILE_EXT}, {IfsSync2Constants.DB_FIELD_VSS_FILE_EXT}, {IfsSync2Constants.DB_FIELD_POLICY_NAME})" +
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
					CommandText = $"UPDATE '{IfsSync2Constants.DB_TABLE_JOB_LIST}' SET {IfsSync2Constants.DB_FIELD_IS_GLOBAL_USER} = {job.IsGlobalUser} , " +
									$"{IfsSync2Constants.DB_FIELD_USER_ID} = {job.UserId} , " +
									$"{IfsSync2Constants.DB_FIELD_POLICY_NAME} = '{job.StrPolicy}', " +
									$"{IfsSync2Constants.DB_FIELD_PATH} = '{job.StrPath}', " +
									$"{IfsSync2Constants.DB_FIELD_BLACK_PATH} = '{job.StrBlackPath}', " +
									$"{IfsSync2Constants.DB_FIELD_BLACK_FILE} = '{job.StrBlackFile}', " +
									$"{IfsSync2Constants.DB_FIELD_BLACK_FILE_EXT} = '{job.StrBlackFileExt}', " +
									$"{IfsSync2Constants.DB_FIELD_WHITE_FILE} = '{job.StrWhiteFile}', " +
									$"{IfsSync2Constants.DB_FIELD_WHITE_FILE_EXT} = '{job.StrWhiteFileExt}', " +
									$"{IfsSync2Constants.DB_FIELD_VSS_FILE_EXT} = '{job.StrVSSFileExt}', " +
									$"{IfsSync2Constants.DB_FIELD_REMOVE} = {job.Remove} , " +
									$"{IfsSync2Constants.DB_FIELD_IS_INIT} = {job.IsInit} , " +
									$"{IfsSync2Constants.DB_FIELD_FILTER_UPDATE} = {true} , " +
									$"{IfsSync2Constants.DB_FIELD_SENDER_UPDATE} = {true} " +
									$"WHERE {IfsSync2Constants.DB_FIELD_ID} = {job.Id}; ",
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

				using var cmd = new SQLiteCommand(conn) { CommandText = $"DELETE FROM '{IfsSync2Constants.DB_TABLE_JOB_LIST}' WHERE {IfsSync2Constants.DB_FIELD_ID} = {jobId}" };
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
				{ CommandText = $"DELETE FROM '{IfsSync2Constants.DB_TABLE_JOB_LIST}' WHERE {IfsSync2Constants.DB_FIELD_USER_ID} = {userId} AND {IfsSync2Constants.DB_FIELD_IS_GLOBAL_USER} = {isGlobalUser};" };

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
					cmd.CommandText = $"DELETE FROM '{IfsSync2Constants.DB_TABLE_SCHEDULE_LIST}' WHERE {IfsSync2Constants.DB_FIELD_SCHEDULE_JOB_ID} = {job.Id}";
					cmd.ExecuteNonQuery();
				}

				//Insert
				foreach (var item in job.ScheduleList)
				{
					using var cmd = new SQLiteCommand(conn)
					{
						CommandText = $"INSERT INTO '{IfsSync2Constants.DB_TABLE_SCHEDULE_LIST}' ( {IfsSync2Constants.DB_FIELD_SCHEDULE_JOB_ID} , {IfsSync2Constants.DB_FIELD_SCHEDULE_WEEKS} , {IfsSync2Constants.DB_FIELD_SCHEDULE_AT_TIME} , {IfsSync2Constants.DB_FIELD_SCHEDULE_FOR_HOURS} )" +
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

				using var cmd = new SQLiteCommand(conn) { CommandText = $"UPDATE '{IfsSync2Constants.DB_TABLE_JOB_LIST}' SET {IfsSync2Constants.DB_FIELD_IS_INIT} = {flag} WHERE {IfsSync2Constants.DB_FIELD_ID} = {job.Id};" };
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

				using var cmd = new SQLiteCommand(conn) { CommandText = $"UPDATE '{IfsSync2Constants.DB_TABLE_JOB_LIST}' SET {IfsSync2Constants.DB_FIELD_FILTER_UPDATE} = {false} WHERE {IfsSync2Constants.DB_FIELD_ID} = {job.Id};" };
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

				using var cmd = new SQLiteCommand(conn) { CommandText = $"UPDATE '{IfsSync2Constants.DB_TABLE_JOB_LIST}' SET {IfsSync2Constants.DB_FIELD_SENDER_UPDATE} = {false} WHERE {IfsSync2Constants.DB_FIELD_ID} = {job.Id};" };
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

				using var cmd = new SQLiteCommand(conn) { CommandText = $"UPDATE '{IfsSync2Constants.DB_TABLE_JOB_LIST}' SET {IfsSync2Constants.DB_FIELD_FILTER_UPDATE} = {false} , {IfsSync2Constants.DB_FIELD_SENDER_UPDATE} = {false} WHERE {IfsSync2Constants.DB_FIELD_ID} = {job.Id};" };
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

				using var cmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM '{IfsSync2Constants.DB_TABLE_JOB_LIST}' WHERE {IfsSync2Constants.DB_FIELD_HOSTNAME} = '{hostName}' AND {IfsSync2Constants.DB_FIELD_JOBNAME} = '{jobName}';" };
				using var rdr = cmd.ExecuteReader();

				while (rdr.Read()) id = rdr.GetInt(IfsSync2Constants.DB_FIELD_ID);

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

				using var cmd = new SQLiteCommand(conn) { CommandText = $"SELECT count(*) FROM '{IfsSync2Constants.DB_TABLE_JOB_LIST}' WHERE {IfsSync2Constants.DB_FIELD_HOSTNAME} = '{hostName}' AND {IfsSync2Constants.DB_FIELD_JOBNAME} = '{jobName}';" };
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

				using var jobCmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM '{IfsSync2Constants.DB_TABLE_JOB_LIST}'" };
				using var jobRdr = jobCmd.ExecuteReader();

				while (jobRdr.Read())
				{
					var data = new JobData
					{
						Id = jobRdr.GetInt(IfsSync2Constants.DB_FIELD_ID),
						Global = jobRdr.GetBool(IfsSync2Constants.DB_FIELD_GLOBAL),
						HostName = jobRdr.GetString(IfsSync2Constants.DB_FIELD_HOSTNAME),
						JobName = jobRdr.GetString(IfsSync2Constants.DB_FIELD_JOBNAME),
						IsGlobalUser = Convert.ToBoolean(jobRdr[IfsSync2Constants.DB_FIELD_IS_GLOBAL_USER]),
						UserId = jobRdr.GetInt(IfsSync2Constants.DB_FIELD_USER_ID),
						StrPolicy = jobRdr.GetString(IfsSync2Constants.DB_FIELD_POLICY_NAME),
						StrPath = jobRdr.GetString(IfsSync2Constants.DB_FIELD_PATH),
						StrBlackPath = jobRdr.GetString(IfsSync2Constants.DB_FIELD_BLACK_PATH),
						StrBlackFile = jobRdr.GetString(IfsSync2Constants.DB_FIELD_BLACK_FILE),
						StrBlackFileExt = jobRdr.GetString(IfsSync2Constants.DB_FIELD_BLACK_FILE_EXT),
						StrWhiteFile = jobRdr.GetString(IfsSync2Constants.DB_FIELD_WHITE_FILE),
						StrWhiteFileExt = jobRdr.GetString(IfsSync2Constants.DB_FIELD_WHITE_FILE_EXT),
						StrVSSFileExt = jobRdr.GetString(IfsSync2Constants.DB_FIELD_VSS_FILE_EXT),
						Remove = jobRdr.GetBool(IfsSync2Constants.DB_FIELD_REMOVE),
						IsInit = jobRdr.GetBool(IfsSync2Constants.DB_FIELD_IS_INIT),
						FilterUpdate = jobRdr.GetBool(IfsSync2Constants.DB_FIELD_FILTER_UPDATE),
						SenderUpdate = jobRdr.GetBool(IfsSync2Constants.DB_FIELD_SENDER_UPDATE),
					};

					//Get Schedule
					if (data.Policy == JobData.PolicyType.Schedule)
					{
						using var scheduleCmd = new SQLiteCommand(conn);
						scheduleCmd.CommandText = string.Format("SELECT * FROM {0} WHERE {1} = {2};", IfsSync2Constants.DB_TABLE_SCHEDULE_LIST, IfsSync2Constants.DB_FIELD_SCHEDULE_JOB_ID, data.Id);
						using var scheduleRdr = scheduleCmd.ExecuteReader();

						while (scheduleRdr.Read())
						{
							data.ScheduleList.Add(new Schedule()
							{
								Id = Convert.ToInt32(scheduleRdr[IfsSync2Constants.DB_FIELD_ID]),
								JobId = Convert.ToInt32(scheduleRdr[IfsSync2Constants.DB_FIELD_SCHEDULE_JOB_ID]),
								Weeks = Convert.ToInt32(scheduleRdr[IfsSync2Constants.DB_FIELD_SCHEDULE_WEEKS]),
								AtTime = Convert.ToInt32(scheduleRdr[IfsSync2Constants.DB_FIELD_SCHEDULE_AT_TIME]),
								ForHours = Convert.ToInt32(scheduleRdr[IfsSync2Constants.DB_FIELD_SCHEDULE_FOR_HOURS]),
							});
						}
					}

					items.Add(data);
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
		public List<JobData> GetJobs(string hostName)
		{
			if (!File.Exists(_filePath)) CreateDBFile();
			try
			{
				var items = new List<JobData>();

				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var jobCmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM '{IfsSync2Constants.DB_TABLE_JOB_LIST}' Where {IfsSync2Constants.DB_FIELD_HOSTNAME} = '{hostName}';" };
				using var jobRdr = jobCmd.ExecuteReader();

				while (jobRdr.Read())
				{
					var data = new JobData
					{
						Id = jobRdr.GetInt(IfsSync2Constants.DB_FIELD_ID),
						Global = jobRdr.GetBool(IfsSync2Constants.DB_FIELD_GLOBAL),
						HostName = jobRdr.GetString(IfsSync2Constants.DB_FIELD_HOSTNAME),
						JobName = jobRdr.GetString(IfsSync2Constants.DB_FIELD_JOBNAME),
						IsGlobalUser = jobRdr.GetBool(IfsSync2Constants.DB_FIELD_IS_GLOBAL_USER),
						UserId = jobRdr.GetInt(IfsSync2Constants.DB_FIELD_USER_ID),
						StrPolicy = jobRdr.GetString(IfsSync2Constants.DB_FIELD_POLICY_NAME),
						StrPath = jobRdr.GetString(IfsSync2Constants.DB_FIELD_PATH),
						StrBlackPath = jobRdr.GetString(IfsSync2Constants.DB_FIELD_BLACK_PATH),
						StrBlackFile = jobRdr.GetString(IfsSync2Constants.DB_FIELD_BLACK_FILE),
						StrBlackFileExt = jobRdr.GetString(IfsSync2Constants.DB_FIELD_BLACK_FILE_EXT),
						StrWhiteFile = jobRdr.GetString(IfsSync2Constants.DB_FIELD_WHITE_FILE),
						StrWhiteFileExt = jobRdr.GetString(IfsSync2Constants.DB_FIELD_WHITE_FILE_EXT),
						StrVSSFileExt = jobRdr.GetString(IfsSync2Constants.DB_FIELD_VSS_FILE_EXT),
						Remove = jobRdr.GetBool(IfsSync2Constants.DB_FIELD_REMOVE),
						IsInit = jobRdr.GetBool(IfsSync2Constants.DB_FIELD_IS_INIT),
						FilterUpdate = jobRdr.GetBool(IfsSync2Constants.DB_FIELD_FILTER_UPDATE),
						SenderUpdate = jobRdr.GetBool(IfsSync2Constants.DB_FIELD_SENDER_UPDATE),
					};

					//Get Schedule
					if (data.Policy == JobData.PolicyType.Schedule)
					{
						using var scheduleCmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM {IfsSync2Constants.DB_TABLE_SCHEDULE_LIST} WHERE {IfsSync2Constants.DB_FIELD_SCHEDULE_JOB_ID} = {data.Id};" };
						using var scheduleRdr = scheduleCmd.ExecuteReader();

						while (scheduleRdr.Read())
						{
							var item = new Schedule()
							{
								Id = Convert.ToInt32(scheduleRdr[IfsSync2Constants.DB_FIELD_ID]),
								JobId = Convert.ToInt32(scheduleRdr[IfsSync2Constants.DB_FIELD_SCHEDULE_JOB_ID]),
								Weeks = Convert.ToInt32(scheduleRdr[IfsSync2Constants.DB_FIELD_SCHEDULE_WEEKS]),
								AtTime = Convert.ToInt32(scheduleRdr[IfsSync2Constants.DB_FIELD_SCHEDULE_AT_TIME]),
								ForHours = Convert.ToInt32(scheduleRdr[IfsSync2Constants.DB_FIELD_SCHEDULE_FOR_HOURS]),
							};
							data.ScheduleList.Add(item);
						}
					}

					items.Add(data);
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
		public JobData? GetJob(int id)
		{
			if (!File.Exists(_filePath)) CreateDBFile();
			try
			{
				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM '{IfsSync2Constants.DB_TABLE_JOB_LIST}' WHERE {IfsSync2Constants.DB_FIELD_ID}={id}" };
				using var rdr = cmd.ExecuteReader();

				rdr.Read();

				var data = new JobData
				{
					Id = rdr.GetInt(IfsSync2Constants.DB_FIELD_ID),
					Global = rdr.GetBool(IfsSync2Constants.DB_FIELD_GLOBAL),
					HostName = rdr.GetString(IfsSync2Constants.DB_FIELD_HOSTNAME),
					JobName = rdr.GetString(IfsSync2Constants.DB_FIELD_JOBNAME),
					IsGlobalUser = rdr.GetBool(IfsSync2Constants.DB_FIELD_IS_GLOBAL_USER),
					UserId = rdr.GetInt(IfsSync2Constants.DB_FIELD_USER_ID),
					StrPolicy = rdr.GetString(IfsSync2Constants.DB_FIELD_POLICY_NAME),
					StrPath = rdr.GetString(IfsSync2Constants.DB_FIELD_PATH),
					StrBlackPath = rdr.GetString(IfsSync2Constants.DB_FIELD_BLACK_PATH),
					StrBlackFile = rdr.GetString(IfsSync2Constants.DB_FIELD_BLACK_FILE),
					StrBlackFileExt = rdr.GetString(IfsSync2Constants.DB_FIELD_BLACK_FILE_EXT),
					StrWhiteFile = rdr.GetString(IfsSync2Constants.DB_FIELD_WHITE_FILE),
					StrWhiteFileExt = rdr.GetString(IfsSync2Constants.DB_FIELD_WHITE_FILE_EXT),
					StrVSSFileExt = rdr.GetString(IfsSync2Constants.DB_FIELD_VSS_FILE_EXT),
					Remove = rdr.GetBool(IfsSync2Constants.DB_FIELD_REMOVE),
					IsInit = rdr.GetBool(IfsSync2Constants.DB_FIELD_IS_INIT),
					FilterUpdate = rdr.GetBool(IfsSync2Constants.DB_FIELD_FILTER_UPDATE),
					SenderUpdate = rdr.GetBool(IfsSync2Constants.DB_FIELD_SENDER_UPDATE),
				};

				//Get Schedule
				if (data.Policy == JobData.PolicyType.Schedule)
				{
					using var scheduleCmd = new SQLiteCommand(conn);
					scheduleCmd.CommandText = string.Format(
									"SELECT * FROM {0} WHERE {1} = {2};",
									IfsSync2Constants.DB_TABLE_SCHEDULE_LIST, IfsSync2Constants.DB_FIELD_SCHEDULE_JOB_ID, data.Id);
					using var scheduleRdr = scheduleCmd.ExecuteReader();

					while (scheduleRdr.Read())
					{
						data.ScheduleList.Add(new Schedule()
						{
							Id = scheduleRdr.GetInt(IfsSync2Constants.DB_FIELD_ID),
							JobId = scheduleRdr.GetInt(IfsSync2Constants.DB_FIELD_SCHEDULE_JOB_ID),
							Weeks = scheduleRdr.GetInt(IfsSync2Constants.DB_FIELD_SCHEDULE_WEEKS),
							AtTime = scheduleRdr.GetInt(IfsSync2Constants.DB_FIELD_SCHEDULE_AT_TIME),
							ForHours = scheduleRdr.GetInt(IfsSync2Constants.DB_FIELD_SCHEDULE_FOR_HOURS),
						});
					}
				}

				_log.Debug($"GetJob : {cmd.CommandText} : {data.JobName}");
				return data;
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
			int index = 0;
			try
			{
				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM '{IfsSync2Constants.DB_SQLITE_SEQUENCE}' WHERE name = '{IfsSync2Constants.DB_TABLE_JOB_LIST}'" };
				using var rdr = cmd.ExecuteReader();
				while (rdr.Read())
				{
					index = rdr.GetInt(IfsSync2Constants.DB_FIELD_SEQ);
				}

				_log.Debug($"NextJobIndex : {cmd.CommandText} : {index}");
			}
			catch (Exception e) { _log.Error(e); }
			finally { _mutex.ReleaseMutex(); }
			return index + 1;
		}
	}
}
