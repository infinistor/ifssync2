/*
* Copyright (c) 2021 PSPACE, inc. KSAN Development Team ksan@pspace.co.kr
* KSAN is a suite of free software: you can redistribute it and/or modify it under the terms of
* the GNU General Public License as published by the Free Software Foundation, either version 
* 3 of the License.See LICENSE for details
*
* 본 프로그램 및 관련 소스코드, 문서 등 모든 자료는 있는 그대로 제공이 됩니다.
* KSAN 프로젝트의 개발자 및 개발사는 이 프로그램을 사용한 결과에 따른 어떠한 책임도 지지 않습니다.
* KSAN 개발팀은 사전 공지, 허락, 동의 없이 KSAN 개발에 관련된 모든 결과물에 대한 LICENSE 방식을 변경 할 권리가 있습니다.
*/
using System.Data.SQLite;
using log4net;

namespace IfsSync2Common
{
	public class TaskDbManager
	{
		#region Define
		const string SQLITE_SEQUENCE = "sqlite_sequence";
		const string TASK_TABLE_NAME = "TaskList";
		const string PENDING_TABLE_NAME = "PendingList";
		const string SUCCESS_TABLE_NAME = "SuccessTaskList";
		const string FAILURE_TABLE_NAME = "FailureTaskList";
		const string INDEX_NAME = "Id";
		const string TASK_NAME = "TaskName";
		const string FILEPATH = "FilePath";
		const string NEW_FILE_PATH = "NewFilePath";
		const string SNAPSHOT_PATH = "SnapshotPath";
		const string FILE_SIZE = "FileSize";
		const string EVENT_TIME = "EventTime";
		const string UPLOAD_TIME = "UploadTime";
		const string RESULT = "Result";

		const string LOG_TABLE_NAME = "LogList";
		const string LOG_NAME = "Log";

		const int DEFAULT_LIMIT = 3000;
		#endregion

		#region Attributes
		readonly ILog _log = LogManager.GetLogger(typeof(TaskDbManager));

		readonly Mutex _sqliteMutex;
		readonly string _filePath;

		public readonly List<TaskData> TaskList = [];

		public int TaskCount => TaskList.Count;
		public long TaskSize
		{
			get
			{
				long size = 0;
				foreach (var task in TaskList) size += task.FileSize;
				return size;
			}
		}
		#endregion

		public TaskDbManager(string jobName)
		{
			try
			{
				_filePath = IfsSync2Utilities.GetDBFilePath(jobName);
				_sqliteMutex = new Mutex(false, IfsSync2Constants.MUTEX_NAME_JOB_SQL, out bool createdNew);

				if (!createdNew) _log.Debug($"Mutex({IfsSync2Constants.MUTEX_NAME_JOB_SQL})");
				else _log.Debug($"Mutex({IfsSync2Constants.MUTEX_NAME_JOB_SQL}) create");
				CreateDBFile();
			}
			catch (Exception e)
			{
				_log.Error($"Mutex({IfsSync2Constants.MUTEX_NAME_JOB_SQL}) fail", e);
				throw new InvalidOperationException($"Mutex({IfsSync2Constants.MUTEX_NAME_JOB_SQL}) fail", e);
			}
		}

