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
using Microsoft.Win32;

namespace IfsSync2Common
{
	public class SenderConfig
	{
		#region Define
		const string MULTIPART_UPLOAD_FILE_SIZE = "MultipartUploadFileSize";
		const string MULTIPART_UPLOAD_PART_SIZE = "MultipartUploadPartSize";
		const string SENDER_THREAD_COUNT = "SenderThreadCount";
		const string SENDER_LOG_RETENTION = "SenderLogRetention";
		const string SENDER_FETCH_COUNT = "SenderFetchCount";
		const string SENDER_DELAY = "SenderDelay";
		const string SENDER_CHECK_DELAY = "SenderCheckDelay";
		const string SENDER_STOP = "SenderStop";
		const string ROOT_PATH = "RootPath";
		const int DEFAULT_THREAD_COUNT = 10;
		const int DEFAULT_FETCH_COUNT = 1000; //5sec
		const int DEFAULT_SENDER_DELAY = 5 * 1000; //5sec
		const int DEFAULT_SENDER_CHECK_DELAY = 5 * 1000; //5 sec
		const long DEFAULT_MULTIPART_UPLOAD_FILE_SIZE = 100000000; //100MB
		const long DEFAULT_MULTIPART_UPLOAD_PART_SIZE = 10000000; //10MB
		const int DEFAULT_LOG_RETENTION = 0; // 무제한
		#endregion

		readonly RegistryKey SenderConfigKey;

#pragma warning disable CA1416

		public long MultipartUploadFileSize
		{
			get => RegistryUtility.GetLongValue(SenderConfigKey, MULTIPART_UPLOAD_FILE_SIZE);
			set => SenderConfigKey.SetValue(MULTIPART_UPLOAD_FILE_SIZE, value, RegistryValueKind.QWord);
		}
		public long MultipartUploadPartSize
		{
			get => RegistryUtility.GetLongValue(SenderConfigKey, MULTIPART_UPLOAD_PART_SIZE);
			set => SenderConfigKey.SetValue(MULTIPART_UPLOAD_PART_SIZE, value, RegistryValueKind.QWord);
		}
		public int ThreadCount
		{
			get => RegistryUtility.GetIntValue(SenderConfigKey, SENDER_THREAD_COUNT);
			set => SenderConfigKey.SetValue(SENDER_THREAD_COUNT, value, RegistryValueKind.DWord);
		}
		public int LogRetention
		{
			get => RegistryUtility.GetIntValue(SenderConfigKey, SENDER_LOG_RETENTION);
			set => SenderConfigKey.SetValue(SENDER_LOG_RETENTION, value, RegistryValueKind.DWord);
		}
		public int FetchCount
		{
			get => RegistryUtility.GetIntValue(SenderConfigKey, SENDER_FETCH_COUNT);
			set => SenderConfigKey.SetValue(SENDER_FETCH_COUNT, value, RegistryValueKind.DWord);
		}
		public int SenderDelay
		{
			get => RegistryUtility.GetIntValue(SenderConfigKey, SENDER_DELAY);
			set => SenderConfigKey.SetValue(SENDER_DELAY, value, RegistryValueKind.DWord);
		}
		public int SenderCheckDelay
		{
			get => RegistryUtility.GetIntValue(SenderConfigKey, SENDER_CHECK_DELAY);
			set => SenderConfigKey.SetValue(SENDER_CHECK_DELAY, value, RegistryValueKind.DWord);
		}
		public string RootPath
		{
			get => RegistryUtility.GetStringValue(SenderConfigKey, ROOT_PATH);
			set => SenderConfigKey.SetValue(ROOT_PATH, value, RegistryValueKind.String);
		}
		public bool Stop
		{
			get => RegistryUtility.GetBoolValue(SenderConfigKey, SENDER_STOP);
			set => SenderConfigKey.SetValue(SENDER_STOP, value ? IfsSync2Constants.MY_TRUE : IfsSync2Constants.MY_FALSE, RegistryValueKind.DWord);
		}
		public bool Alive
		{
			get => RegistryUtility.GetBoolValue(SenderConfigKey, IfsSync2Constants.ALIVE_CHECK);
			set => SenderConfigKey.SetValue(IfsSync2Constants.ALIVE_CHECK, value ? IfsSync2Constants.MY_TRUE : IfsSync2Constants.MY_FALSE, RegistryValueKind.DWord);
		}
		public SenderConfig(bool write = false)
		{
			var temp = Registry.LocalMachine.OpenSubKey(IfsSync2Constants.SENDER_CONFIG_PATH, write);
			if (temp == null)
			{
				temp = Registry.LocalMachine.CreateSubKey(IfsSync2Constants.SENDER_CONFIG_PATH);

				temp.SetValue(MULTIPART_UPLOAD_FILE_SIZE, DEFAULT_MULTIPART_UPLOAD_FILE_SIZE, RegistryValueKind.QWord);
				temp.SetValue(MULTIPART_UPLOAD_PART_SIZE, DEFAULT_MULTIPART_UPLOAD_PART_SIZE, RegistryValueKind.QWord);
				temp.SetValue(SENDER_THREAD_COUNT, DEFAULT_THREAD_COUNT, RegistryValueKind.DWord);
				temp.SetValue(SENDER_FETCH_COUNT, DEFAULT_FETCH_COUNT, RegistryValueKind.DWord);
				temp.SetValue(SENDER_DELAY, DEFAULT_SENDER_DELAY, RegistryValueKind.DWord);
				temp.SetValue(SENDER_CHECK_DELAY, DEFAULT_SENDER_CHECK_DELAY, RegistryValueKind.DWord);
				temp.SetValue(ROOT_PATH, "", RegistryValueKind.String);
				temp.SetValue(SENDER_STOP, IfsSync2Constants.MY_FALSE, RegistryValueKind.DWord);
				temp.SetValue(IfsSync2Constants.ALIVE_CHECK, IfsSync2Constants.MY_FALSE, RegistryValueKind.DWord);
				temp.SetValue(SENDER_LOG_RETENTION, DEFAULT_LOG_RETENTION, RegistryValueKind.DWord);
			}
			SenderConfigKey = temp;
		}
		public void SetOptions(long multipartUploadFileSize, long multipartUploadPartSize, int threadCount, int logRetention)
		{
			MultipartUploadFileSize = multipartUploadFileSize;
			MultipartUploadPartSize = multipartUploadPartSize;
			ThreadCount = threadCount;
			LogRetention = logRetention;
		}

		public void Close() { SenderConfigKey?.Close(); }
		public void Delete()
		{
			SenderConfigKey.DeleteSubKeyTree(IfsSync2Constants.SENDER_CONFIG_PATH);
		}
#pragma warning restore CA1416
	}
}