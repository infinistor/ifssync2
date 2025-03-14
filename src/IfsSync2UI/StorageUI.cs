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

		public StorageUI(Grid main, TextBlock storageName, TextBlock url, StackPanel graph,
						 Label sizeRate, Label total, Label used, Label free, TextBox boxStorageName, TextBox boxS3FileManagerUrl)
		{
			Main = main;
			StorageName = storageName;
			URL = url;
			Graph = graph;
			SizeRate = sizeRate;
			Total = total;
			Used = used;
			Free = free;
			Box_StorageName = boxStorageName;
			Box_S3FileManagerURL = boxS3FileManagerUrl;
		}
		public StorageUI(Grid main, TextBlock storageName, TextBlock url, StackPanel graph,
						 Label sizeRate, Label total, Label used, Label free, TextBox boxStorageName, TextBox boxS3FileManagerUrl,
						 ToggleButton popupButton, TextBox boxUrl, TextBox boxAccessKey,
						 TextBox boxAccessSecret, TextBox boxUserName,
						 TextBlock blackAwsMessage, Grid gridQuota)
			: this(main, storageName, url, graph, sizeRate, total, used, free, boxStorageName, boxS3FileManagerUrl)
		{
			PopupButton = popupButton;
			Box_URL = boxUrl;
			Box_AccessKey = boxAccessKey;
			Box_AccessSecret = boxAccessSecret;
			Box_UserName = boxUserName;
			Black_AWSMessage = blackAwsMessage;
			Grid_Quota = gridQuota;
		}
	}
}