		void CreateDBFile()
		{
			try
			{
				IfsSync2Utilities.CreateFile(_filePath);

				_sqliteMutex.WaitOne();
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();
				using var cmd = new SQLiteCommand(conn)
				{
					//Task List
					CommandText =
					$"Create Table IF NOT EXISTS '{TASK_TABLE_NAME}'(" +
								 $"'{INDEX_NAME}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
								 $"'{TASK_NAME}' TEXT NOT NULL, " +
								 $"'{FILEPATH}' TEXT NOT NULL, " +
								 $"'{NEW_FILE_PATH}' TEXT, " +
								 $"'{FILE_SIZE}' INTEGER DEFAULT 0," +
								 $"'{EVENT_TIME}' TEXT NOT NULL, " +
								 $"'{UPLOAD_TIME}' TEXT);" +
					//Pending List
					$"Create Table IF NOT EXISTS '{PENDING_TABLE_NAME}'(" +
								 $"'{INDEX_NAME}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
								 $"'{TASK_NAME}' TEXT NOT NULL, " +
								 $"'{FILEPATH}' TEXT NOT NULL, " +
								 $"'{NEW_FILE_PATH}' TEXT, " +
								 $"'{SNAPSHOT_PATH}' TEXT NOT NULL, " +
								 $"'{FILE_SIZE}' INTEGER DEFAULT 0," +
								 $"'{EVENT_TIME}' TEXT NOT NULL, " +
								 $"'{RESULT}' TEXT);" +
					////Success
					$"Create Table IF NOT EXISTS '{SUCCESS_TABLE_NAME}'(" +
								 $"'{INDEX_NAME}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
								 $"'{TASK_NAME}' TEXT NOT NULL, " +
								 $"'{FILEPATH}' TEXT NOT NULL, " +
								 $"'{NEW_FILE_PATH}' TEXT, " +
								 $"'{FILE_SIZE}' INTEGER DEFAULT 0," +
								 $"'{EVENT_TIME}' TEXT NOT NULL, " +
								 $"'{UPLOAD_TIME}' TEXT);" +
					//Failure
					$"Create Table IF NOT EXISTS '{FAILURE_TABLE_NAME}'(" +
								 $"'{INDEX_NAME}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
								 $"'{TASK_NAME}' TEXT NOT NULL, " +
								 $"'{FILEPATH}' TEXT NOT NULL, " +
								 $"'{NEW_FILE_PATH}' TEXT, " +
								 $"'{FILE_SIZE}' INTEGER DEFAULT 0," +
								 $"'{EVENT_TIME}' TEXT NOT NULL, " +
								 $"'{UPLOAD_TIME}' TEXT NOT NULL, " +
								 $"'{RESULT}' TEXT);" +
					//Log List
					$"Create Table IF NOT EXISTS '{LOG_TABLE_NAME}'(" +
								 $"'{INDEX_NAME}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
								 $"'{LOG_NAME}' TEXT NOT NULL);" +
							 "PRAGMA foreign_keys = On;"
				};
				cmd.ExecuteNonQuery();

				_log.Debug($"Success : {cmd.CommandText}");
			}
			catch (Exception e) { _log.Error(e); }
			finally { _sqliteMutex.ReleaseMutex(); }
		}

		public bool DeleteDBFile()
		{

			if (File.Exists(_filePath))
			{
				AllClear();
				try
				{
					_sqliteMutex.WaitOne();

					for (int i = 0; i < 5; i++)
					{
						if (new FileInfo(_filePath).Exists) File.Delete(_filePath);
						else break;
						Thread.Sleep(100);
					}

					_log.Debug($"Success : {_filePath}");

					return true;
				}
				catch (Exception e) { _log.Error(e); return false; }
				finally { _sqliteMutex.ReleaseMutex(); }
			}
			return false;
		}

		public bool Insert(TaskData task)
		{
			try
			{
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");

				_sqliteMutex.WaitOne();
				conn.Open();
				using var cmd = new SQLiteCommand(conn)
				{
					CommandText = $"INSERT INTO '{TASK_TABLE_NAME}'({TASK_NAME}, {FILEPATH}, {NEW_FILE_PATH}, {FILE_SIZE}, {EVENT_TIME})" +
															$" VALUES ('{task.TaskType}', '{task.FilePath}', '{task.NewFilePath}', {task.FileSize}, '{task.EventTime}')"
				};
				var result = cmd.ExecuteNonQuery();

				if (result > 0) { _log.Debug($"Success : {cmd.CommandText}"); return true; }
				else { _log.Error($"Failed : {cmd.CommandText}"); return false; }
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _sqliteMutex.ReleaseMutex(); }
		}

		public bool Update(TaskData task)
		{
			try
			{
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				_sqliteMutex.WaitOne();
				conn.Open();

				var query = $"DELETE FROM '{TASK_TABLE_NAME}' WHERE {INDEX_NAME} = {task.Index};";
				query += task.UploadFlag ?
				$"\nINSERT INTO '{SUCCESS_TABLE_NAME}' ( {TASK_NAME}, {FILEPATH}, {NEW_FILE_PATH}, {FILE_SIZE}, {UPLOAD_TIME} ,{EVENT_TIME} )" +
													$" VALUES('{task.TaskType}', '{task.FilePath}', '{task.NewFilePath}', {task.FileSize}, '{task.UploadTime}', '{task.EventTime}');"
				: $"\nINSERT INTO '{FAILURE_TABLE_NAME}' ( {TASK_NAME}, {FILEPATH}, {NEW_FILE_PATH}, {FILE_SIZE}, {UPLOAD_TIME} ,{EVENT_TIME}, {RESULT} )" +
													$" VALUES('{task.TaskType}', '{task.FilePath}', '{task.NewFilePath}', {task.FileSize}, '{task.UploadTime}', '{task.EventTime}', '{task.Result.Replace("'", "\"")}');";

				using var cmd = new SQLiteCommand(conn) { CommandText = query };

				int result = cmd.ExecuteNonQuery();
				if (result > 0) { _log.Debug($"Success : {cmd.CommandText}"); return true; }
				else { _log.Error($"Failed : {cmd.CommandText}"); return false; }
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _sqliteMutex.ReleaseMutex(); }
		}

