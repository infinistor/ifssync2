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
using Newtonsoft.Json;

namespace IfsSync2WatcherService.Models
{
	/// <summary>
	/// 사용자 정보 요청 모델
	/// </summary>
	public class UserInfoRequest
	{
		/// <summary>IP 주소</summary>
		[JsonProperty("ip")]
		public string Ip { get; set; }
		
		/// <summary>호스트명</summary>
		[JsonProperty("hostname")]
		public string Hostname { get; set; }
		
		/// <summary>포트</summary>
		[JsonProperty("port")]
		public int Port { get; set; }
		
		/// <summary>MAC 주소</summary>
		[JsonProperty("mac")]
		public string Mac { get; set; }
		
		/// <summary>운영체제</summary>
		[JsonProperty("os")]
		public string Os { get; set; }
		
		/// <summary>그룹</summary>
		[JsonProperty("group")]
		public string Group { get; set; }
		
		/// <summary>PC 이름</summary>
		[JsonProperty("pcName")]
		public string PcName { get; set; }
	}
} 