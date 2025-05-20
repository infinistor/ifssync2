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
	/// 활성 상태 체크 요청 모델
	/// </summary>
	public class AliveRequest
	{
		/// <summary>사용자 ID</summary>
		[JsonProperty("userid")]
		public string UserId { get; set; }
		
		/// <summary>운영체제</summary>
		[JsonProperty("os")]
		public string Os { get; set; }
		
		/// <summary>Sender 활성 상태</summary>
		[JsonProperty("senderAlive")]
		public bool SenderAlive { get; set; }
		
		/// <summary>Listener 활성 상태</summary>
		[JsonProperty("listenAlive")]
		public bool ListenAlive { get; set; }
		
		/// <summary>초기화 상태</summary>
		[JsonProperty("iniStatus")]
		public bool IniStatus { get; set; }
		
		/// <summary>모니터링 항목 수</summary>
		[JsonProperty("monRemain")]
		public long MonRemain { get; set; }
		
		/// <summary>실패 항목 수</summary>
		[JsonProperty("failRemain")]
		public long FailRemain { get; set; }
		
		/// <summary>PC 이름</summary>
		[JsonProperty("pcName")]
		public string PcName { get; set; }
	}
} 