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
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace IfsSync2UI
{
	class StorageUI
	{
		public readonly Grid Main;
		public readonly TextBlock StorageName;
		public readonly TextBlock URL;
		public readonly StackPanel Graph;
		public readonly Label SizeRate;
		public readonly Label Total;
		public readonly Label Used;
		public readonly Label Free;
		public readonly TextBox Box_StorageName;

		public readonly ToggleButton PopupButton = null;
		public readonly TextBox Box_URL = null;
		public readonly TextBox Box_AccessKey = null;
		public readonly TextBox Box_AccessSecret = null;
		public readonly TextBox Box_UserName = null;
		public readonly TextBox Box_S3FileManagerURL = null;
		public readonly TextBlock Black_AWSMessage = null;
		public readonly Grid Grid_Quota = null;

		public StorageUI(Grid _Main, TextBlock _StorageName, TextBlock _URL, StackPanel _Graph,
						 Label _SizeRate, Label _Total, Label _Used, Label _Free, TextBox _Box_StorageName, TextBox _Box_S3FileManagerURL)
		{
			Main = _Main;
			StorageName = _StorageName;
			URL = _URL;
			Graph = _Graph;
			SizeRate = _SizeRate;
			Total = _Total;
			Used = _Used;
			Free = _Free;
			Box_StorageName = _Box_StorageName;
			Box_S3FileManagerURL = _Box_S3FileManagerURL;
		}
		public StorageUI(Grid _Main, TextBlock _StorageName, TextBlock _URL, StackPanel _Graph,
						 Label _SizeRate, Label _Total, Label _Used, Label _Free, TextBox _Box_StorageName, TextBox _Box_S3FileManagerURL,
						 ToggleButton _PopupButton, TextBox _Box_URL, TextBox _Box_AccessKey,
						 TextBox _Box_AccessSecret, TextBox _Box_UserName,
						 TextBlock _Black_AWSMessage, Grid _Grid_Quota)
			: this(_Main, _StorageName, _URL, _Graph, _SizeRate, _Total, _Used, _Free, _Box_StorageName, _Box_S3FileManagerURL)
		{
			PopupButton = _PopupButton;
			Box_URL = _Box_URL;
			Box_AccessKey = _Box_AccessKey;
			Box_AccessSecret = _Box_AccessSecret;
			Box_UserName = _Box_UserName;
			Black_AWSMessage = _Black_AWSMessage;
			Grid_Quota = _Grid_Quota;
		}
	}
}
