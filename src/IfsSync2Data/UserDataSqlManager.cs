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
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Threading;

namespace IfsSync2Data
{
    public class UserDataSqlManager
    {
        private const string CLASS_NAME = "JobDataSqlManager";
        /******************** User List Attribute ********************************/
        private const string STR_NORMAL_USER_TABLE_NAME = "NormalUserList";
        private const string STR_GLOBAL_USER_TABLE_NAME = "GlobalUserList";
        private const string STR_USER_ID                = "ID";
        private const string STR_USER_HOSTNAME          = "HostName";
        private const string STR_USER_USERNAME          = "UserName";
        private const string STR_USER_URL               = "URL";
        private const string STR_USER_ACCESSKEY         = "AccessKey";
        private const string STR_USER_ACCESSSECRET      = "AccessSecret";
        private const string STR_USER_STORAGE_NAME      = "StorageName";
        private const string STR_S3_FILEMANAGER_URL     = "S3FileManagerURL";
        private const string STR_USER_DEBUG             = "Debug";
        private const string STR_USER_UPDATEFLAG        = "UpdateFlag";
        /**************************** DB **************************************/
        private readonly string FilePath;
        private readonly Mutex SqliteMutex;

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public UserDataSqlManager(string RootPath)
        {
            const string FUNCTION_NAME = "Init";
            FilePath = MainData.CreateDBFileName(RootPath, MainData.USER_DB_FILE_NAME);
            try
            {
                SqliteMutex = new Mutex(false, MainData.MUTEX_NAME_USER_SQL, out bool CreatedNew);

                if (!CreatedNew) log.DebugFormat("[{0}:{1}] Mutex({3})", CLASS_NAME, FUNCTION_NAME, "Mutex", MainData.MUTEX_NAME_USER_SQL);
                else log.DebugFormat("[{0}:{1}] Mutex({3}) create", CLASS_NAME, FUNCTION_NAME, "Mutex", MainData.MUTEX_NAME_USER_SQL);
            }
            catch(Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] Mutex({3}) fail : ", CLASS_NAME, FUNCTION_NAME, "Exception", MainData.MUTEX_NAME_USER_SQL, e.Message);
            }
        }

