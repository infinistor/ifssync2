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
using System.Data.SQLite;

namespace IfsSync2Common
{
	public class UserDbManager
	{
		static readonly ILog log = LogManager.GetLogger(typeof(UserDbManager));
		readonly string _filePath = IfsSync2Utilities.GetDBFilePath(IfsSync2Constants.USER_DB_FILE_NAME);
		readonly Mutex _mutex;


		public UserDbManager()
		{
			try
			{
				_mutex = new Mutex(false, IfsSync2Constants.MUTEX_NAME_USER_SQL, out bool createdNew);

				if (!createdNew) log.Debug($"Mutex({IfsSync2Constants.MUTEX_NAME_USER_SQL})");
				else log.Debug($"Mutex({IfsSync2Constants.MUTEX_NAME_USER_SQL}) create");
			}
			catch (Exception e)
			{
				log.Error($"Mutex({IfsSync2Constants.MUTEX_NAME_USER_SQL})", e);
				throw new InvalidOperationException($"Mutex({IfsSync2Constants.MUTEX_NAME_USER_SQL})", e);
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
					//Global User List
					$"Create Table IF NOT EXISTS '{IfsSync2Constants.DB_TABLE_GLOBAL_USER_LIST}'(" +
								 $"'{IfsSync2Constants.DB_FIELD_ID}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT," +
								 $"'{IfsSync2Constants.DB_FIELD_HOSTNAME}' TEXT, " +
								 $"'{IfsSync2Constants.DB_FIELD_USERNAME}' TEXT NOT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_URL}' TEXT NOT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_ACCESS_KEY}' TEXT NOT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_SECRET_KEY}' TEXT NOT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_STORAGE_NAME}' TEXT NOT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_S3_FILE_MANAGER_URL}' TEXT, " +
								 $"'{IfsSync2Constants.DB_FIELD_DEBUG}' BOOL NOT NULL DEFAULT TRUE, " +
								 $"'{IfsSync2Constants.DB_FIELD_UPDATE_FLAG}' BOOL NOT NULL DEFAULT FALSE);" +
					//User List
					$"Create Table IF NOT EXISTS '{IfsSync2Constants.DB_TABLE_NORMAL_USER_LIST}'(" +
								 $"'{IfsSync2Constants.DB_FIELD_ID}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT," +
								 $"'{IfsSync2Constants.DB_FIELD_HOSTNAME}' TEXT, " +
								 $"'{IfsSync2Constants.DB_FIELD_USERNAME}' TEXT NOT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_URL}' TEXT NOT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_ACCESS_KEY}' TEXT NOT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_SECRET_KEY}' TEXT NOT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_STORAGE_NAME}' TEXT NOT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_S3_FILE_MANAGER_URL}' TEXT, " +
								 $"'{IfsSync2Constants.DB_FIELD_DEBUG}' BOOL NOT NULL DEFAULT TRUE, " +
								 $"'{IfsSync2Constants.DB_FIELD_UPDATE_FLAG}' BOOL NOT NULL DEFAULT FALSE);"
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
				var tableName = global ? IfsSync2Constants.DB_TABLE_GLOBAL_USER_LIST : IfsSync2Constants.DB_TABLE_NORMAL_USER_LIST;
				if (!File.Exists(_filePath) && !CreateDBFile()) return false;

				_mutex.WaitOne();
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");

				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{
					CommandText = $"INSERT INTO '{tableName}'( {IfsSync2Constants.DB_FIELD_HOSTNAME}, {IfsSync2Constants.DB_FIELD_USERNAME}, {IfsSync2Constants.DB_FIELD_URL}, {IfsSync2Constants.DB_FIELD_ACCESS_KEY}, {IfsSync2Constants.DB_FIELD_SECRET_KEY}, {IfsSync2Constants.DB_FIELD_STORAGE_NAME}, {IfsSync2Constants.DB_FIELD_S3_FILE_MANAGER_URL}, {IfsSync2Constants.DB_FIELD_DEBUG}, {IfsSync2Constants.DB_FIELD_UPDATE_FLAG} )" +
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
			var items = new List<UserData>();
			try
			{
				var tableName = global ? IfsSync2Constants.DB_TABLE_GLOBAL_USER_LIST : IfsSync2Constants.DB_TABLE_NORMAL_USER_LIST;
				if (!File.Exists(_filePath)) CreateDBFile();


				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM '{tableName}'" };
				using var rdr = cmd.ExecuteReader();

				while (rdr.Read())
				{
					items.Add(new UserData
					{
						Id = rdr.GetInt(IfsSync2Constants.DB_FIELD_ID),
						HostName = rdr.GetString(IfsSync2Constants.DB_FIELD_HOSTNAME),
						UserName = rdr.GetString(IfsSync2Constants.DB_FIELD_USERNAME),
						URL = rdr.GetString(IfsSync2Constants.DB_FIELD_URL),
						AccessKey = rdr.GetString(IfsSync2Constants.DB_FIELD_ACCESS_KEY),
						SecretKey = rdr.GetString(IfsSync2Constants.DB_FIELD_SECRET_KEY),
						StorageName = rdr.GetString(IfsSync2Constants.DB_FIELD_STORAGE_NAME),
						S3FileManagerURL = rdr.GetString(IfsSync2Constants.DB_FIELD_S3_FILE_MANAGER_URL),
						Debug = rdr.GetBool(IfsSync2Constants.DB_FIELD_DEBUG),
						UpdateFlag = rdr.GetBool(IfsSync2Constants.DB_FIELD_UPDATE_FLAG),
					});
				}
				log.Debug($"{tableName} Count : {items.Count}");
			}
			catch (Exception e) { log.Error(e); }
			finally { _mutex.ReleaseMutex(); }
			return items;
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
					CommandText = $"SELECT * FROM '{IfsSync2Constants.DB_TABLE_NORMAL_USER_LIST}' WHERE {IfsSync2Constants.DB_FIELD_HOSTNAME} = '{hostName}';"
				};
				using var rdr = cmd.ExecuteReader();

				while (rdr.Read())
				{
					items.Add(new UserData
					{
						Id = rdr.GetInt(IfsSync2Constants.DB_FIELD_ID),
						HostName = rdr.GetString(IfsSync2Constants.DB_FIELD_HOSTNAME),
						UserName = rdr.GetString(IfsSync2Constants.DB_FIELD_USERNAME),
						URL = rdr.GetString(IfsSync2Constants.DB_FIELD_URL),
						AccessKey = rdr.GetString(IfsSync2Constants.DB_FIELD_ACCESS_KEY),
						SecretKey = rdr.GetString(IfsSync2Constants.DB_FIELD_SECRET_KEY),
						StorageName = rdr.GetString(IfsSync2Constants.DB_FIELD_STORAGE_NAME),
						S3FileManagerURL = rdr.GetString(IfsSync2Constants.DB_FIELD_S3_FILE_MANAGER_URL),
						Debug = rdr.GetBool(IfsSync2Constants.DB_FIELD_DEBUG),
						UpdateFlag = rdr.GetBool(IfsSync2Constants.DB_FIELD_UPDATE_FLAG),
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
				var tableName = global ? IfsSync2Constants.DB_TABLE_GLOBAL_USER_LIST : IfsSync2Constants.DB_TABLE_NORMAL_USER_LIST;
				if (!File.Exists(_filePath)) CreateDBFile();
				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{
					CommandText = $"SELECT count(id) FROM {tableName} WHERE {IfsSync2Constants.DB_FIELD_USERNAME} = '{userName}';"
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
				var tableName = global ? IfsSync2Constants.DB_TABLE_GLOBAL_USER_LIST : IfsSync2Constants.DB_TABLE_NORMAL_USER_LIST;
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
												tableName, IfsSync2Constants.DB_FIELD_ID, id)
				};
				int result = cmd.ExecuteNonQuery();

				if (result > 0) { log.Debug($"Delete Success UserID : {id}"); return true; }
				else { log.Error($"Delete Fail UserID : {id}"); return false; }
			}
			catch (Exception e) { log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}

		public bool UpdateUser(UserData data, bool global)
		{
			try
			{
				var tableName = global ? IfsSync2Constants.DB_TABLE_GLOBAL_USER_LIST : IfsSync2Constants.DB_TABLE_NORMAL_USER_LIST;
				if (!File.Exists(_filePath)) return false;

				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{
					CommandText = $"UPDATE '{tableName}' SET " +
									$"{IfsSync2Constants.DB_FIELD_HOSTNAME} = '{data.HostName}', " +
									$"{IfsSync2Constants.DB_FIELD_USERNAME} = '{data.UserName}', " +
									$"{IfsSync2Constants.DB_FIELD_URL} = '{data.URL}', " +
									$"{IfsSync2Constants.DB_FIELD_ACCESS_KEY} = '{data.AccessKey}', " +
									$"{IfsSync2Constants.DB_FIELD_SECRET_KEY} = '{data.SecretKey}', " +
									$"{IfsSync2Constants.DB_FIELD_STORAGE_NAME} = '{data.StorageName}', " +
									$"{IfsSync2Constants.DB_FIELD_S3_FILE_MANAGER_URL} = '{data.S3FileManagerURL}', " +
									$"{IfsSync2Constants.DB_FIELD_DEBUG} = {data.Debug}, " +
									$"{IfsSync2Constants.DB_FIELD_UPDATE_FLAG} = {data.UpdateFlag}" +
									$" WHERE {IfsSync2Constants.DB_FIELD_ID} = {data.Id} "
				};
				int result = cmd.ExecuteNonQuery();

				if (result > 0) { log.Debug($"Success({result}): {cmd.CommandText}"); return true; }
				else { log.Error($"Failed({result}) : {cmd.CommandText}"); return false; }
			}
			catch (Exception e) { log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}

		public bool UpdateUserStorageName(int id, string storageName, bool global)
		{
			try
			{
				var tableName = global ? IfsSync2Constants.DB_TABLE_GLOBAL_USER_LIST : IfsSync2Constants.DB_TABLE_NORMAL_USER_LIST;
				if (!File.Exists(_filePath)) return false;

				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{
					CommandText = $"UPDATE '{tableName}' SET " +
									$"{IfsSync2Constants.DB_FIELD_STORAGE_NAME} = '{storageName}' " +
									$"WHERE {IfsSync2Constants.DB_FIELD_ID} = {id}"
				};
				int result = cmd.ExecuteNonQuery();

				if (result > 0) { log.Debug($"Success({result}): {cmd.CommandText}"); return true; }
				else { log.Error($"Failed({result}) : {cmd.CommandText}"); return false; }
			}
			catch (Exception e) { log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}

		public bool UpdateUserS3Proxy(int id, string s3Proxy, bool global)
		{
			try
			{
				var tableName = global ? IfsSync2Constants.DB_TABLE_GLOBAL_USER_LIST : IfsSync2Constants.DB_TABLE_NORMAL_USER_LIST;
				if (!File.Exists(_filePath)) return false;

				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{
					CommandText = $"UPDATE '{tableName}' SET " +
									$"{IfsSync2Constants.DB_FIELD_URL} = '{s3Proxy}' " +
									$"WHERE {IfsSync2Constants.DB_FIELD_ID} = {id}"
				};
				int result = cmd.ExecuteNonQuery();

				if (result > 0) { log.Debug($"Success({result}): {cmd.CommandText}"); return true; }
				else { log.Error($"Failed({result}) : {cmd.CommandText}"); return false; }
			}
			catch (Exception e) { log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}

		public bool UpdateUserS3FileManagerURL(int id, string s3FileManagerURL, bool global)
		{
			try
			{
				var tableName = global ? IfsSync2Constants.DB_TABLE_GLOBAL_USER_LIST : IfsSync2Constants.DB_TABLE_NORMAL_USER_LIST;
				if (!File.Exists(_filePath)) return false;

				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{
					CommandText = $"UPDATE '{tableName}' SET " +
									$"{IfsSync2Constants.DB_FIELD_S3_FILE_MANAGER_URL} = '{s3FileManagerURL}' " +
									$"WHERE {IfsSync2Constants.DB_FIELD_ID} = {id}"
				};
				int result = cmd.ExecuteNonQuery();

				if (result > 0) { log.Debug($"Success({result}): {cmd.CommandText}"); return true; }
				else { log.Error($"Failed({result}) : {cmd.CommandText}"); return false; }
			}
			catch (Exception e) { log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}

		public bool UpdateUserCheck(UserData data, bool global)
		{
			try
			{
				var tableName = global ? IfsSync2Constants.DB_TABLE_GLOBAL_USER_LIST : IfsSync2Constants.DB_TABLE_NORMAL_USER_LIST;
				if (!File.Exists(_filePath)) return false;

				_mutex.WaitOne();

				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn)
				{
					CommandText = $"UPDATE '{tableName}' SET " +
									$"{IfsSync2Constants.DB_FIELD_DEBUG} = {data.Debug}, " +
									$"{IfsSync2Constants.DB_FIELD_UPDATE_FLAG} = {data.UpdateFlag}" +
									$" WHERE {IfsSync2Constants.DB_FIELD_ID} = {data.Id} "
				};
				int result = cmd.ExecuteNonQuery();

				if (result > 0) { log.Debug($"Success({result}): {cmd.CommandText}"); return true; }
				else { log.Error($"Failed({result}) : {cmd.CommandText}"); return false; }
			}
			catch (Exception e) { log.Error(e); return false; }
			finally { _mutex.ReleaseMutex(); }
		}
	}
}