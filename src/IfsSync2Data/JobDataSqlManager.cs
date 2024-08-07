﻿/*
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
    public class JobDataSqlManager
    {
        /******************** Global Job List Attribute *************************/
        private const string STR_GLOBAL_JOB_TABLE_NAME      = "GlobalJobList";
        private const string STR_GLOBAL_SCHEDULE_TABLE_NAME = "GlobalScheduleList";
        private const string SQLITE_SEQUENCE                = "sqlite_sequence";
        private const string SQLITE_SEQUENCE_SEQ            = "seq";

        /******************** Job List Attribute ********************************/
        private const string STR_JOB_TABLE_NAME     = "JobList";
        private const string STR_JOB_ID             = "ID";
        private const string STR_JOB_HOSTNAME       = "HostName";
        private const string STR_JOB_NAME           = "JobName";
        private const string STR_JOB_ISGLOBALUSER   = "IsGlobalUser";
        private const string STR_JOB_USERID         = "UserID";
        private const string STR_JOB_POLICY_NAME    = "PolicyName";
        private const string STR_JOB_PATH           = "Path";
        private const string STR_JOB_BLACK_PATH     = "BlackPath";
        private const string STR_JOB_BLACK_FILE     = "BlackFile";
        private const string STR_JOB_BLACK_FILE_EXT = "BlackFileExt";
        private const string STR_JOB_WHITE_FILE     = "WhiteFile";
        private const string STR_JOB_WHITE_FILE_EXT = "WhiteFileExt";
        private const string STR_JOB_VSS_FILE_EXT   = "VSSFileExt";
        private const string STR_JOB_REMOVE         = "Remove";
        private const string STR_JOB_IS_INIT        = "IsInit";
        private const string STR_JOB_FILTER_UPDATE  = "FilterUpdate";
        private const string STR_JOB_SENDER_UPDATE  = "SenderUpdate";
        /***************** Schedule List Attribute ******************************/
        private const string STR_SCHEDULE_TABLE_NAME  = "ScheduleList";
        private const string STR_SCHEDULE_ID          = "ID";
        private const string STR_SCHEDULE_JOB_ID      = "JobID";
        private const string STR_SCHEDULE_WEEKS       = "Weeks";
        private const string STR_SCHEDULE_ATTIME      = "AtTime";
        private const string STR_SCHEDULE_FORHOURS    = "ForHours";
        /**************************** ETC **************************************/
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly Mutex SqliteMutex;
        private readonly string FilePath;

        public JobDataSqlManager(string RootPath)
        {
            FilePath = MainData.CreateDBFileName(RootPath, MainData.JOB_DB_FILE_NAME);
            try
            {
                SqliteMutex = new Mutex(false, MainData.MUTEX_NAME_JOB_SQL, out bool CreatedNew);

                if (!CreatedNew) log.Debug($"Mutex({MainData.MUTEX_NAME_JOB_SQL})");
                else log.Debug($"Mutex({MainData.MUTEX_NAME_JOB_SQL}) create");
            }
            catch (Exception e)
            {
                log.Error($"Mutex({MainData.MUTEX_NAME_JOB_SQL}) fail", e);
            }
        }

        private bool CreateDBFile()
        {
            try
            {
                MainData.CreateDirectory(FilePath);
                SQLiteConnection.CreateFile(FilePath);

                SqliteMutex.WaitOne();
                SQLiteConnection conn = new SQLiteConnection($"Data Source={FilePath};Version=3;");
                conn.Open();
                SQLiteCommand cmd = new SQLiteCommand(conn)
                {
                    CommandText =
                    //JobList
                                string.Format(
                    $"Create Table '{STR_JOB_TABLE_NAME}'(" + 
                                 $"'{STR_JOB_ID}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT," + 
                                 $"'{STR_JOB_HOSTNAME}' TEXT, " + 
                                 $"'{STR_JOB_NAME}' TEXT NOT NULL, " + 
                                 $"'{STR_JOB_ISGLOBALUSER}' BOOL NOT NULL, " + 
                                 $"'{STR_JOB_USERID}' INTEGER  NOT NULL, " + 
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
                                 $"'{STR_JOB_FILTER_UPDATE}' BOOL NOT NULL DEFAULT TRUE," + 
                                 $"'{STR_JOB_SENDER_UPDATE}' BOOL NOT NULL DEFAULT TRUE);") +
                    //Global JobList
                                string.Format(
                    $"Create Table '{STR_GLOBAL_JOB_TABLE_NAME}'(" + 
                                 $"'{STR_JOB_ID}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT," + 
                                 $"'{STR_JOB_HOSTNAME}' TEXT, " + 
                                 $"'{STR_JOB_NAME}' TEXT NOT NULL, " + 
                                 $"'{STR_JOB_ISGLOBALUSER}' BOOL NOT NULL, " + 
                                 $"'{STR_JOB_USERID}' INTEGER  NOT NULL, " + 
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
                                 $"'{STR_JOB_FILTER_UPDATE}' BOOL NOT NULL DEFAULT TRUE," + 
                                 $"'{STR_JOB_SENDER_UPDATE}' BOOL NOT NULL DEFAULT TRUE);") +
                    //ScheduleList
                    string.Format( "\n" + 
                    $"Create Table '{STR_SCHEDULE_TABLE_NAME}'(" + 
                                 $"'{STR_SCHEDULE_ID}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT," +
                                 $"'{STR_SCHEDULE_JOB_ID}' INTEGER NOT NULL, " + 
                                 $"'{STR_SCHEDULE_WEEKS}' INTEGER NOT NULL, " + 
                                 $"'{STR_SCHEDULE_ATTIME}' INTEGER NOT NULL, " + 
                                 $"'{STR_SCHEDULE_FORHOURS}' INTEGER NOT NULL, " + 
                                 $"FOREIGN KEY('{STR_SCHEDULE_JOB_ID}') REFERENCES '{STR_JOB_TABLE_NAME}'('{STR_JOB_ID}') ON DELETE CASCADE);") +
                    //GlobalScheduleList
                    string.Format("\n" +
                    $"Create Table '{STR_GLOBAL_SCHEDULE_TABLE_NAME}'(" + 
                                 $"'{STR_SCHEDULE_ID}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT," +
                                 $"'{STR_SCHEDULE_JOB_ID}' INTEGER NOT NULL, " + 
                                 $"'{STR_SCHEDULE_WEEKS}' INTEGER NOT NULL, " + 
                                 $"'{STR_SCHEDULE_ATTIME}' INTEGER NOT NULL, " + 
                                 $"'{STR_SCHEDULE_FORHOURS}' INTEGER NOT NULL, " + 
                                 $"FOREIGN KEY('{STR_SCHEDULE_JOB_ID}') REFERENCES '{STR_JOB_TABLE_NAME}'('{STR_JOB_ID}') ON DELETE CASCADE);") + 
                                 "PRAGMA foreign_keys = On;" //Delete Cascade : on
                };
                cmd.ExecuteNonQuery();
                conn.Close();
                
                log.Debug($"SuccessMainData.MUTEX_NAME_JOB_SQL: {cmd.CommandText}");
                return true;
            }
            catch (SQLiteException e) { log.Error(e); return false; }
            catch (Exception e) { log.Error(e); return false; }
            finally { SqliteMutex.ReleaseMutex(); }
        }

        public bool Insert(JobData Data, bool Global = false)
        {
            if (!File.Exists(FilePath)) if(!CreateDBFile()) return false;

            try
            {
                SqliteMutex.WaitOne();
                SQLiteConnection conn = new SQLiteConnection($"Data Source={FilePath};Version=3;");
                if (conn == null)
                {
                    log.Error("SQLiteConnection fail");
                    return false;
                }
                conn.Open();
                
                string TableName = string.Empty;
                if (Global) TableName = STR_GLOBAL_JOB_TABLE_NAME;
                else        TableName = STR_JOB_TABLE_NAME;

                SQLiteCommand cmd = new SQLiteCommand(conn)
                {
                    CommandText = string.Format(
                    "INSERT INTO '{0}'  ( {1} ,  {3} , {5}, {7},   {9} ,  {11} ,  {13} ,  {15} ,  {17} ,  {19} ,  {21} ,  {23} , {25})" +
                               " VALUES ('{2}', '{4}', {6}, {8}, '{10}', '{12}', '{14}', '{16}', '{18}', '{20}', '{22}', '{24}', {26});",
                    TableName,
                    STR_JOB_HOSTNAME      , Data.HostName,          //  1,  2" 
                    STR_JOB_NAME          , Data.JobName,           //  3,  4"
                    STR_JOB_ISGLOBALUSER  , Data.IsGlobalUser,      //  5,  6
                    STR_JOB_USERID        , Data.UserID,            //  7,  8
                    STR_JOB_PATH          , Data.StrPath,           //  9, 10"
                    STR_JOB_BLACK_PATH    , Data.StrBlackPath,      // 11, 12"
                    STR_JOB_BLACK_FILE    , Data.StrBlackFile,      // 13, 14"
                    STR_JOB_BLACK_FILE_EXT, Data.StrBlackFileExt,   // 15, 16"
                    STR_JOB_WHITE_FILE    , Data.StrWhiteFile,      // 17, 18"
                    STR_JOB_WHITE_FILE_EXT, Data.StrWhiteFileExt,   // 19, 20"
                    STR_JOB_VSS_FILE_EXT  , Data.StrVSSFileExt,     // 21, 22"
                    STR_JOB_POLICY_NAME   , Data.StrPolicy,         // 23, 24"
                    STR_JOB_IS_INIT       , true)                   // 25, 26
                };
                
                int result = cmd.ExecuteNonQuery();
                conn.Close();
                if (result > 0) { log.Debug($"Success({result}): {cmd.CommandText}"); return true; }
                else            { log.Error($"Failed({result}) : {cmd.CommandText}"); return false; }
            }
            catch (SQLiteException e) { log.Error(e); return false; }
            catch (Exception e) { log.Error(e); return false; }
            finally { SqliteMutex.ReleaseMutex(); }
        }
        public bool Update(JobData Data, bool Global = false)
        {
            if (!File.Exists(FilePath)) return false;

            try
            {
                SqliteMutex.WaitOne();

                SQLiteConnection conn = new SQLiteConnection($"Data Source={FilePath};Version=3;");
                if (conn == null)
                {
                    log.Error("SQLiteConnection fail");
                    return false;
                }
                conn.Open();
                
                string TableName = string.Empty;
                if (Global) TableName = STR_GLOBAL_JOB_TABLE_NAME;
                else        TableName = STR_JOB_TABLE_NAME;

                SQLiteCommand cmd = new SQLiteCommand(conn)
                {
                    CommandText = string.Format(
                                    "UPDATE '{0}' SET {1}  =   {2} , " +
                                                     "{3}  =   {4} , " +
                                                     "{5}  =  '{6}', " +
                                                     "{7}  =  '{8}', " +
                                                     "{9}  = '{10}', " +
                                                     "{11} = '{12}', " +
                                                     "{13} = '{14}', " +
                                                     "{15} = '{16}', " +
                                                     "{17} = '{18}', " +
                                                     "{19} = '{20}', " +
                                                     "{21} =  {22} , " +
                                                     "{23} =  {24} , " +
                                                     "{25} =  {26} , " +
                                                     "{27} =  {28}   " +
                                               "WHERE {29} =  {30};  " ,
                                    TableName,
                                    STR_JOB_ISGLOBALUSER  , Data.IsGlobalUser,   //  1,  2
                                    STR_JOB_USERID        , Data.UserID,         //  3,  4
                                    STR_JOB_POLICY_NAME   , Data.StrPolicy,      //  5,  6"
                                    STR_JOB_PATH          , Data.StrPath,        //  7,  8"
                                    STR_JOB_BLACK_PATH    , Data.StrBlackPath,   //  9, 10"
                                    STR_JOB_BLACK_FILE    , Data.StrBlackFile,   // 11, 12"
                                    STR_JOB_BLACK_FILE_EXT, Data.StrBlackFileExt,// 13, 14"
                                    STR_JOB_WHITE_FILE    , Data.StrWhiteFile,   // 15, 16"
                                    STR_JOB_WHITE_FILE_EXT, Data.StrWhiteFileExt,// 17, 18"
                                    STR_JOB_VSS_FILE_EXT  , Data.StrVSSFileExt,  // 19, 20"
                                    STR_JOB_REMOVE        , Data.Remove,         // 21, 22
                                    STR_JOB_IS_INIT       , Data.IsInit,         // 23, 24
                                    STR_JOB_FILTER_UPDATE , true,                // 25, 26
                                    STR_JOB_SENDER_UPDATE , true,                // 27, 28
                                    STR_JOB_ID            , Data.ID)             // 29, 30
                };
                int result = cmd.ExecuteNonQuery();
               
                conn.Close();
                if (result > 0) { log.Debug($"Success({result}): {cmd.CommandText}"); return true; }
                else            { log.Error($"Failed({result}) : {cmd.CommandText}"); return false; }
            }
            catch (SQLiteException e) { log.Error(e); return false; }
            catch (Exception e) { log.Error(e); return false; }
            finally { SqliteMutex.ReleaseMutex(); }
        }
        public bool Delete(int JobID, bool Global = false)
        {
            if (!File.Exists(FilePath)) return false;

            try
            {
                SqliteMutex.WaitOne();

                SQLiteConnection conn = new SQLiteConnection($"Data Source={FilePath};Version=3;");
                if (conn == null)
                {
                    log.Error("SQLiteConnection fail");
                    return false;
                }

                conn.Open();

                string TableName = string.Empty;
                if (Global) TableName = STR_GLOBAL_JOB_TABLE_NAME;
                else TableName = STR_JOB_TABLE_NAME;

                SQLiteCommand cmd = new SQLiteCommand(conn) { CommandText = $"DELETE FROM '{TableName}' WHERE {STR_JOB_ID} = {JobID}" };

                int result = cmd.ExecuteNonQuery();
                conn.Close();
                if (result > 0) { log.Debug($"Success({result}): {cmd.CommandText}"); return true; }
                else            { log.Error($"Failed({result}) : {cmd.CommandText}"); return false; }
            }
            catch (SQLiteException e) { log.Error(e); return false; }
            catch (Exception e) { log.Error(e); return false; }
            finally { SqliteMutex.ReleaseMutex(); }
        }

        public bool DeleteCascadeForUser(int UserID, bool IsGlobalUser, bool Global = false)
        {
            if (!File.Exists(FilePath)) return false;

            try
            {
                SqliteMutex.WaitOne();

                SQLiteConnection conn = new SQLiteConnection($"Data Source={FilePath};Version=3;");
                if (conn == null)
                {
                    log.Error("SQLiteConnection fail");
                    return false;
                }

                conn.Open();

                string TableName = string.Empty;
                if (Global) TableName = STR_GLOBAL_JOB_TABLE_NAME;
                else TableName = STR_JOB_TABLE_NAME;

                SQLiteCommand cmd = new SQLiteCommand(conn) 
                { CommandText = $"DELETE FROM '{TableName}' WHERE {STR_JOB_USERID} = {UserID} AND {STR_JOB_ISGLOBALUSER} = {IsGlobalUser};" };

                int result = cmd.ExecuteNonQuery();
                conn.Close();
                if (result > 0) { log.Debug($"Success({result}): {cmd.CommandText}"); return true; }
                else            { log.Error($"Failed({result}) : {cmd.CommandText}"); return false; }
            }
            catch (SQLiteException e) { log.Error(e); return false; }
            catch (Exception e) { log.Error(e); return false; }
            finally { SqliteMutex.ReleaseMutex(); }
        }

        private bool InsertScheduleList(JobData Data, bool Global = false)
        {
            if (!File.Exists(FilePath)) return false;
            if (Data.ID < 1) Data.ID = GetJobDataID(Data.HostName, Data.JobName);
            if (Data.ScheduleList.Count == 0)
            {
                log.Error("ScheduleList is Empty");
                return false;
            }

            try
            {
                SqliteMutex.WaitOne();
                SQLiteConnection conn = new SQLiteConnection($"Data Source={FilePath};Version=3;");
                if (conn == null)
                {
                    log.Error("SQLiteConnection fail");
                    return false;
                }

                conn.Open();

                string TableName = string.Empty;
                if (Global) TableName = STR_GLOBAL_SCHEDULE_TABLE_NAME;
                else        TableName = STR_SCHEDULE_TABLE_NAME;

                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = $"DELETE FROM '{TableName}' WHERE {STR_SCHEDULE_JOB_ID} = {Data.ID}";
                    cmd.ExecuteNonQuery();
                }

                //Insert
                foreach (Schedule item in Data.ScheduleList)
                {
                    SQLiteCommand cmd = new SQLiteCommand(conn)
                    {
                        CommandText = string.Format(
                                        "INSERT INTO '{0}'  ( {1} ,  {3} ,  {5} ,  {7} )" +
                                                   " VALUES ('{2}', '{4}', '{6}', '{8}')",
                                        TableName,
                                        STR_SCHEDULE_JOB_ID     , Data.ID,
                                        STR_SCHEDULE_WEEKS      , item.Weeks,
                                        STR_SCHEDULE_ATTIME     , item.AtTime,
                                        STR_SCHEDULE_FORHOURS   , item.ForHours)
                    };
                    if (cmd.ExecuteNonQuery() > 0)
                    {
                        log.Debug($"SuccessMainData.MUTEX_NAME_JOB_SQL{Data.JobName}) : {cmd.CommandText}");
                    }
                    else
                    {
                        log.Error($"Fail({Data.JobName}) : {cmd.CommandText}");
                        return false;
                    }
                }
                conn.Close();

                return true;
            }
            catch (SQLiteException e) { log.Error(e); return false; }
            catch (Exception e) { log.Error(e); return false; }
            finally { SqliteMutex.ReleaseMutex(); }
        }
        
        public bool UpdateIsinit(JobData Data, bool Flag, bool Global = false)
        {
            if (!File.Exists(FilePath)) return false;

            try
            {
                SqliteMutex.WaitOne();

                SQLiteConnection conn = new SQLiteConnection($"Data Source={FilePath};Version=3;");
                if (conn == null)
                {
                    log.Error("SQLiteConnection fail");
                    return false;
                }
                conn.Open();

                string TableName = string.Empty;
                if (Global) TableName = STR_GLOBAL_JOB_TABLE_NAME;
                else TableName = STR_JOB_TABLE_NAME;

                SQLiteCommand cmd = new SQLiteCommand(conn)
                { CommandText = $"UPDATE '{TableName}' SET {STR_JOB_IS_INIT} = {Flag} WHERE {STR_JOB_ID} = {Data.ID};" };
                int result = cmd.ExecuteNonQuery();

                conn.Close();
                if (result > 0) { log.Debug($"Success({result}): {cmd.CommandText}"); return true; }
                else            { log.Error($"Failed({result}) : {cmd.CommandText}"); return false; }
            }
            catch (SQLiteException e) { log.Error(e); return false; }
            catch (Exception e) { log.Error(e); return false; }
            finally { SqliteMutex.ReleaseMutex(); }
        }
        public bool UpdateFilterCheck(JobData Data, bool Global = false)
        {
            if (!File.Exists(FilePath)) return false;

            try
            {
                SqliteMutex.WaitOne();

                SQLiteConnection conn = new SQLiteConnection($"Data Source={FilePath};Version=3;");
                if (conn == null)
                {
                    log.Error("SQLiteConnection fail");
                    return false;
                }
                conn.Open();
                
                string TableName = string.Empty;
                if (Global) TableName = STR_GLOBAL_JOB_TABLE_NAME;
                else        TableName = STR_JOB_TABLE_NAME;

                SQLiteCommand cmd = new SQLiteCommand(conn)
                { CommandText = $"UPDATE '{TableName}' SET {STR_JOB_FILTER_UPDATE} = {false} WHERE {STR_JOB_ID} = {Data.ID};" };
                int result = cmd.ExecuteNonQuery();
               
                conn.Close();
                if (result > 0) { log.Debug($"Success({result}): {cmd.CommandText}"); return true; }
                else            { log.Error($"Failed({result}) : {cmd.CommandText}"); return false; }
            }
            catch (SQLiteException e) { log.Error(e); return false; }
            catch (Exception e) { log.Error(e); return false; }
            finally { SqliteMutex.ReleaseMutex(); }
        }
        public bool UpdateSenderCheck(JobData Data, bool Global = false)
        {
            if (!File.Exists(FilePath)) return false;

            try
            {
                SqliteMutex.WaitOne();

                SQLiteConnection conn = new SQLiteConnection($"Data Source={FilePath};Version=3;");
                if (conn == null)
                {
                    log.Error("SQLiteConnection fail");
                    return false;
                }
                conn.Open();

                string TableName = string.Empty;
                if (Global) TableName = STR_GLOBAL_JOB_TABLE_NAME;
                else TableName = STR_JOB_TABLE_NAME;

                SQLiteCommand cmd = new SQLiteCommand(conn)
                { CommandText = $"UPDATE '{TableName}' SET {STR_JOB_SENDER_UPDATE} = {false} WHERE {STR_JOB_ID} = {Data.ID};" };
                int result = cmd.ExecuteNonQuery();

                conn.Close();
                if (result > 0) { log.Debug($"Success({result}): {cmd.CommandText}"); return true; }
                else            { log.Error($"Failed({result}) : {cmd.CommandText}"); return false; }
            }
            catch (SQLiteException e) { log.Error(e); return false; }
            catch (Exception e) { log.Error(e); return false; }
            finally { SqliteMutex.ReleaseMutex(); }
        }
        public int GetJobDataID(string HostName, string JobName)
        {
            int ID = 0;
            try
            {
                SqliteMutex.WaitOne();

                SQLiteConnection conn = new SQLiteConnection($"Data Source={FilePath};Version=3;");
                if (conn == null)
                {
                    log.Error("SQLiteConnection fail");
                    return ID;
                }
                conn.Open();

                SQLiteCommand cmd = new SQLiteCommand(conn)
                { CommandText = $"SELECT * FROM '{STR_JOB_TABLE_NAME}' WHERE {STR_JOB_HOSTNAME} = '{HostName}' AND {STR_JOB_NAME} = '{JobName}';" };
                SQLiteDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read()) ID = Convert.ToInt32(rdr[STR_JOB_ID]);

                conn.Close();

                if (ID > 0) log.Debug($"Success : {cmd.CommandText}");
                else        log.Error($"Failed : {cmd.CommandText}");
            }
            catch (SQLiteException e) { log.Error(e); }
            catch (Exception e) { log.Error(e); }
            finally { SqliteMutex.ReleaseMutex(); }
            return ID;
        }

        public bool IsJobName(string HostName, string JobName)
        {
            try
            {
                SqliteMutex.WaitOne();

                SQLiteConnection conn = new SQLiteConnection($"Data Source={FilePath};Version=3;");
                if (conn == null)
                {
                    log.Error("SQLiteConnection fail");
                    return false;
                }
                conn.Open();

                SQLiteCommand cmd = new SQLiteCommand(conn)
                {
                    CommandText = string.Format(
                                    "SELECT count(*) FROM '{0}' WHERE {1} = '{2}' AND {3} = '{4}';",
                                    STR_JOB_TABLE_NAME, STR_JOB_HOSTNAME, HostName, STR_JOB_NAME, JobName)
                };
                int result = Convert.ToInt32(cmd.ExecuteScalar());

                conn.Close();
                if (result > 0) { log.Debug($"Success({result}): {cmd.CommandText}"); return true; }
                else            { log.Error($"Failed({result}) : {cmd.CommandText}"); return false; }
            }
            catch (SQLiteException e) { log.Error(e); return false; }
            catch (Exception e) { log.Error(e); return false; }
            finally { SqliteMutex.ReleaseMutex(); }
        }

        public List<JobData> GetJobDatas(bool Global = false)
        {

            if (!File.Exists(FilePath)) CreateDBFile();
            try
            {
                List<JobData> items = new List<JobData>();

                SqliteMutex.WaitOne();

                SQLiteConnection conn = new SQLiteConnection($"Data Source={FilePath};Version=3;");
                if (conn == null)
                {
                    string msg = "SQLiteConnection fail";

                    log.Error(msg);
                    throw new Exception(msg);
                }
                conn.Open();
                
                string TableName = string.Empty;
                if (Global) TableName = STR_GLOBAL_JOB_TABLE_NAME;
                else        TableName = STR_JOB_TABLE_NAME;

                SQLiteCommand JobCmd = new SQLiteCommand(conn)
                {
                    CommandText = string.Format("SELECT * FROM '{0}'", TableName)
                };
                SQLiteDataReader JobRdr = JobCmd.ExecuteReader();

                while (JobRdr.Read())
                {
                    JobData Data = new JobData
                    {
                        ID              = Convert.ToInt32(JobRdr[STR_JOB_ID]),
                        HostName        = JobRdr[STR_JOB_HOSTNAME].ToString(),
                        JobName         = JobRdr[STR_JOB_NAME].ToString(),
                        IsGlobalUser    = Convert.ToBoolean(JobRdr[STR_JOB_ISGLOBALUSER]),
                        UserID          = Convert.ToInt32(JobRdr[STR_JOB_USERID]),
                        StrPolicy       = JobRdr[STR_JOB_POLICY_NAME   ].ToString(),
                        StrPath         = JobRdr[STR_JOB_PATH          ].ToString(),
                        StrBlackPath    = JobRdr[STR_JOB_BLACK_PATH    ].ToString(),
                        StrBlackFile    = JobRdr[STR_JOB_BLACK_FILE    ].ToString(),
                        StrBlackFileExt = JobRdr[STR_JOB_BLACK_FILE_EXT].ToString(),
                        StrWhiteFile    = JobRdr[STR_JOB_WHITE_FILE    ].ToString(),
                        StrWhiteFileExt = JobRdr[STR_JOB_WHITE_FILE_EXT].ToString(),
                        StrVSSFileExt   = JobRdr[STR_JOB_VSS_FILE_EXT  ].ToString(),
                        Remove          = Convert.ToBoolean(JobRdr[STR_JOB_REMOVE]),
                        IsInit          = Convert.ToBoolean(JobRdr[STR_JOB_IS_INIT]),
                        FilterUpdate    = Convert.ToBoolean(JobRdr[STR_JOB_FILTER_UPDATE]),
                        SenderUpdate    = Convert.ToBoolean(JobRdr[STR_JOB_SENDER_UPDATE]),
                        Global          = Global
                    };

                    //Get Schedule
                    if (Data.Policy == JobData.PolicyNameList.Schedule)
                    {
                        using (SQLiteCommand ScheduleCmd = new SQLiteCommand(conn))
                        {
                            ScheduleCmd.CommandText = string.Format(
                                            "SELECT * FROM {0} WHERE {1} = {2};",
                                            STR_SCHEDULE_TABLE_NAME, STR_SCHEDULE_JOB_ID, Data.ID);
                            SQLiteDataReader ScheduleRdr = ScheduleCmd.ExecuteReader();

                            while (ScheduleRdr.Read())
                            {
                                Schedule Item = new Schedule()
                                {
                                    ID = Convert.ToInt32(ScheduleRdr[STR_SCHEDULE_ID]),
                                    JobID = Convert.ToInt32(ScheduleRdr[STR_SCHEDULE_JOB_ID]),
                                    Weeks = Convert.ToInt32(ScheduleRdr[STR_SCHEDULE_WEEKS]),
                                    AtTime = Convert.ToInt32(ScheduleRdr[STR_SCHEDULE_ATTIME]),
                                    ForHours = Convert.ToInt32(ScheduleRdr[STR_SCHEDULE_FORHOURS]),
                                };
                                Data.ScheduleList.Add(Item);
                            }
                            ScheduleRdr.Close();
                        }
                    }

                    items.Add(Data);
                }
                JobRdr.Close();

                conn.Close();
                log.Debug($"{TableName} : {items.Count}");
                return items;
            }
            catch (SQLiteException e) { log.Error(e); throw e; }
            catch (Exception e) { log.Error(e); throw e; }
            finally { SqliteMutex.ReleaseMutex(); }
        }
        public List<JobData> GetJobDatas(string HostName = "")
        {
            if (!File.Exists(FilePath)) CreateDBFile();
            try
            {
                List<JobData> items = new List<JobData>();

                SqliteMutex.WaitOne();

                SQLiteConnection conn = new SQLiteConnection($"Data Source={FilePath};Version=3;");
                if (conn == null)
                {
                    string msg = "SQLiteConnection fail";

                    log.Error(msg);
                    throw new Exception(msg);
                }
                conn.Open();
                
                SQLiteCommand JobCmd = new SQLiteCommand(conn)
                {
                    CommandText = string.Format("SELECT * FROM '{0}' Where {1} = '{2}';",
                                            STR_JOB_TABLE_NAME, STR_JOB_HOSTNAME, HostName)
                };
                SQLiteDataReader JobRdr = JobCmd.ExecuteReader();

                while (JobRdr.Read())
                {
                    JobData Data = new JobData
                    {
                        ID              = Convert.ToInt32(JobRdr[STR_JOB_ID]),
                        HostName        = JobRdr[STR_JOB_HOSTNAME].ToString(),
                        JobName         = JobRdr[STR_JOB_NAME].ToString(),
                        IsGlobalUser    = Convert.ToBoolean(JobRdr[STR_JOB_ISGLOBALUSER]),
                        UserID          = Convert.ToInt32(JobRdr[STR_JOB_USERID]),
                        StrPolicy       = JobRdr[STR_JOB_POLICY_NAME].ToString(),
                        StrPath         = JobRdr[STR_JOB_PATH].ToString(),
                        StrBlackPath    = JobRdr[STR_JOB_BLACK_PATH].ToString(),
                        StrBlackFile    = JobRdr[STR_JOB_BLACK_FILE].ToString(),
                        StrBlackFileExt = JobRdr[STR_JOB_BLACK_FILE_EXT].ToString(),
                        StrWhiteFile    = JobRdr[STR_JOB_WHITE_FILE].ToString(),
                        StrWhiteFileExt = JobRdr[STR_JOB_WHITE_FILE_EXT].ToString(),
                        StrVSSFileExt   = JobRdr[STR_JOB_VSS_FILE_EXT].ToString(),
                        Remove          = Convert.ToBoolean(JobRdr[STR_JOB_REMOVE]),
                        IsInit          = Convert.ToBoolean(JobRdr[STR_JOB_IS_INIT]),
                        FilterUpdate    = Convert.ToBoolean(JobRdr[STR_JOB_FILTER_UPDATE]),
                        SenderUpdate    = Convert.ToBoolean(JobRdr[STR_JOB_SENDER_UPDATE]),
                        Global = false
                    };

                    //Get Schedule
                    if (Data.Policy == JobData.PolicyNameList.Schedule)
                    {
                        using (SQLiteCommand ScheduleCmd = new SQLiteCommand(conn))
                        {
                            ScheduleCmd.CommandText = string.Format(
                                            "SELECT * FROM {0} WHERE {1} = {2};",
                                            STR_SCHEDULE_TABLE_NAME, STR_SCHEDULE_JOB_ID, Data.ID);
                            SQLiteDataReader ScheduleRdr = ScheduleCmd.ExecuteReader();

                            while (ScheduleRdr.Read())
                            {
                                Schedule Item = new Schedule()
                                {
                                    ID = Convert.ToInt32(ScheduleRdr[STR_SCHEDULE_ID]),
                                    JobID = Convert.ToInt32(ScheduleRdr[STR_SCHEDULE_JOB_ID]),
                                    Weeks = Convert.ToInt32(ScheduleRdr[STR_SCHEDULE_WEEKS]),
                                    AtTime = Convert.ToInt32(ScheduleRdr[STR_SCHEDULE_ATTIME]),
                                    ForHours = Convert.ToInt32(ScheduleRdr[STR_SCHEDULE_FORHOURS]),
                                };
                                Data.ScheduleList.Add(Item);
                            }
                            ScheduleRdr.Close();
                        }
                    }

                    items.Add(Data);
                }
                JobRdr.Close();

                conn.Close();
                log.Debug($"{HostName} : {items.Count}");
                return items;
            }
            catch (SQLiteException e) { log.Error(e); throw e; }
            catch (Exception e) { log.Error(e); throw e; }
            finally { SqliteMutex.ReleaseMutex(); }
        }
        public JobData GetJobData(int ID, bool Global = false)
        {
            if (!File.Exists(FilePath)) CreateDBFile();
            try
            {
                SqliteMutex.WaitOne();

                SQLiteConnection conn = new SQLiteConnection($"Data Source={FilePath};Version=3;");
                if (conn == null)
                {
                    string msg = "SQLiteConnection fail";

                    log.Error(msg);
                    throw new Exception(msg);
                }
                conn.Open();
                
                string TableName = string.Empty;
                if (Global) TableName = STR_GLOBAL_JOB_TABLE_NAME;
                else        TableName = STR_JOB_TABLE_NAME;

                SQLiteCommand JobCmd = new SQLiteCommand(conn)
                {
                    CommandText = string.Format("SELECT * FROM '{0}' WHERE {1}={2}", TableName, STR_JOB_ID, ID)
                };
                SQLiteDataReader JobRdr = JobCmd.ExecuteReader();

                JobRdr.Read();

                JobData Data = new JobData
                {
                    ID              = Convert.ToInt32(JobRdr[STR_JOB_ID]),
                    HostName        = JobRdr[STR_JOB_HOSTNAME].ToString(),
                    JobName         = JobRdr[STR_JOB_NAME].ToString(),
                    IsGlobalUser    = Convert.ToBoolean(JobRdr[STR_JOB_ISGLOBALUSER]),
                    UserID          = Convert.ToInt32(JobRdr[STR_JOB_USERID]),
                    StrPolicy       = JobRdr[STR_JOB_POLICY_NAME   ].ToString(),
                    StrPath         = JobRdr[STR_JOB_PATH          ].ToString(),
                    StrBlackPath    = JobRdr[STR_JOB_BLACK_PATH    ].ToString(),
                    StrBlackFile    = JobRdr[STR_JOB_BLACK_FILE    ].ToString(),
                    StrBlackFileExt = JobRdr[STR_JOB_BLACK_FILE_EXT].ToString(),
                    StrWhiteFile    = JobRdr[STR_JOB_WHITE_FILE    ].ToString(),
                    StrWhiteFileExt = JobRdr[STR_JOB_WHITE_FILE_EXT].ToString(),
                    StrVSSFileExt   = JobRdr[STR_JOB_VSS_FILE_EXT  ].ToString(),
                    Remove          = Convert.ToBoolean(JobRdr[STR_JOB_REMOVE]),
                    IsInit          = Convert.ToBoolean(JobRdr[STR_JOB_IS_INIT]),
                    FilterUpdate    = Convert.ToBoolean(JobRdr[STR_JOB_FILTER_UPDATE]),
                    SenderUpdate    = Convert.ToBoolean(JobRdr[STR_JOB_SENDER_UPDATE]),
                    Global          = Global
                };

                //Get Schedule
                if (Data.Policy == JobData.PolicyNameList.Schedule)
                {
                    using (SQLiteCommand ScheduleCmd = new SQLiteCommand(conn))
                    {
                        ScheduleCmd.CommandText = string.Format(
                                        "SELECT * FROM {0} WHERE {1} = {2};",
                                        STR_SCHEDULE_TABLE_NAME, STR_SCHEDULE_JOB_ID, Data.ID);
                        SQLiteDataReader ScheduleRdr = ScheduleCmd.ExecuteReader();

                        while (ScheduleRdr.Read())
                        {
                            Schedule Item = new Schedule()
                            {
                                ID = Convert.ToInt32(ScheduleRdr[STR_SCHEDULE_ID]),
                                JobID = Convert.ToInt32(ScheduleRdr[STR_SCHEDULE_JOB_ID]),
                                Weeks = Convert.ToInt32(ScheduleRdr[STR_SCHEDULE_WEEKS]),
                                AtTime = Convert.ToInt32(ScheduleRdr[STR_SCHEDULE_ATTIME]),
                                ForHours = Convert.ToInt32(ScheduleRdr[STR_SCHEDULE_FORHOURS]),
                            };
                            Data.ScheduleList.Add(Item);
                        }
                        ScheduleRdr.Close();
                    }
                }
                JobRdr.Close();

                conn.Close();
                log.Debug($"Job : {Data.JobName}");
                return Data;
            }
            catch (SQLiteException e) { log.Error(e); throw e; }
            catch (Exception e) { log.Error(e); throw e; }
            finally { SqliteMutex.ReleaseMutex(); }
        }

        public bool PutJobData(JobData Data)
        {
            if (Data.ID > 0) { if (!Update(Data)) return false; }
            else             { if (!Insert(Data)) return false; }

            if (Data.Policy == JobData.PolicyNameList.Schedule) return InsertScheduleList(Data);
            return true;
        }
        
        public int NextGlobalJobIndex()
        {
            int Index = 0;
            try
            {
                SqliteMutex.WaitOne();

                SQLiteConnection conn = new SQLiteConnection($"Data Source={FilePath};Version=3;");
                if (conn == null)
                {
                    log.Error("SQLiteConnection fail");
                    return Index;
                }
                conn.Open();

                SQLiteCommand cmd = new SQLiteCommand(conn)
                {
                    CommandText = string.Format(
                                    "SELECT * FROM '{0}' WHERE name = '{1}'",
                                    SQLITE_SEQUENCE, STR_GLOBAL_JOB_TABLE_NAME)
                };
                SQLiteDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    Index = Convert.ToInt32(rdr[SQLITE_SEQUENCE_SEQ]);
                }

                conn.Close();

                log.Debug($"Success : {cmd.CommandText}");
            }
            catch (SQLiteException e) { log.Error(e);}
            catch (Exception e) { log.Error(e); }
            finally { SqliteMutex.ReleaseMutex(); }
            return Index + 1;
        }
    }
}
