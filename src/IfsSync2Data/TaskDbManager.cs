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
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using log4net;
using System.Reflection;
using System.Threading;

namespace IfsSync2Data
{
	class TaskDbManager
	{
		#region Define
		const string SQLITE_SEQUENCE = "sqlite_sequence";
		const string STR_TASK_TABLE_NAME = "TaskList";
		const string STR_PENDING_TABLE_NAME = "PendingList";
		const string STR_SUCCESS_TABLE_NAME = "SuccessTaskList";
		const string STR_FAILURE_TABLE_NAME = "FailureTaskList";
		const string STR_INDEX_NAME = "Id";
		const string STR_TASK_NAME = "TaskName";
		const string STR_FILEPATH = "FilePath";
		const string STR_NEW_FILE_PATH = "NewFilePath";
		const string STR_SNAPSHOT_PATH = "SnapshotPath";
		const string STR_FILE_SIZE = "FileSize";
		const string STR_EVENT_TIME = "EventTime";
		const string STR_UPLOAD_TIME = "UploadTime";
		const string STR_RESULT = "Result";

		const string STR_LOG_TABLE_NAME = "LogList";
		const string STR_LOG_NAME = "Log";

		const int DEFAULT_LIMIT = 3000;
		#endregion

		#region Attributes
		readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

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
				_filePath = MainData.GetDBFilePath(jobName);
				_sqliteMutex = new Mutex(false, MainData.MUTEX_NAME_JOB_SQL, out bool createdNew);

