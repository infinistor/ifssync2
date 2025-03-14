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
using System.Windows;
using IfsSync2Data;

namespace IfsSync2UI
{
	/// <summary>
	/// ConfigWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class ConfigWindow : Window
	{
		public ConfigWindow()
		{
			InitializeComponent();
			LoadConfig();
		}

		private void LoadConfig()
		{
			SenderConfig senderConfig = new();
			T_MultipartUploadSize.Text = MainData.SizeToString(senderConfig.MultipartUploadFileSize);
			T_MultipartPartSize.Text = MainData.SizeToString(senderConfig.MultipartUploadPartSize);
			T_ThreadCount.Text = senderConfig.ThreadCount.ToString();
		}

		public void Btn_Save(object sender, RoutedEventArgs e)
		{
			var multipartUploadFileSize = MainData.StringToSize(T_MultipartUploadSize.Text);
			var multipartUploadPartSize = MainData.StringToSize(T_MultipartPartSize.Text);
			var threadCount = int.Parse(T_ThreadCount.Text);
			if (multipartUploadFileSize == 0)
			{
				MessageBox.Show("MultipartUploadFileSize에 숫자를 입력해주세요.");
				return;
			}
			if (multipartUploadPartSize == 0)
			{
				MessageBox.Show("MultipartUploadPartSize에 숫자를 입력해주세요.");
				return;
			}
			if (threadCount < 1)
			{
				MessageBox.Show("ThreadCount에 1 이상의 숫자를 입력해주세요.");
				return;
			}

			if (multipartUploadFileSize < multipartUploadPartSize)
			{
				MessageBox.Show("MultipartUploadFileSize는 MultipartUploadPartSize보다 크거나 같아야 합니다.");
				return;
			}

			SenderConfig senderConfig = new(true);
			senderConfig.SetOptions(multipartUploadFileSize, multipartUploadPartSize, threadCount);

			MessageBox.Show("설정이 저장되었습니다.");
		}
		public void Btn_Close(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