		public bool Delete(TaskData task)
		{
			try
			{
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				_sqliteMutex.WaitOne();
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"DELETE FROM '{TASK_TABLE_NAME}' WHERE {INDEX_NAME} = {task.Index};" };
				int result = cmd.ExecuteNonQuery();
				if (result > 0) { _log.Debug($"Success : {cmd.CommandText}"); return true; }
				else { _log.Error($"Failed : {cmd.CommandText}"); return false; }
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _sqliteMutex.ReleaseMutex(); }
		}

		public bool Pending(TaskData task)
		{
			try
			{
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				_sqliteMutex.WaitOne();
				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{
					CommandText = $"DELETE FROM '{TASK_TABLE_NAME}' WHERE {INDEX_NAME} = {task.Index};"
								+ $"\nINSERT INTO '{FAILURE_TABLE_NAME}' ({TASK_NAME}, {FILEPATH}, {NEW_FILE_PATH}, {FILE_SIZE}, {EVENT_TIME}, {UPLOAD_TIME} ,{RESULT} )" +
																$" VALUES('{task.TaskType}', '{task.FilePath}', '{task.NewFilePath}', {task.FileSize}, '{task.EventTime}', '{task.UploadTime}', '{task.Result.Replace("'", "\"")}');",
				};

				int result = cmd.ExecuteNonQuery();
				if (result > 0) { _log.Debug($"Success : {cmd.CommandText}"); return true; }
				else { _log.Error($"Failed : {cmd.CommandText}"); return false; }
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _sqliteMutex.ReleaseMutex(); }
		}

		public List<TaskData> GetSuccessList(int startIndex = int.MaxValue, int limit = DEFAULT_LIMIT)
		{
			var tasks = new List<TaskData>();
			try
			{
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");

				_sqliteMutex.WaitOne();
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM '{SUCCESS_TABLE_NAME}' WHERE {INDEX_NAME} < {startIndex} ORDER BY {INDEX_NAME} DESC LIMIT {limit}" };

				using var rdr = cmd.ExecuteReader();
				while (rdr.Read())
				{
					var task = new TaskData
					{
						Index = rdr.GetLong(INDEX_NAME),
						StrTaskType = rdr.GetString(TASK_NAME),
						FilePath = rdr.GetString(FILEPATH),
						NewFilePath = rdr.GetString(NEW_FILE_PATH),
						FileSize = rdr.GetLong(FILE_SIZE),
						EventTime = rdr.GetString(EVENT_TIME),
						UploadTime = rdr.GetString(UPLOAD_TIME)
					};

					tasks.Add(task);
				}

				_log.Debug($"List Count : {tasks.Count}");
			}
			catch (Exception e) { _log.Error(e); }
			finally { _sqliteMutex.ReleaseMutex(); }
			return tasks;
		}
		public List<TaskData> GetFailureList(int startIndex = int.MaxValue, int limit = DEFAULT_LIMIT)
		{
			var tasks = new List<TaskData>();
			try
			{
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");

				_sqliteMutex.WaitOne();
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM '{FAILURE_TABLE_NAME}' WHERE {INDEX_NAME} < {startIndex} ORDER BY {INDEX_NAME} DESC LIMIT {limit}" };

				using var rdr = cmd.ExecuteReader();
				while (rdr.Read())
				{
					tasks.Add(new TaskData
					{
						Index = rdr.GetLong(INDEX_NAME),
						StrTaskType = rdr.GetString(TASK_NAME),
						FilePath = rdr.GetString(FILEPATH),
						NewFilePath = rdr.GetString(NEW_FILE_PATH),
						FileSize = rdr.GetLong(FILE_SIZE),
						EventTime = rdr.GetString(EVENT_TIME),
						UploadTime = rdr.GetString(UPLOAD_TIME),
						Result = rdr.GetString(RESULT)
					});
				}

				_log.Debug($"List Count : {tasks.Count}");
			}
			catch (Exception e) { _log.Error(e); }
			finally { _sqliteMutex.ReleaseMutex(); }
			return tasks;
		}
		public bool Clear()
		{
			try
			{
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");

				_sqliteMutex.WaitOne();
				conn.Open();
				using var cmd = new SQLiteCommand(conn) { CommandText = string.Format("DELETE FROM '{0}';", TASK_TABLE_NAME) };

				int result = cmd.ExecuteNonQuery();
				if (result > 0) { _log.Debug($"Success({result}) : {cmd.CommandText}"); return true; }
				else { _log.Error($"Failed({result}) : {cmd.CommandText}"); return false; }
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _sqliteMutex.ReleaseMutex(); }
		}
		public bool AllClear()
		{
			try
			{
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");

				_sqliteMutex.WaitOne();
				conn.Open();
				using var cmd = new SQLiteCommand(conn)
				{
					CommandText = $"DELETE FROM '{TASK_TABLE_NAME}';"
								+ $" DELETE FROM '{PENDING_TABLE_NAME}';"
								+ $" DELETE FROM '{SUCCESS_TABLE_NAME}';"
								+ $" DELETE FROM '{FAILURE_TABLE_NAME}';"
								+ $" DELETE FROM '{LOG_TABLE_NAME}';"
								+ $" DELETE FROM '{SQLITE_SEQUENCE}';"
				};

				int result = cmd.ExecuteNonQuery();
				if (result > 0) { _log.Debug($"Success({result}) : {cmd.CommandText}"); return true; }
				else { _log.Error($"Failed({result}) : {cmd.CommandText}"); return false; }
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _sqliteMutex.ReleaseMutex(); }
		}
		public bool GetList(int limit = 3000)
		{
			try
			{
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");

				_sqliteMutex.WaitOne();
				conn.Open();

				TaskList.Clear();

				using var cmd = new SQLiteCommand(conn)
				{ CommandText = $"SELECT * FROM '{TASK_TABLE_NAME}' Limit {limit}" };

				using var rdr = cmd.ExecuteReader();
				while (rdr.Read())
				{
					TaskList.Add(new TaskData
					{
						Index = rdr.GetLong(INDEX_NAME),
						StrTaskType = rdr.GetString(TASK_NAME),
						FilePath = rdr.GetString(FILEPATH),
						NewFilePath = rdr.GetString(NEW_FILE_PATH),
						FileSize = rdr.GetLong(FILE_SIZE),
						EventTime = rdr.GetString(EVENT_TIME),
						UploadTime = rdr.GetString(UPLOAD_TIME)
					});
				}

				_log.Debug($"List Count : {TaskList.Count}");
				return true;
			}
			catch (Exception e) { _log.Error($"GetList({_filePath}, {limit})", e); return false; }
			finally { _sqliteMutex.ReleaseMutex(); }
		}

		public bool InsertLog(string msg)
		{
			try
			{
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");

				_sqliteMutex.WaitOne();
				conn.Open();
				using var cmd = new SQLiteCommand(conn) { CommandText = $"INSERT INTO '{LOG_TABLE_NAME}' ({LOG_NAME}) VALUES ('[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}')" };
				int result = cmd.ExecuteNonQuery();
				if (result > 0)
				{
					_log.Debug($"Success({result}) : {cmd.CommandText}");
					return true;
				}
				else
				{
					_log.Error($"Failed({result}) : {cmd.CommandText}");
					return false;
				}
			}
			catch (Exception e)
			{
				_log.Error(e);
				return false;
			}
			finally
			{
				_sqliteMutex.ReleaseMutex();
			}
		}
		public List<string> GetLog(int startIndex = int.MaxValue)
		{
			try
			{
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");

				_sqliteMutex.WaitOne();
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM {LOG_TABLE_NAME} WHERE {INDEX_NAME} < {startIndex} ORDER BY {INDEX_NAME} DESC LIMIT 50000;" };
				using var rdr = cmd.ExecuteReader();

				var items = new List<string>();
				while (rdr.Read())
				{
					items.Add(rdr.GetString(LOG_NAME));
				}

				_log.Debug($"List Count : {items.Count}");
				return items;
			}
			catch (Exception e)
			{
				_log.Error($"GetLog({_filePath}, {startIndex})", e);
				return [];
			}
			finally
			{
				_sqliteMutex.ReleaseMutex();
			}
		}

		public bool DeleteOldLogs(int days = IfsSync2Constants.DEFAULT_DELETE_DATE)
		{
			try
			{
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"DELETE FROM {LOG_TABLE_NAME} WHERE {LOG_NAME} < '{DateTime.Now.AddDays(-days):yyyy-MM-dd HH:mm:ss}';" };
				int result = cmd.ExecuteNonQuery();
				if (result > 0) { _log.Debug($"Success({result}) : {cmd.CommandText}"); return true; }
				else { _log.Error($"Failed({result}) : {cmd.CommandText}"); return false; }
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _sqliteMutex.ReleaseMutex(); }
		}
	}
}