				if (!createdNew) _log.Debug($"Mutex({MainData.MUTEX_NAME_JOB_SQL})");
				else _log.Debug($"Mutex({MainData.MUTEX_NAME_JOB_SQL}) create");
				CreateDBFile();
			}
			catch (Exception e)
			{
				_log.Error($"Mutex({MainData.MUTEX_NAME_JOB_SQL}) fail", e);
			}
		}

		void CreateDBFile()
		{
			try
			{
				MainData.CreateFile(_filePath);

				_sqliteMutex.WaitOne();
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();
				using var cmd = new SQLiteCommand(conn)
				{
					//Task List
					CommandText =
					$"Create Table IF NOT EXISTS '{STR_TASK_TABLE_NAME}'(" +
								 $"'{STR_INDEX_NAME}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
								 $"'{STR_TASK_NAME}' TEXT NOT NULL, " +
								 $"'{STR_FILEPATH}' TEXT NOT NULL, " +
								 $"'{STR_NEW_FILE_PATH}' TEXT, " +
								 $"'{STR_FILE_SIZE}' INTEGER DEFAULT 0," +
								 $"'{STR_EVENT_TIME}' TEXT NOT NULL, " +
								 $"'{STR_UPLOAD_TIME}' TEXT);" +
					//Pending List
					$"Create Table IF NOT EXISTS '{STR_PENDING_TABLE_NAME}'(" +
								 $"'{STR_INDEX_NAME}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
								 $"'{STR_TASK_NAME}' TEXT NOT NULL, " +
								 $"'{STR_FILEPATH}' TEXT NOT NULL, " +
								 $"'{STR_NEW_FILE_PATH}' TEXT, " +
								 $"'{STR_SNAPSHOT_PATH}' TEXT NOT NULL, " +
								 $"'{STR_FILE_SIZE}' INTEGER DEFAULT 0," +
								 $"'{STR_EVENT_TIME}' TEXT NOT NULL, " +
								 $"'{STR_RESULT}' TEXT);" +
					////Success
					$"Create Table IF NOT EXISTS '{STR_SUCCESS_TABLE_NAME}'(" +
								 $"'{STR_INDEX_NAME}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
								 $"'{STR_TASK_NAME}' TEXT NOT NULL, " +
								 $"'{STR_FILEPATH}' TEXT NOT NULL, " +
								 $"'{STR_NEW_FILE_PATH}' TEXT, " +
								 $"'{STR_FILE_SIZE}' INTEGER DEFAULT 0," +
								 $"'{STR_EVENT_TIME}' TEXT NOT NULL, " +
								 $"'{STR_UPLOAD_TIME}' TEXT);" +
					//Failure
					$"Create Table IF NOT EXISTS '{STR_FAILURE_TABLE_NAME}'(" +
								 $"'{STR_INDEX_NAME}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
								 $"'{STR_TASK_NAME}' TEXT NOT NULL, " +
								 $"'{STR_FILEPATH}' TEXT NOT NULL, " +
								 $"'{STR_NEW_FILE_PATH}' TEXT, " +
								 $"'{STR_FILE_SIZE}' INTEGER DEFAULT 0," +
								 $"'{STR_EVENT_TIME}' TEXT NOT NULL, " +
								 $"'{STR_UPLOAD_TIME}' TEXT NOT NULL, " +
								 $"'{STR_RESULT}' TEXT);" +
					//Log List
					$"Create Table IF NOT EXISTS '{STR_LOG_TABLE_NAME}'(" +
								 $"'{STR_INDEX_NAME}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
								 $"'{STR_LOG_NAME}' TEXT NOT NULL);" +
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
					CommandText = $"INSERT INTO '{STR_TASK_TABLE_NAME}'({STR_TASK_NAME}, {STR_FILEPATH}, {STR_NEW_FILE_PATH}, {STR_FILE_SIZE}, {STR_EVENT_TIME})" +
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

				var query = $"DELETE FROM '{STR_TASK_TABLE_NAME}' WHERE {STR_INDEX_NAME} = {task.Index};";
				query += task.UploadFlag ?
				$"\nINSERT INTO '{STR_SUCCESS_TABLE_NAME}' ( {STR_TASK_NAME}, {STR_FILEPATH}, {STR_NEW_FILE_PATH}, {STR_FILE_SIZE}, {STR_UPLOAD_TIME} ,{STR_EVENT_TIME} )" +
													$" VALUES('{task.TaskType}', '{task.FilePath}', '{task.NewFilePath}', {task.FileSize}, '{task.UploadTime}', '{task.EventTime}');"
				: $"\nINSERT INTO '{STR_FAILURE_TABLE_NAME}' ( {STR_TASK_NAME}, {STR_FILEPATH}, {STR_NEW_FILE_PATH}, {STR_FILE_SIZE}, {STR_UPLOAD_TIME} ,{STR_EVENT_TIME}, {STR_RESULT} )" +
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

				using var cmd = new SQLiteCommand(conn) { CommandText = $"DELETE FROM '{STR_TASK_TABLE_NAME}' WHERE {STR_INDEX_NAME} = {task.Index};" };
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
					CommandText = $"DELETE FROM '{STR_TASK_TABLE_NAME}' WHERE {STR_INDEX_NAME} = {task.Index};"
								+ $"\nINSERT INTO '{STR_FAILURE_TABLE_NAME}' ({STR_TASK_NAME}, {STR_FILEPATH}, {STR_NEW_FILE_PATH}, {STR_FILE_SIZE}, {STR_EVENT_TIME}, {STR_UPLOAD_TIME} ,{STR_RESULT} )" +
																$" VALUES('{task.TaskType}', '{task.FilePath}', '{task.NewFilePath}', {task.FileSize}, '{task.EventTime}', '{task.UploadTime}', '{task.Result.Replace("'", "\"")}');",
				};

				int result = cmd.ExecuteNonQuery();
				if (result > 0) { _log.Debug($"Success : {cmd.CommandText}"); return true; }
				else { _log.Error($"Failed : {cmd.CommandText}"); return false; }
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _sqliteMutex.ReleaseMutex(); }
		}

		public List<TaskData> GetSuccessList(int startIndex = 0, int limit = DEFAULT_LIMIT)
		{
			var tasks = new List<TaskData>();
			try
			{
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");

				_sqliteMutex.WaitOne();
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM '{STR_SUCCESS_TABLE_NAME}' Limit {limit} OFFSET {startIndex}" };

				using var rdr = cmd.ExecuteReader();
				while (rdr.Read())
				{
					var task = new TaskData
					{
						Index = Convert.ToInt64(rdr[STR_INDEX_NAME]),
						StrTaskType = rdr[STR_TASK_NAME].ToString(),
						FilePath = rdr[STR_FILEPATH].ToString(),
						NewFilePath = rdr[STR_NEW_FILE_PATH].ToString(),
						FileSize = Convert.ToInt64(rdr[STR_FILE_SIZE]),
						EventTime = rdr[STR_EVENT_TIME].ToString(),
						UploadTime = rdr[STR_UPLOAD_TIME].ToString()
					};

					tasks.Add(task);
				}

				_log.Debug($"List Count : {tasks.Count}");
			}
			catch (Exception e) { _log.Error(e); }
			finally { _sqliteMutex.ReleaseMutex(); }
			return tasks;
		}
		public List<TaskData> GetFailureList(int startIndex = 0, int limit = DEFAULT_LIMIT)
		{
			var tasks = new List<TaskData>();
			try
			{
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");

				_sqliteMutex.WaitOne();
				conn.Open();


				using var cmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM '{STR_FAILURE_TABLE_NAME}' Limit {limit} OFFSET {startIndex}" };

				using var rdr = cmd.ExecuteReader();
				while (rdr.Read())
				{
					tasks.Add(new TaskData
					{
						Index = Convert.ToInt64(rdr[STR_INDEX_NAME]),
						StrTaskType = rdr[STR_TASK_NAME].ToString(),
						FilePath = rdr[STR_FILEPATH].ToString(),
						NewFilePath = rdr[STR_NEW_FILE_PATH].ToString(),
						FileSize = Convert.ToInt64(rdr[STR_FILE_SIZE]),
						EventTime = rdr[STR_EVENT_TIME].ToString(),
						UploadTime = rdr[STR_UPLOAD_TIME].ToString(),
						Result = rdr[STR_RESULT].ToString()
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
				using var cmd = new SQLiteCommand(conn) { CommandText = string.Format("DELETE FROM '{0}';", STR_TASK_TABLE_NAME) };

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
					CommandText = $"DELETE FROM '{STR_TASK_TABLE_NAME}';"
								+ $" DELETE FROM '{STR_PENDING_TABLE_NAME}';"
								+ $" DELETE FROM '{STR_SUCCESS_TABLE_NAME}';"
								+ $" DELETE FROM '{STR_FAILURE_TABLE_NAME}';"
								+ $" DELETE FROM '{STR_LOG_TABLE_NAME}';"
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
				{ CommandText = $"SELECT * FROM '{STR_TASK_TABLE_NAME}' Limit {limit}" };

				using var rdr = cmd.ExecuteReader();
				while (rdr.Read())
				{
					TaskList.Add(new TaskData
					{
						Index = Convert.ToInt64(rdr[STR_INDEX_NAME]),
						StrTaskType = rdr[STR_TASK_NAME].ToString(),
						FilePath = rdr[STR_FILEPATH].ToString(),
						NewFilePath = rdr[STR_NEW_FILE_PATH].ToString(),
						FileSize = Convert.ToInt64(rdr[STR_FILE_SIZE]),
						EventTime = rdr[STR_EVENT_TIME].ToString(),
						UploadTime = rdr[STR_UPLOAD_TIME].ToString()
					});
				}

				_log.Debug($"List Count : {TaskList.Count}");
				return true;
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _sqliteMutex.ReleaseMutex(); }
		}

		public bool InsertLog(string msg)
		{
			try
			{
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");

				_sqliteMutex.WaitOne();
				conn.Open();
				using var cmd = new SQLiteCommand(conn) { CommandText = $"INSERT INTO '{STR_LOG_TABLE_NAME}' ({STR_LOG_NAME}) VALUES ('[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}')" };
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
		public List<string> GetLog(long startIndex = 0)
		{
			try
			{
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");

				_sqliteMutex.WaitOne();
				conn.Open();


				using var cmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM {STR_LOG_TABLE_NAME} LIMIT 50000 OFFSET {startIndex};" };
				using var rdr = cmd.ExecuteReader();

				var items = new List<string>();
				while (rdr.Read())
				{
					items.Add(rdr[STR_LOG_NAME].ToString());
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
	}
}
