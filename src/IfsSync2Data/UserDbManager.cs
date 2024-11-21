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
using log4net;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Threading;

namespace IfsSync2Data
{
	public class UserDbManager
	{
		#region Define
		const string CONNECTION_FAILED = "SQLiteConnection fail";
		const string STR_NORMAL_USER_TABLE_NAME = "NormalUserList";
		const string STR_GLOBAL_USER_TABLE_NAME = "GlobalUserList";
		const string STR_USER_ID = "Id";
		const string STR_USER_HOSTNAME = "HostName";
		const string STR_USER_USERNAME = "UserName";
		const string STR_USER_URL = "URL";
		const string STR_USER_ACCESS_KEY = "AccessKey";
		const string STR_USER_SECRET_KEY = "AccessSecret";
		const string STR_USER_STORAGE_NAME = "StorageName";
		const string STR_S3_FILE_MANAGER_URL = "S3FileManagerURL";
		const string STR_USER_DEBUG = "Debug";
		const string STR_USER_UPDATE_FLAG = "UpdateFlag";
		#endregion

		static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		readonly string _filePath = MainData.GetDBFilePath(MainData.USER_DB_FILE_NAME);
		readonly Mutex _mutex;


		public UserDbManager()
		{
			try
			{
				_mutex = new Mutex(false, MainData.MUTEX_NAME_USER_SQL, out bool createdNew);

				if (!createdNew) log.Debug($"Mutex({MainData.MUTEX_NAME_USER_SQL})");
				else log.Debug($"Mutex({MainData.MUTEX_NAME_USER_SQL}) create");
			}
			catch (Exception e)
			{
				log.Error($"Mutex({MainData.MUTEX_NAME_USER_SQL})", e);
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
					//Global User List
					$"Create Table IF NOT EXISTS '{STR_GLOBAL_USER_TABLE_NAME}'(" +
								 $"'{STR_USER_ID}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT," +
								 $"'{STR_USER_HOSTNAME}' TEXT, " +
								 $"'{STR_USER_USERNAME}' TEXT NOT NULL, " +
								 $"'{STR_USER_URL}' TEXT NOT NULL, " +
								 $"'{STR_USER_ACCESS_KEY}' TEXT NOT NULL, " +
								 $"'{STR_USER_SECRET_KEY}' TEXT NOT NULL, " +
								 $"'{STR_USER_STORAGE_NAME}' TEXT NOT NULL, " +
								 $"'{STR_S3_FILE_MANAGER_URL}' TEXT, " +
								 $"'{STR_USER_DEBUG}' BOOL NOT NULL DEFAULT TRUE, " +
								 $"'{STR_USER_UPDATE_FLAG}' BOOL NOT NULL DEFAULT FALSE);" +
					//User List
					$"Create Table IF NOT EXISTS '{STR_NORMAL_USER_TABLE_NAME}'(" +
								 $"'{STR_USER_ID}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT," +
								 $"'{STR_USER_HOSTNAME}' TEXT, " +
								 $"'{STR_USER_USERNAME}' TEXT NOT NULL, " +
								 $"'{STR_USER_URL}' TEXT NOT NULL, " +
								 $"'{STR_USER_ACCESS_KEY}' TEXT NOT NULL, " +
								 $"'{STR_USER_SECRET_KEY}' TEXT NOT NULL, " +
								 $"'{STR_USER_STORAGE_NAME}' TEXT NOT NULL, " +
								 $"'{STR_S3_FILE_MANAGER_URL}' TEXT, " +
								 $"'{STR_USER_DEBUG}' BOOL NOT NULL DEFAULT TRUE, " +
								 $"'{STR_USER_UPDATE_FLAG}' BOOL NOT NULL DEFAULT FALSE);"
				};
				cmd.ExecuteNonQuery();
				log.Debug($"Success : {cmd.CommandText}");
				return true;
			}
			catch (Exception e) { log.Error("Create DB Failed.", e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}

		public bool InsertUser(UserData user, bool global)
		{
			try
			{
				var tableName = global ? STR_GLOBAL_USER_TABLE_NAME : STR_NORMAL_USER_TABLE_NAME;
				if (!File.Exists(_filePath) && !CreateDBFile()) return false;

				_mutex.WaitOne();
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");

				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{
					CommandText = $"INSERT INTO '{tableName}'( {STR_USER_HOSTNAME}, {STR_USER_USERNAME}, {STR_USER_URL}, {STR_USER_ACCESS_KEY}, {STR_USER_SECRET_KEY}, {STR_USER_STORAGE_NAME}, {STR_S3_FILE_MANAGER_URL}, {STR_USER_DEBUG}, {STR_USER_UPDATE_FLAG} )" +
									$" VALUES ('{user.HostName}', '{user.UserName}', '{user.URL}', '{user.AccessKey}', '{user.SecretKey}', '{user.StorageName}', '{user.S3FileManagerURL}', {user.Debug}, {user.UpdateFlag} );"
				};

				int result = cmd.ExecuteNonQuery();
				if (result > 0) { log.Debug($"Success({result}): {cmd.CommandText}"); return true; }
				else { log.Error($"Failed({result}) : {cmd.CommandText}"); return false; }
			}
			catch (Exception e) { log.Error("Insert User Failed.", e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}

		public List<UserData> GetUsers(bool global)
		{
			var Items = new List<UserData>();
			try
			{
				var tableName = global ? STR_GLOBAL_USER_TABLE_NAME : STR_NORMAL_USER_TABLE_NAME;
				if (!File.Exists(_filePath)) CreateDBFile();


				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM '{tableName}'" };
				using var Rdr = cmd.ExecuteReader();

				while (Rdr.Read())
				{
					Items.Add(new UserData
					{
						Id = Convert.ToInt32(Rdr[STR_USER_ID]),
						HostName = Rdr[STR_USER_HOSTNAME].ToString(),
						UserName = Rdr[STR_USER_USERNAME].ToString(),
						URL = Rdr[STR_USER_URL].ToString(),
						AccessKey = Rdr[STR_USER_ACCESS_KEY].ToString(),
						SecretKey = Rdr[STR_USER_SECRET_KEY].ToString(),
						StorageName = Rdr[STR_USER_STORAGE_NAME].ToString(),
						S3FileManagerURL = Rdr[STR_S3_FILE_MANAGER_URL].ToString(),
						Debug = Convert.ToBoolean(Rdr[STR_USER_DEBUG]),
						UpdateFlag = Convert.ToBoolean(Rdr[STR_USER_UPDATE_FLAG]),
					});
				}
				log.Debug($"{tableName} Count : {Items.Count}");
			}
			catch (Exception e) { log.Error(e); }
			finally { _mutex.ReleaseMutex(); }
			return Items;
		}
		public List<UserData> GetUsers(string hostName)
		{
			var items = new List<UserData>();
			try
			{
				if (!File.Exists(_filePath)) CreateDBFile();

				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{
					CommandText = $"SELECT * FROM '{STR_NORMAL_USER_TABLE_NAME}' WHERE {STR_USER_HOSTNAME} = '{hostName}';"
				};
				using var rdr = cmd.ExecuteReader();

				while (rdr.Read())
				{
					items.Add(new UserData
					{
						Id = Convert.ToInt32(rdr[STR_USER_ID]),
						HostName = rdr[STR_USER_HOSTNAME].ToString(),
						UserName = rdr[STR_USER_USERNAME].ToString(),
						URL = rdr[STR_USER_URL].ToString(),
						AccessKey = rdr[STR_USER_ACCESS_KEY].ToString(),
						SecretKey = rdr[STR_USER_SECRET_KEY].ToString(),
						StorageName = rdr[STR_USER_STORAGE_NAME].ToString(),
						S3FileManagerURL = rdr[STR_S3_FILE_MANAGER_URL].ToString(),
						Debug = Convert.ToBoolean(rdr[STR_USER_DEBUG]),
						UpdateFlag = Convert.ToBoolean(rdr[STR_USER_UPDATE_FLAG]),
					});
				}
				log.Debug($"Count({hostName}) : {items.Count}");
			}
			catch (Exception e) { log.Error(e); }
			finally { _mutex.ReleaseMutex(); }
			return items;
		}
		public bool IsUserName(string userName, bool global)
		{
			try
			{
				var tableName = global ? STR_GLOBAL_USER_TABLE_NAME : STR_NORMAL_USER_TABLE_NAME;
				if (!File.Exists(_filePath)) CreateDBFile();
				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{
					CommandText = $"SELECT count(id) FROM {tableName} WHERE {STR_USER_USERNAME} = '{userName}';"
				};
				int result = Convert.ToInt32(cmd.ExecuteScalar());

				if (result > 0)
				{
					log.Debug($"Exists User({result}) : {userName}");
					return true;
				}
				else
				{
					log.Debug($"Not Exists User({result}) : {cmd.CommandText}");
					return false;
				}
			}
			catch (Exception e) { log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}

		public bool DeleteUserToId(int id, bool global)
		{
			try
			{
				var tableName = global ? STR_GLOBAL_USER_TABLE_NAME : STR_NORMAL_USER_TABLE_NAME;
				var file = new FileInfo(_filePath);
				if (!file.Exists)
				{
					log.Error($"{_filePath} Not Exists");
					return false;
				}

				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{
					CommandText = string.Format("DELETE FROM '{0}' WHERE {1} = {2}",
												tableName, STR_USER_ID, id)
				};
				int result = cmd.ExecuteNonQuery();

				if (result > 0) { log.Debug($"Delete Success UserID : {id}"); return true; }
				else { log.Error($"Delete Fail UserID : {id}"); return false; }
			}
			catch (Exception e) { log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}
		public bool UpdateUser(UserData Data, bool global)
		{
			try
			{
				var tableName = global ? STR_GLOBAL_USER_TABLE_NAME : STR_NORMAL_USER_TABLE_NAME;
				var file = new FileInfo(_filePath);
				if (!file.Exists)
				{
					log.Error($"{_filePath} Not Exists");
					return false;
				}
				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{
					CommandText = $"UPDATE '{tableName}' SET {STR_USER_URL} = '{Data.URL}', " +
										$"{STR_USER_ACCESS_KEY} = '{Data.AccessKey}', " +
										$"{STR_USER_SECRET_KEY} = '{Data.SecretKey}', " +
										$"{STR_USER_STORAGE_NAME} = '{Data.StorageName}', " +
										$"{STR_S3_FILE_MANAGER_URL} = '{Data.S3FileManagerURL}', " +
										$"{STR_USER_DEBUG} = {Data.Debug}, " +
										$"{STR_USER_UPDATE_FLAG} = true " +
										$"WHERE {STR_USER_ID} = '{Data.Id}';"
				};

				int result = cmd.ExecuteNonQuery();

				if (result > 0) { log.Debug($"Update Success User : {Data.UserName}"); return true; }
				else { log.Error($"Update Fail User : {Data.UserName}"); return false; }
			}
			catch (Exception e) { log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}
		public bool UpdateUserStorageName(int id, string storageName, bool global)
		{
			try
			{
				var tableName = global ? STR_GLOBAL_USER_TABLE_NAME : STR_NORMAL_USER_TABLE_NAME;
				var file = new FileInfo(_filePath);
				if (!file.Exists)
				{
					log.Error($"{_filePath} Not Exists");
					return false;
				}
				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{
					CommandText = $"UPDATE '{tableName}' SET {STR_USER_STORAGE_NAME} = '{storageName}' WHERE {STR_USER_ID} = '{id}';"
				};

				int result = cmd.ExecuteNonQuery();

				if (result > 0) { log.Debug($"Update Success User : {storageName}"); return true; }
				else { log.Error($"Update Fail User : {storageName}"); return false; }
			}
			catch (Exception e) { log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}
		public bool UpdateUserS3Proxy(int id, string s3Proxy, bool global)
		{
			try
			{
				var tableName = global ? STR_GLOBAL_USER_TABLE_NAME : STR_NORMAL_USER_TABLE_NAME;
				var file = new FileInfo(_filePath);
				if (!file.Exists)
				{
					log.Error($"{_filePath} Not Exists");
					return false;
				}
				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{
					CommandText = $"UPDATE '{tableName}' SET {STR_USER_URL} = '{s3Proxy}' WHERE {STR_USER_ID} = '{id}';"
				};

				int result = cmd.ExecuteNonQuery();

				if (result > 0) { log.Debug($"Update Success User : {s3Proxy}"); return true; }
				else { log.Error($"Update Fail User : {s3Proxy}"); return false; }
			}
			catch (Exception e) { log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}
		public bool UpdateUserS3FileManagerURL(int id, string S3FileManagerURL, bool global)
		{
			try
			{
				var tableName = global ? STR_GLOBAL_USER_TABLE_NAME : STR_NORMAL_USER_TABLE_NAME;
				var file = new FileInfo(_filePath);
				if (!file.Exists)
				{
					log.Error($"{_filePath} Not Exists");
					return false;
				}
				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{
					CommandText = $"UPDATE '{tableName}' SET {STR_S3_FILE_MANAGER_URL} = '{S3FileManagerURL}`'WHERE {STR_USER_ID} = '{id}';"
				};

				int result = cmd.ExecuteNonQuery();

				if (result > 0) { log.Debug($"Update Success User : {S3FileManagerURL}"); return true; }
				else { log.Error($"Update Fail User : {S3FileManagerURL}"); return false; }
			}
			catch (Exception e) { log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}

		public bool UpdateUserCheck(UserData Data, bool global)
		{
			try
			{
				var tableName = global ? STR_GLOBAL_USER_TABLE_NAME : STR_NORMAL_USER_TABLE_NAME;
				var file = new FileInfo(_filePath);
				if (!file.Exists)
				{
					log.Error($"{_filePath} Not Exists");
					return false;
				}
				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{
					CommandText = string.Format(
								"UPDATE '{0}' SET {1} ='{2}' " +
										 "WHERE {3} = {4};",
								tableName,
								STR_USER_UPDATE_FLAG, false,
								STR_USER_ID, Data.Id)
				};

				int result = cmd.ExecuteNonQuery();

				if (result > 0) { log.Debug($"Delete Success User : {Data.UserName}"); return true; }
				else { log.Error($"Delete Fail User : {Data.UserName}"); return false; }
			}
			catch (Exception e) { log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}
	}
}
