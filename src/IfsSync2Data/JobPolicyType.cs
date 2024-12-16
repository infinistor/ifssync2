namespace IfsSync2Data
{
	/// <summary> 작업 정책 목록 </summary>
	public enum JobPolicyType
	{
		/// <summary>Start immediately</summary>
		Now = -1,
		/// <summary>Real time upload</summary>
		RealTime = 0,
		/// <summary>Every few hours</summary>
		Schedule,
	}
}