        private bool CreateDBFile()
        {
            const string FUNCTION_NAME = "CreateDBFile";
            try
            {
                MainData.CreateDirectory(FilePath);
                SQLiteConnection.CreateFile(FilePath);

                SqliteMutex.WaitOne();
                using (SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath)))
                {
                    conn.Open();
                    SQLiteCommand cmd = new SQLiteCommand(conn)
                    {
                        CommandText =
                                    //Global User List
                                    string.Format(
                        "Create Table '{0}'(" + //STR_GLOBAL_USER_TABLE_NAME
                                     "'{1}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT," + //STR_USER_ID
                                     "'{2}' TEXT, "          + //STR_USER_HOSTNAME
                                     "'{3}' TEXT NOT NULL, " + //STR_USER_USERNAME
                                     "'{4}' TEXT NOT NULL, " + //STR_USER_URL
                                     "'{5}' TEXT NOT NULL, " + //STR_USER_ACCESSKEY
                                     "'{6}' TEXT NOT NULL, " + //STR_USER_ACCESSSECRET
                                     "'{7}' TEXT NOT NULL, " + //STR_USER_STORAGE_NAME
                                     "'{8}' TEXT, "          + //STR_S3_FILEMANAGER_URL
                                     "'{9}' BOOL NOT NULL DEFAULT TRUE, " + //STR_USER_DEBUG
                                     "'{10}' BOOL NOT NULL DEFAULT FALSE);",  //STR_USER_UPDATEFLAG
                                     STR_GLOBAL_USER_TABLE_NAME,
                                     STR_USER_ID,
                                     STR_USER_HOSTNAME,
                                     STR_USER_USERNAME,
                                     STR_USER_URL,
                                     STR_USER_ACCESSKEY,
                                     STR_USER_ACCESSSECRET,
                                     STR_USER_STORAGE_NAME,
                                     STR_S3_FILEMANAGER_URL,
                                     STR_USER_DEBUG,
                                     STR_USER_UPDATEFLAG) +
                                    //User List
                                    string.Format(
                        "Create Table '{0}'(" + //STR_NORMAL_USER_TABLE_NAME
                                     "'{1}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT," + //STR_USER_ID
                                     "'{2}' TEXT, "          + //STR_USER_HOSTNAME
                                     "'{3}' TEXT NOT NULL, " + //STR_USER_USERNAME
                                     "'{4}' TEXT NOT NULL, " + //STR_USER_URL
                                     "'{5}' TEXT NOT NULL, " + //STR_USER_ACCESSKEY
                                     "'{6}' TEXT NOT NULL, " + //STR_USER_ACCESSSECRET
                                     "'{7}' TEXT NOT NULL, " + //STR_USER_STORAGE_NAME
                                     "'{8}' TEXT, " + //STR_S3_FILEMANAGER_URL
                                     "'{9}' BOOL NOT NULL DEFAULT TRUE, " + //STR_USER_DEBUG
                                     "'{10}' BOOL NOT NULL DEFAULT FALSE);",  //STR_USER_UPDATEFLAG
                                     STR_NORMAL_USER_TABLE_NAME,
                                     STR_USER_ID,
                                     STR_USER_HOSTNAME,
                                     STR_USER_USERNAME,
                                     STR_USER_URL,
                                     STR_USER_ACCESSKEY,
                                     STR_USER_ACCESSSECRET,
                                     STR_USER_STORAGE_NAME,
                                     STR_S3_FILEMANAGER_URL,
                                     STR_USER_DEBUG,
                                     STR_USER_UPDATEFLAG)
                    };
                    cmd.ExecuteNonQuery();
                    conn.Close();
                    log.DebugFormat("[{0}:{1}] Success : {2}", CLASS_NAME, FUNCTION_NAME, cmd.CommandText);
                }
                return true;
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }

        public bool InsertUser(UserData Data, bool Global)
        {
            const string FUNCTION_NAME = "InsertUser";
            if (!File.Exists(FilePath)) if (!CreateDBFile()) return false;

            try
            {
                SqliteMutex.WaitOne();
                SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath));
                if (conn == null)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                    return false;
                }

                conn.Open();
                
                string TableName = string.Empty;
                if (Global) TableName = STR_GLOBAL_USER_TABLE_NAME;
                else        TableName = STR_NORMAL_USER_TABLE_NAME;

                SQLiteCommand cmd = new SQLiteCommand(conn)
                {
                    CommandText = string.Format(
                                    "INSERT INTO '{0}'  ( {1},   {3},   {5},   {7} ,   {9} ,  {11} ,  {13} , {15}, {17} )" +
                                               " VALUES ('{2}', '{4}', '{6}', '{8}', '{10}', '{12}', '{14}', {16}, {18} );",
                                    TableName,
                                    STR_USER_HOSTNAME, Data.HostName,               //   1,  2"
                                    STR_USER_USERNAME, Data.UserName,               //   3,  4"
                                    STR_USER_URL, Data.URL,                         //   5,  6"
                                    STR_USER_ACCESSKEY, Data.AccessKey,             //   7,  8"
                                    STR_USER_ACCESSSECRET, Data.AccessSecret,       //   9, 10"
                                    STR_USER_STORAGE_NAME, Data.StorageName,        //  11, 12"
                                    STR_S3_FILEMANAGER_URL, Data.S3FileManagerURL,  //  13, 14"
                                    STR_USER_DEBUG, Data.Debug,                     //  15, 16
                                    STR_USER_UPDATEFLAG, Data.UpdateFlag)           //  17, 18
                };  

                int result = cmd.ExecuteNonQuery();
                conn.Close();

                if (result > 0)
                {
                    log.DebugFormat("[{0}:{1}] Success : {2}", CLASS_NAME, FUNCTION_NAME, cmd.CommandText);
                    return true;
                }
                else
                {
                    log.ErrorFormat("[{0}:{1}:{2}] Fail : ", CLASS_NAME, FUNCTION_NAME, "ExecuteNonQuery", cmd.CommandText);
                    return false;
                }
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }

        public List<UserData> GetUsers(bool Global)
        {
            const string FUNCTION_NAME = "GetUsers";
            if (!File.Exists(FilePath)) CreateDBFile();
            try
            {
                List<UserData> Items = new List<UserData>();

                SqliteMutex.WaitOne();

                using (SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath)))
                {
                    if (conn == null)
                    {
                        string msg = string.Format("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");

                        log.ErrorFormat(msg);
                        throw new Exception(msg);
                    }
                    conn.Open();

                    string TableName = string.Empty;
                    if (Global) TableName = STR_GLOBAL_USER_TABLE_NAME;
                    else        TableName = STR_NORMAL_USER_TABLE_NAME;

                    SQLiteCommand cmd = new SQLiteCommand(conn)
                    {
                        CommandText = string.Format("SELECT * FROM '{0}'", TableName)
                    };
                    SQLiteDataReader Rdr = cmd.ExecuteReader();

                    while (Rdr.Read())
                    {
                        UserData Data = new UserData
                        {
                            ID           = Convert.ToInt32(Rdr[STR_USER_ID]),
                            HostName     = Rdr[STR_USER_HOSTNAME].ToString(),
                            UserName     = Rdr[STR_USER_USERNAME].ToString(),
                            URL          = Rdr[STR_USER_URL].ToString(),
                            AccessKey    = Rdr[STR_USER_ACCESSKEY].ToString(),
                            AccessSecret = Rdr[STR_USER_ACCESSSECRET].ToString(),
                            StorageName    = Rdr[STR_USER_STORAGE_NAME].ToString(),
                            S3FileManagerURL = Rdr[STR_S3_FILEMANAGER_URL].ToString(),
                            Debug        = Convert.ToBoolean(Rdr[STR_USER_DEBUG]),
                            UpdateFlag   = Convert.ToBoolean(Rdr[STR_USER_UPDATEFLAG]),
                        };
                        Items.Add(Data);
                    }
                    Rdr.Close();
                    conn.Close();
                    log.DebugFormat("[{0}:{1}] {2} Count : {3}", CLASS_NAME, FUNCTION_NAME, TableName, Items.Count);
                }
                return Items;
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                throw e;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                throw e;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }
        public List<UserData> GetUsers(string HostName)
        {
            const string FUNCTION_NAME = "GetUsers";
            if (!File.Exists(FilePath)) CreateDBFile();
            try
            {
                List<UserData> Items = new List<UserData>();

                SqliteMutex.WaitOne();

                using (SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath)))
                {
                    if (conn == null)
                    {
                        string msg = string.Format("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");

                        log.ErrorFormat(msg);
                        throw new Exception(msg);
                    }
                    conn.Open();

                    SQLiteCommand cmd = new SQLiteCommand(conn)
                    {
                        CommandText = string.Format("SELECT * FROM '{0}' WHERE {1} = '{2}';",
                                    STR_NORMAL_USER_TABLE_NAME, STR_USER_HOSTNAME, HostName)
                    };
                    SQLiteDataReader Rdr = cmd.ExecuteReader();

                    while (Rdr.Read())
                    {
                        UserData Data = new UserData
                        {
                            ID = Convert.ToInt32(Rdr[STR_USER_ID]),
                            HostName = Rdr[STR_USER_HOSTNAME].ToString(),
                            UserName = Rdr[STR_USER_USERNAME].ToString(),
                            URL = Rdr[STR_USER_URL].ToString(),
                            AccessKey = Rdr[STR_USER_ACCESSKEY].ToString(),
                            AccessSecret = Rdr[STR_USER_ACCESSSECRET].ToString(),
                            StorageName = Rdr[STR_USER_STORAGE_NAME].ToString(),
                            S3FileManagerURL = Rdr[STR_S3_FILEMANAGER_URL].ToString(),
                            Debug = Convert.ToBoolean(Rdr[STR_USER_DEBUG]),
                            UpdateFlag = Convert.ToBoolean(Rdr[STR_USER_UPDATEFLAG]),
                        };
                        Items.Add(Data);
                    }
                    Rdr.Close();
                    conn.Close();
                    log.DebugFormat("[{0}:{1}] Count({2}) : {3}", CLASS_NAME, FUNCTION_NAME, HostName, Items.Count);
                }
                return Items;
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                throw e;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                throw e;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }
        public UserData GetUserToID(int ID, bool Global)
        {
            const string FUNCTION_NAME = "GetUserToID";
            try
            {
                FileInfo file = new FileInfo(FilePath);
                if (!file.Exists) CreateDBFile();

                SqliteMutex.WaitOne();

                using (SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath)))
                {
                    if (conn == null)
                    {
                        log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                        throw new Exception();
                    }
                    conn.Open();
                    
                    string TableName = string.Empty;
                    if (Global) TableName = STR_GLOBAL_USER_TABLE_NAME;
                    else        TableName = STR_NORMAL_USER_TABLE_NAME;

                    SQLiteCommand cmd = new SQLiteCommand(conn)
                    {
                        CommandText = string.Format("SELECT * FROM {0} WHERE {1} = {2}",
                                                      TableName, STR_USER_ID, ID)
                    };
                    SQLiteDataReader Rdr = cmd.ExecuteReader();

                    Rdr.Read();

                    UserData Data = new UserData
                    {
                        ID           = Convert.ToInt32(Rdr[STR_USER_ID]),
                        HostName     = Rdr[STR_USER_HOSTNAME].ToString(),
                        UserName     = Rdr[STR_USER_USERNAME].ToString(),
                        URL          = Rdr[STR_USER_URL].ToString(),
                        AccessKey    = Rdr[STR_USER_ACCESSKEY].ToString(),
                        AccessSecret = Rdr[STR_USER_ACCESSSECRET].ToString(),
                        StorageName    = Rdr[STR_USER_STORAGE_NAME].ToString(),
                        S3FileManagerURL = Rdr[STR_S3_FILEMANAGER_URL].ToString(),
                        Debug        = Convert.ToBoolean(Rdr[STR_USER_DEBUG]),
                        UpdateFlag   = Convert.ToBoolean(Rdr[STR_USER_UPDATEFLAG]),
                    };
                    Rdr.Close();
                    conn.Close();
                    log.DebugFormat("[{0}:{1}] Success User : {2}", CLASS_NAME, FUNCTION_NAME, Data.UserName);
                    return Data;
                }
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                throw new Exception();
            }
            catch (AbandonedMutexException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "FileNotFoundException", e.Message);
                throw new Exception();
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                throw new Exception();
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }
        public bool IsUserName(string UserName, bool Global)
        {

            const string FUNCTION_NAME = "IsUserName";
            try
            {
                FileInfo file = new FileInfo(FilePath);
                if (!file.Exists) CreateDBFile();

                SqliteMutex.WaitOne();

                using (SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath)))
                {
                    if (conn == null)
                    {
                        log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                        return false;
                    }
                    conn.Open();

                    string TableName = string.Empty;
                    if (Global) TableName = STR_GLOBAL_USER_TABLE_NAME;
                    else TableName = STR_NORMAL_USER_TABLE_NAME;

                    SQLiteCommand cmd = new SQLiteCommand(conn)
                    {
                        CommandText = string.Format("SELECT count(ID) FROM {0} WHERE {1} = '{2}';", TableName, STR_USER_USERNAME, UserName)
                    };
                    int result = Convert.ToInt32(cmd.ExecuteScalar());
                    conn.Close();

                    if (result > 0)
                    {
                        log.DebugFormat("[{0}:{1}] Exists User({3}) : {2}", CLASS_NAME, FUNCTION_NAME, UserName, result);
                        return true;
                    }
                    else
                    {
                        log.DebugFormat("[{0}:{1}] Not Exists User({3}) : {2}", CLASS_NAME, FUNCTION_NAME, cmd.CommandText, result);
                        return false;
                    }
                }
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (AbandonedMutexException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "FileNotFoundException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }

        public bool DeleteUserToID(int ID, bool Global)
        {
            const string FUNCTION_NAME = "DeleteUser";
            try
            {
                FileInfo file = new FileInfo(FilePath);
                if (!file.Exists)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] {3} Not Exists", CLASS_NAME, FUNCTION_NAME, "FileInfo", FilePath);
                    return false;
                }

                SqliteMutex.WaitOne();

                using (SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath)))
                {
                    if (conn == null)
                    {
                        log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                        throw new Exception();
                    }
                    conn.Open();
                    
                    string TableName = string.Empty;
                    if (Global) TableName = STR_GLOBAL_USER_TABLE_NAME;
                    else        TableName = STR_NORMAL_USER_TABLE_NAME;

                    SQLiteCommand cmd = new SQLiteCommand(conn)
                    {
                        CommandText = string.Format("DELETE FROM '{0}' WHERE {1} = {2}",
                                                      TableName, STR_USER_ID, ID)
                    };
                    int result = cmd.ExecuteNonQuery();
                    conn.Close();

                    if (result > 0)
                    {
                        log.DebugFormat("[{0}:{1}] Delete Success UserID : {2}", CLASS_NAME, FUNCTION_NAME, ID);
                        return true;
                    }
                    else
                    {
                        log.ErrorFormat("[{0}:{1}] Delete Fail UserID : {2}", CLASS_NAME, FUNCTION_NAME, ID);
                        return false;
                    }
                }
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }
        public bool UpdateUser(UserData Data, bool Global)
        {
            const string FUNCTION_NAME = "UpdateUser";
            try
            {
                FileInfo file = new FileInfo(FilePath);
                if (!file.Exists)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] {3} Not Exists", CLASS_NAME, FUNCTION_NAME, "FileInfo", FilePath);
                    return false;
                }
                SqliteMutex.WaitOne();

                using (SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath)))
                {
                    if (conn == null)
                    {
                        log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                        throw new Exception();
                    }
                    conn.Open();
                    
                    string TableName = string.Empty;
                    if (Global) TableName = STR_GLOBAL_USER_TABLE_NAME;
                    else        TableName = STR_NORMAL_USER_TABLE_NAME;

                    SQLiteCommand cmd = new SQLiteCommand(conn)
                    {
                        CommandText = string.Format(
                                    "UPDATE '{0}' SET {1} =   '{2}', " +
                                                     "{3} =   '{4}', " +
                                                     "{5} =   '{6}'," +
                                                     "{7} =   '{8}'," +
                                                     "{9} =  '{10}'," +
                                                     "{11} =  {12} ," +
                                                     "{13} =  {14}  " +
                                               "WHERE {15} = '{16}';",
                                    TableName,
                                    STR_USER_URL         , Data.URL,
                                    STR_USER_ACCESSKEY   , Data.AccessKey,
                                    STR_USER_ACCESSSECRET, Data.AccessSecret,
                                    STR_USER_STORAGE_NAME, Data.StorageName,
                                    STR_S3_FILEMANAGER_URL, Data.S3FileManagerURL,
                                    STR_USER_DEBUG       , Data.Debug,
                                    STR_USER_UPDATEFLAG  , true,
                                    STR_USER_ID, Data.ID)
                    };

                    int result = cmd.ExecuteNonQuery();
                    conn.Close();

                    if (result > 0)
                    {
                        log.DebugFormat("[{0}:{1}] Update Success User : {2}", CLASS_NAME, FUNCTION_NAME, Data.UserName);
                        return true;
                    }
                    else
                    {
                        log.ErrorFormat("[{0}:{1}] Update Fail User : {2}", CLASS_NAME, FUNCTION_NAME, Data.UserName);
                        return false;
                    }
                }
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }
        public bool UpdateUserStorageName(int ID, string StorageName, bool Global)
        {
            const string FUNCTION_NAME = "UpdateUserStorageName";
            try
            {
                FileInfo file = new FileInfo(FilePath);
                if (!file.Exists)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] {3} Not Exists", CLASS_NAME, FUNCTION_NAME, "FileInfo", FilePath);
                    return false;
                }
                SqliteMutex.WaitOne();

                using (SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath)))
                {
                    if (conn == null)
                    {
                        log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                        throw new Exception();
                    }
                    conn.Open();

                    string TableName = string.Empty;
                    if (Global) TableName = STR_GLOBAL_USER_TABLE_NAME;
                    else TableName = STR_NORMAL_USER_TABLE_NAME;

                    SQLiteCommand cmd = new SQLiteCommand(conn)
                    {
                        CommandText = string.Format(
                                    "UPDATE '{0}' SET {1} = '{2}' " +
                                               "WHERE {3} = '{4}';",
                                    TableName,
                                    STR_USER_STORAGE_NAME, StorageName,
                                    STR_USER_ID, ID)
                    };

                    int result = cmd.ExecuteNonQuery();
                    conn.Close();

                    if (result > 0)
                    {
                        log.DebugFormat("[{0}:{1}] Update Success User : {2}", CLASS_NAME, FUNCTION_NAME, StorageName);
                        return true;
                    }
                    else
                    {
                        log.ErrorFormat("[{0}:{1}] Update Fail User : {2}", CLASS_NAME, FUNCTION_NAME, StorageName);
                        return false;
                    }
                }
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }
        public bool UpdateUserS3Proxy(int ID, string S3Proxy, bool Global)
        {
            const string FUNCTION_NAME = "UpdateUserS3Proxy";
            try
            {
                FileInfo file = new FileInfo(FilePath);
                if (!file.Exists)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] {3} Not Exists", CLASS_NAME, FUNCTION_NAME, "FileInfo", FilePath);
                    return false;
                }
                SqliteMutex.WaitOne();

                using (SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath)))
                {
                    if (conn == null)
                    {
                        log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                        throw new Exception();
                    }
                    conn.Open();

                    string TableName = string.Empty;
                    if (Global) TableName = STR_GLOBAL_USER_TABLE_NAME;
                    else TableName = STR_NORMAL_USER_TABLE_NAME;

                    SQLiteCommand cmd = new SQLiteCommand(conn)
                    {
                        CommandText = string.Format(
                                    "UPDATE '{0}' SET {1} = '{2}' " +
                                               "WHERE {3} = '{4}';",
                                    TableName,
                                    STR_USER_URL, S3Proxy,
                                    STR_USER_ID, ID)
                    };

                    int result = cmd.ExecuteNonQuery();
                    conn.Close();

                    if (result > 0)
                    {
                        log.DebugFormat("[{0}:{1}] Update Success User : {2}", CLASS_NAME, FUNCTION_NAME, S3Proxy);
                        return true;
                    }
                    else
                    {
                        log.ErrorFormat("[{0}:{1}] Update Fail User : {2}", CLASS_NAME, FUNCTION_NAME, S3Proxy);
                        return false;
                    }
                }
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }
        public bool UpdateUserS3FileManagerURL(int ID, string S3FileManagerURL, bool Global)
        {
            const string FUNCTION_NAME = "UpdateUserStorageName";
            try
            {
                FileInfo file = new FileInfo(FilePath);
                if (!file.Exists)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] {3} Not Exists", CLASS_NAME, FUNCTION_NAME, "FileInfo", FilePath);
                    return false;
                }
                SqliteMutex.WaitOne();

                using (SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath)))
                {
                    if (conn == null)
                    {
                        log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                        throw new Exception();
                    }
                    conn.Open();

                    string TableName = string.Empty;
                    if (Global) TableName = STR_GLOBAL_USER_TABLE_NAME;
                    else TableName = STR_NORMAL_USER_TABLE_NAME;

                    SQLiteCommand cmd = new SQLiteCommand(conn)
                    {
                        CommandText = string.Format(
                                    "UPDATE '{0}' SET {1} = '{2}' " +
                                               "WHERE {3} = '{4}';",
                                    TableName,
                                    STR_S3_FILEMANAGER_URL, S3FileManagerURL,
                                    STR_USER_ID, ID)
                    };

                    int result = cmd.ExecuteNonQuery();
                    conn.Close();

                    if (result > 0)
                    {
                        log.DebugFormat("[{0}:{1}] Update Success User : {2}", CLASS_NAME, FUNCTION_NAME, S3FileManagerURL);
                        return true;
                    }
                    else
                    {
                        log.ErrorFormat("[{0}:{1}] Update Fail User : {2}", CLASS_NAME, FUNCTION_NAME, S3FileManagerURL);
                        return false;
                    }
                }
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }

        public bool UpdateUserCheck(UserData Data, bool Global)
        {
            const string FUNCTION_NAME = "UpdateUserCheck";
            try
            {
                FileInfo file = new FileInfo(FilePath);
                if (!file.Exists)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] {3} Not Exists", CLASS_NAME, FUNCTION_NAME, "FileInfo", FilePath);
                    return false;
                }
                SqliteMutex.WaitOne();

                using (SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath)))
                {
                    if (conn == null)
                    {
                        log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                        throw new Exception();
                    }
                    conn.Open();

                    string TableName = string.Empty;
                    if (Global) TableName = STR_GLOBAL_USER_TABLE_NAME;
                    else TableName = STR_NORMAL_USER_TABLE_NAME;

                    SQLiteCommand cmd = new SQLiteCommand(conn)
                    {
                        CommandText = string.Format(
                                    "UPDATE '{0}' SET {1} ='{2}' " +
                                               "WHERE {3} = {4};",
                                    TableName,
                                    STR_USER_UPDATEFLAG, false,
                                    STR_USER_ID, Data.ID)
                    };

                    int result = cmd.ExecuteNonQuery();
                    conn.Close();

                    if (result > 0)
                    {
                        log.DebugFormat("[{0}:{1}] Delete Success User : {2}", CLASS_NAME, FUNCTION_NAME, Data.UserName);
                        return true;
                    }
                    else
                    {
                        log.ErrorFormat("[{0}:{1}] Delete Fail User : {2}", CLASS_NAME, FUNCTION_NAME, Data.UserName);
                        return false;
                    }
                }
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }
    }
}
