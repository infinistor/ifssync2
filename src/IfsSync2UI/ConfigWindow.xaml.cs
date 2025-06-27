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
using IfsSync2Common;

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

		private static int String2LogRetention(string str)
		{
			return str switch
			{
				"Unlimited" => 0,
				"1 Week" => 7,
				"2 Weeks" => 14,
				"3 Weeks" => 21,
				"1 Month" => 30,
				"2 Months" => 60,
				"3 Months" => 90,
				"6 Months" => 180,
				"1 Year" => 365,
				_ => 0,
			};
		}

		private static string LogRetention2String(int logRetention)
		{
			return logRetention switch
			{
				0 => "Unlimited",
				7 => "1 Week",
				14 => "2 Weeks",
				21 => "3 Weeks",
				30 => "1 Month",
				60 => "2 Months",
				90 => "3 Months",
				180 => "6 Months",
				365 => "1 Year",
				_ => "Unlimited",
			};
		}

		private void LoadConfig()
		{
			SenderConfig senderConfig = new();
			T_MultipartUploadSize.Text = CapacityUnit.Format(senderConfig.MultipartUploadFileSize);
			T_MultipartPartSize.Text = CapacityUnit.Format(senderConfig.MultipartUploadPartSize);
			T_ThreadCount.Text = senderConfig.ThreadCount.ToString();
			T_RetryDelay.Text = senderConfig.RetryDelay.ToString();
			CB_LogRetention.SelectedValue = LogRetention2String(senderConfig.LogRetention);
		}

		public void Btn_Save(object sender, RoutedEventArgs e)
		{
			if (!CapacityUnit.TryParse(T_MultipartUploadSize.Text, out var multipartUploadFileSize))
			{
				MessageBox.Show("MultipartUploadFileSize에 숫자를 입력해주세요.");
				return;
			}

			if (!CapacityUnit.TryParse(T_MultipartPartSize.Text, out var multipartUploadPartSize))
			{
				MessageBox.Show("MultipartUploadPartSize에 숫자를 입력해주세요.");
				return;
			}

			if (!int.TryParse(T_ThreadCount.Text, out var threadCount) || threadCount < 1)
			{
				MessageBox.Show("ThreadCount에 1 이상의 숫자를 입력해주세요.");
				return;
			}

			if (!int.TryParse(T_RetryDelay.Text, out var retryDelay) || retryDelay < 1)
			{
				MessageBox.Show("RetryDelay에 1 이상의 숫자를 입력해주세요.");
				return;
			}

			if (CB_LogRetention.SelectedValue == null)
			{
				MessageBox.Show("로그 보관 기간을 선택해주세요.");
				return;
			}

			var logRetention = String2LogRetention(CB_LogRetention.SelectedValue.ToString());

			if (multipartUploadFileSize < multipartUploadPartSize)
			{
				MessageBox.Show("MultipartUploadFileSize는 MultipartUploadPartSize보다 크거나 같아야 합니다.");
				return;
			}

			SenderConfig senderConfig = new(true);
			senderConfig.SetOptions(multipartUploadFileSize, multipartUploadPartSize, threadCount, logRetention, retryDelay);

			MessageBox.Show("설정이 저장되었습니다.");
		}

		public void Btn_Close(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
