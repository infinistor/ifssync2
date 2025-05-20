/*
* Copyright (c) 2021 PSPACE, inc. KSAN Development Team ksan@pspace.co.kr
* KSAN is a suite of free software: you can redistribute it and/or modify it under the terms of
* the GNU General Public License as published by the Free Software Foundation, either version
* 3 of the License.See LICENSE for details
*
* 본 프로그램 및 관련 소스코드, 문서 등 모든 자료는 있는 그대로 제공이 됩니다.
* KSAN 프로젝트의 개발자 및 개발사는 이 프로그램을 사용한 결과에 따른 어떠한 책임도 지지 않습니다.
* KSAN 개발팀은 사전 공지, 허락, 동의 없이 KSAN 개발에 관련된 모든 결과물에 대한 LICENSE 방식을 변경 할 권리가 있습니다.
*/
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace IfsSync2Common
{
	public class S3Client
	{
		public const int S3_TIMEOUT = 3600;

		public static readonly string HEADER_DATA = "NONE";
		public static readonly string HEADER_BACKEND = "x-ifs-admin";
		public static readonly string HEADER_REPLICATION = "x-ifs-replication";
		public static readonly string HEADER_VERSION_ID = "x-ifs-version-id";

		public readonly AmazonS3Client _client;
		public S3Client(UserData user)
		{
			if (user == null)
				throw new ArgumentNullException(nameof(user), "S3 클라이언트 생성에 필요한 사용자 데이터가 없습니다");

			BasicAWSCredentials credentials = new(user.AccessKey, user.SecretKey);
			AmazonS3Config s3Config = user.URL == "" ? new()
			{
				RegionEndpoint = RegionEndpoint.APNortheast2,
				Timeout = TimeSpan.FromSeconds(S3_TIMEOUT),
				MaxErrorRetry = 2,
				ForcePathStyle = true,
				UseHttp = true,
			} : new()
			{
				ServiceURL = user.URL,
				Timeout = TimeSpan.FromSeconds(S3_TIMEOUT),
				MaxErrorRetry = 2,
				ForcePathStyle = true,
				UseHttp = user.URL.StartsWith(IfsSync2Constants.HTTP, StringComparison.OrdinalIgnoreCase),
			};

			// HTTPS 연결 시 SSL 인증서 검증 회피 (IfsSync2UI 버전에서 추가된 기능)
			if (!s3Config.UseHttp)
				s3Config.HttpClientFactory = new AmazonS3HttpClientFactory();

			_client = new AmazonS3Client(credentials, s3Config);
			if (_client == null)
				throw new InvalidOperationException("S3 클라이언트 초기화에 실패했습니다");
		}

		// 익명 생성자 추가 (IfsSync2UI의 구현에서 가져옴)
		public S3Client(AmazonS3Client Client) => this._client = Client;

		#region Bucket Function

		public ListBucketsResponse ListBuckets()
		=> _client.ListBucketsAsync().GetAwaiter().GetResult();

		public PutBucketResponse PutBucket(PutBucketRequest request)
		=> _client.PutBucketAsync(request).GetAwaiter().GetResult();

		public PutBucketResponse PutBucket(string bucketName, S3CannedACL? acl = null, string? regionName = null,
			List<S3Grant>? grants = null, List<KeyValuePair<string, string>>? headerList = null,
			bool? objectLockEnabledForBucket = null)
		{
			var request = new PutBucketRequest() { BucketName = bucketName };
			if (acl != null) request.CannedACL = acl;
			if (regionName != null) request.BucketRegionName = regionName;
			if (headerList != null)
			{
				_client.BeforeRequestEvent += delegate (object sender, RequestEventArgs e)
				{
					if (e is WebServiceRequestEventArgs requestEvent)
					{
						foreach (var header in headerList)
							requestEvent.Headers.Add(header.Key, header.Value);
					}
				};
			}
			if (grants != null) request.Grants = grants;
			if (objectLockEnabledForBucket.HasValue) request.ObjectLockEnabledForBucket = objectLockEnabledForBucket.Value;

			return PutBucket(request);
		}

		public DeleteBucketResponse DeleteBucket(DeleteBucketRequest request)
		=> _client.DeleteBucketAsync(request).GetAwaiter().GetResult();
		public DeleteBucketResponse DeleteBucket(string bucketName)
		{
			var request = new DeleteBucketRequest() { BucketName = bucketName };
			return DeleteBucket(request);
		}

		public GetBucketLocationResponse GetBucketLocation(GetBucketLocationRequest request)
		=> _client.GetBucketLocationAsync(request).GetAwaiter().GetResult();
		public GetBucketLocationResponse GetBucketLocation(string bucketName)
		{
			var request = new GetBucketLocationRequest() { BucketName = bucketName };
			return GetBucketLocation(request);
		}

		private PutBucketLoggingResponse PutBucketLogging(PutBucketLoggingRequest request)
		=> _client.PutBucketLoggingAsync(request).GetAwaiter().GetResult();
		public PutBucketLoggingResponse PutBucketLogging(string bucketName, S3BucketLoggingConfig config)
		{
			var request = new PutBucketLoggingRequest() { BucketName = bucketName, LoggingConfig = config };
			return PutBucketLogging(request);
		}
		private GetBucketLoggingResponse GetBucketLogging(GetBucketLoggingRequest request)
		=> _client.GetBucketLoggingAsync(request).GetAwaiter().GetResult();
		public GetBucketLoggingResponse GetBucketLogging(string bucketName)
		{
			var request = new GetBucketLoggingRequest() { BucketName = bucketName };
			return GetBucketLogging(request);
		}

		private PutBucketNotificationResponse PutBucketNotification(PutBucketNotificationRequest request)
		=> _client.PutBucketNotificationAsync(request).GetAwaiter().GetResult();
		public PutBucketNotificationResponse PutBucketNotification(string bucketName, List<TopicConfiguration>? topicConfigurations = null, List<QueueConfiguration>? queueConfigurations = null, List<LambdaFunctionConfiguration>? lambdaFunctionConfigurations = null, string? expectedBucketOwner = null)
		{
			var request = new PutBucketNotificationRequest() { BucketName = bucketName };
			if (topicConfigurations != null) request.TopicConfigurations = topicConfigurations;
			if (queueConfigurations != null) request.QueueConfigurations = queueConfigurations;
			if (lambdaFunctionConfigurations != null) request.LambdaFunctionConfigurations = lambdaFunctionConfigurations;
			if (expectedBucketOwner != null) request.ExpectedBucketOwner = expectedBucketOwner;

			return PutBucketNotification(request);
		}
		private GetBucketNotificationResponse GetBucketNotification(GetBucketNotificationRequest request)
		=> _client.GetBucketNotificationAsync(request).GetAwaiter().GetResult();
		public GetBucketNotificationResponse GetBucketNotification(string bucketName)
		{
			var request = new GetBucketNotificationRequest() { BucketName = bucketName };
			return GetBucketNotification(request);
		}

		public PutBucketVersioningResponse PutBucketVersioning(PutBucketVersioningRequest request)
		=> _client.PutBucketVersioningAsync(request).GetAwaiter().GetResult();
		public PutBucketVersioningResponse PutBucketVersioning(string bucketName, VersionStatus? status = null)
		{
			var request = new PutBucketVersioningRequest() { BucketName = bucketName, VersioningConfig = new S3BucketVersioningConfig() { Status = status } };
			return PutBucketVersioning(request);
		}

		public GetBucketVersioningResponse GetBucketVersioning(GetBucketVersioningRequest request)
		=> _client.GetBucketVersioningAsync(request).GetAwaiter().GetResult();
		public GetBucketVersioningResponse GetBucketVersioning(string bucketName)
		{
			var request = new GetBucketVersioningRequest() { BucketName = bucketName };
			return GetBucketVersioning(request);
		}

		public GetBucketAclResponse GetBucketAcl(GetBucketAclRequest request)
		=> _client.GetBucketAclAsync(request).GetAwaiter().GetResult();
		public GetBucketAclResponse GetBucketAcl(string bucketName)
		{
			var request = new GetBucketAclRequest()
			{
				BucketName = bucketName
			};
			return GetBucketAcl(request);
		}

		public PutBucketAclResponse PutBucketAcl(PutBucketAclRequest request)
		=> _client.PutBucketAclAsync(request).GetAwaiter().GetResult();
		public PutBucketAclResponse PutBucketACL(string bucketName, S3CannedACL? acl = null, S3AccessControlList? accessControlPolicy = null)
		{
			var request = new PutBucketAclRequest()
			{
				BucketName = bucketName
			};

			if (acl != null) request.ACL = acl;
			if (accessControlPolicy != null) request.AccessControlPolicy = accessControlPolicy;

			return PutBucketAcl(request);
		}

		public PutCORSConfigurationResponse PutCORSConfiguration(PutCORSConfigurationRequest request)
		=> _client.PutCORSConfigurationAsync(request).GetAwaiter().GetResult();
		public PutCORSConfigurationResponse PutCORSConfiguration(string bucketName, CORSConfiguration configuration)
		{
			var request = new PutCORSConfigurationRequest()
			{
				BucketName = bucketName,
				Configuration = configuration,
			};
			return PutCORSConfiguration(request);
		}

		public GetCORSConfigurationResponse GetCORSConfiguration(GetCORSConfigurationRequest request)
		=> _client.GetCORSConfigurationAsync(request).GetAwaiter().GetResult();
		public GetCORSConfigurationResponse GetCORSConfiguration(string bucketName)
		{
			var request = new GetCORSConfigurationRequest()
			{
				BucketName = bucketName
			};
			return GetCORSConfiguration(request);
		}

		public DeleteCORSConfigurationResponse DeleteCORSConfiguration(DeleteCORSConfigurationRequest request)
		=> _client.DeleteCORSConfigurationAsync(request).GetAwaiter().GetResult();
		public DeleteCORSConfigurationResponse DeleteCORSConfiguration(string bucketName)
		{
			var request = new DeleteCORSConfigurationRequest()
			{
				BucketName = bucketName
			};
			return DeleteCORSConfiguration(request);
		}

		public GetBucketTaggingResponse GetBucketTagging(GetBucketTaggingRequest request)
		=> _client.GetBucketTaggingAsync(request).GetAwaiter().GetResult();
		public GetBucketTaggingResponse GetBucketTagging(string bucketName)
		{
			var request = new GetBucketTaggingRequest()
			{
				BucketName = bucketName
			};
			return GetBucketTagging(request);
		}

		public PutBucketTaggingResponse PutBucketTagging(PutBucketTaggingRequest request)
		=> _client.PutBucketTaggingAsync(request).GetAwaiter().GetResult();
		public PutBucketTaggingResponse PutBucketTagging(string bucketName, List<Tag> tagSet)
		{
			var request = new PutBucketTaggingRequest()
			{
				BucketName = bucketName,
				TagSet = tagSet
			};
			return PutBucketTagging(request);
		}

		public DeleteBucketTaggingResponse DeleteBucketTagging(DeleteBucketTaggingRequest request)
		=> _client.DeleteBucketTaggingAsync(request).GetAwaiter().GetResult();
		public DeleteBucketTaggingResponse DeleteBucketTagging(string bucketName)
		{
			var request = new DeleteBucketTaggingRequest()
			{
				BucketName = bucketName
			};
			return DeleteBucketTagging(request);
		}

		public PutLifecycleConfigurationResponse PutLifecycleConfiguration(PutLifecycleConfigurationRequest request)
		=> _client.PutLifecycleConfigurationAsync(request).GetAwaiter().GetResult();
		public PutLifecycleConfigurationResponse PutLifecycleConfiguration(string bucketName, LifecycleConfiguration configuration)
		{
			var request = new PutLifecycleConfigurationRequest()
			{
				BucketName = bucketName,
				Configuration = configuration
			};
			return PutLifecycleConfiguration(request);
		}

		public GetLifecycleConfigurationResponse GetLifecycleConfiguration(GetLifecycleConfigurationRequest request)
		=> _client.GetLifecycleConfigurationAsync(request).GetAwaiter().GetResult();
		public GetLifecycleConfigurationResponse GetLifecycleConfiguration(string bucketName)
		{
			var request = new GetLifecycleConfigurationRequest()
			{
				BucketName = bucketName
			};
			return GetLifecycleConfiguration(request);
		}

		public DeleteLifecycleConfigurationResponse DeleteLifecycleConfiguration(DeleteLifecycleConfigurationRequest request)
		=> _client.DeleteLifecycleConfigurationAsync(request).GetAwaiter().GetResult();
		public DeleteLifecycleConfigurationResponse DeleteLifecycleConfiguration(string bucketName)
		{
			var request = new DeleteLifecycleConfigurationRequest()
			{
				BucketName = bucketName
			};
			return DeleteLifecycleConfiguration(request);
		}

		public PutBucketPolicyResponse PutBucketPolicy(PutBucketPolicyRequest request)
		=> _client.PutBucketPolicyAsync(request).GetAwaiter().GetResult();
		public PutBucketPolicyResponse PutBucketPolicy(string bucketName, string policy)
		{
			var request = new PutBucketPolicyRequest()
			{
				BucketName = bucketName,
				Policy = policy
			};
			return PutBucketPolicy(request);
		}

		public GetBucketPolicyResponse GetBucketPolicy(GetBucketPolicyRequest request)
		=> _client.GetBucketPolicyAsync(request).GetAwaiter().GetResult();
		public GetBucketPolicyResponse GetBucketPolicy(string bucketName)
		{
			var request = new GetBucketPolicyRequest()
			{
				BucketName = bucketName
			};
			return GetBucketPolicy(request);
		}

		public DeleteBucketPolicyResponse DeleteBucketPolicy(DeleteBucketPolicyRequest request)
		=> _client.DeleteBucketPolicyAsync(request).GetAwaiter().GetResult();
		public DeleteBucketPolicyResponse DeleteBucketPolicy(string bucketName)
		{
			var request = new DeleteBucketPolicyRequest()
			{
				BucketName = bucketName
			};
			return DeleteBucketPolicy(request);
		}

		public GetBucketPolicyStatusResponse GetBucketPolicyStatus(GetBucketPolicyStatusRequest request)
		=> _client.GetBucketPolicyStatusAsync(request).GetAwaiter().GetResult();
		public GetBucketPolicyStatusResponse GetBucketPolicyStatus(string bucketName)
		{
			var request = new GetBucketPolicyStatusRequest()
			{
				BucketName = bucketName
			};
			return GetBucketPolicyStatus(request);
		}

		#endregion

		#region Object Function

		public PutObjectResponse PutObject(PutObjectRequest request)
		=> _client.PutObjectAsync(request).GetAwaiter().GetResult();
		public PutObjectResponse PutObject(string bucketName, string key, string? filePath = null, string? body = null, byte[]? byteBody = null, Stream? inputStream = null)
		{
			var request = new PutObjectRequest() { BucketName = bucketName, Key = key };
			if (filePath != null) request.FilePath = filePath;
			if (body != null) request.ContentBody = body;
			if (byteBody != null) request.InputStream = new MemoryStream(byteBody);
			if (inputStream != null) request.InputStream = inputStream;

			return PutObject(request);
		}

		public GetObjectResponse GetObject(GetObjectRequest request)
		=> _client.GetObjectAsync(request).GetAwaiter().GetResult();
		public GetObjectResponse GetObject(string bucketName, string key, string? versionId = null, ByteRange? range = null)
		{
			var request = new GetObjectRequest() { BucketName = bucketName, Key = key };
			if (versionId != null) request.VersionId = versionId;
			if (range != null) request.ByteRange = range;

			return GetObject(request);
		}

		public CopyObjectResponse CopyObject(CopyObjectRequest request)
		=> _client.CopyObjectAsync(request).GetAwaiter().GetResult();
		public CopyObjectResponse CopyObject(string sourceBucket, string sourceKey, string destinationBucket, string destinationKey,
			List<KeyValuePair<string, string>>? metadataList = null, S3MetadataDirective metadataDirective = S3MetadataDirective.COPY, ServerSideEncryptionMethod? sse_s3_method = null,
			string? versionId = null, S3CannedACL? acl = null, string? eTagToMatch = null, string? eTagToNotMatch = null, string? contentType = null)
		{
			var request = new CopyObjectRequest
			{
				SourceBucket = sourceBucket,
				SourceKey = sourceKey,
				DestinationBucket = destinationBucket,
				DestinationKey = destinationKey,
				MetadataDirective = metadataDirective,
			};

			if (metadataList != null) foreach (var item in metadataList) request.Metadata.Add(item.Key, item.Value);
			if (versionId != null) request.SourceVersionId = versionId;
			if (acl != null) request.CannedACL = acl;
			if (eTagToMatch != null) request.ETagToMatch = eTagToMatch;
			if (eTagToNotMatch != null) request.ETagToNotMatch = eTagToNotMatch;
			if (contentType != null) request.ContentType = contentType;
			if (sse_s3_method != null) request.ServerSideEncryptionMethod = sse_s3_method;

			return CopyObject(request);
		}

		public ListObjectsResponse ListObjects(ListObjectsRequest request)
		=> _client.ListObjectsAsync(request).GetAwaiter().GetResult();
		public ListObjectsResponse ListObjects(string bucketName, string? delimiter = null, string? marker = null,
											int maxKeys = -1, string? prefix = null, string? encodingTypeName = null)
		{
			var request = new ListObjectsRequest() { BucketName = bucketName };
			if (delimiter != null) request.Delimiter = delimiter;
			if (marker != null) request.Marker = marker;
			if (maxKeys > 0) request.MaxKeys = maxKeys;
			if (prefix != null) request.Prefix = prefix;

			return ListObjects(request);
		}

		public ListObjectsV2Response ListObjectsV2(ListObjectsV2Request request)
		=> _client.ListObjectsV2Async(request).GetAwaiter().GetResult();
		public ListObjectsV2Response ListObjectsV2(string bucketName, string? delimiter = null, string? continuationToken = null,
					int maxKeys = -1, string? prefix = null, string? startAfter = null, string? encodingTypeName = null,
					bool? fetchOwner = null)
		{
			var request = new ListObjectsV2Request() { BucketName = bucketName };

			if (delimiter != null) request.Delimiter = delimiter;
			if (continuationToken != null) request.ContinuationToken = continuationToken;
			if (maxKeys > 0) request.MaxKeys = maxKeys;
			if (prefix != null) request.Prefix = prefix;
			if (startAfter != null) request.StartAfter = startAfter;
			if (fetchOwner.HasValue) request.FetchOwner = fetchOwner.Value;

			return ListObjectsV2(request);
		}

		public ListVersionsResponse ListVersions(ListVersionsRequest request)
		=> _client.ListVersionsAsync(request).GetAwaiter().GetResult();
		public ListVersionsResponse ListVersions(string bucketName, string? nextKeyMarker = null, string? nextVersionIdMarker = null, string? prefix = null, string? delimiter = null, int maxKeys = 1000)
		{
			var request = new ListVersionsRequest()
			{
				BucketName = bucketName
			};
			if (nextKeyMarker != null) request.KeyMarker = nextKeyMarker;
			if (nextVersionIdMarker != null) request.VersionIdMarker = nextVersionIdMarker;
			if (prefix != null) request.Prefix = prefix;
			if (delimiter != null) request.Delimiter = delimiter;
			if (maxKeys != 1000) request.MaxKeys = maxKeys;

			return ListVersions(request);
		}

		public DeleteObjectResponse DeleteObject(DeleteObjectRequest request)
		=> _client.DeleteObjectAsync(request).GetAwaiter().GetResult();
		public DeleteObjectResponse DeleteObject(string bucketName, string key, string? versionId = null, bool bypassGovernanceRetention = true)
		{
			var request = new DeleteObjectRequest()
			{
				BucketName = bucketName,
				Key = key,
				BypassGovernanceRetention = bypassGovernanceRetention
			};
			if (versionId != null) request.VersionId = versionId;

			return DeleteObject(request);
		}

		public DeleteObjectsResponse DeleteObjects(DeleteObjectsRequest request)
		=> _client.DeleteObjectsAsync(request).GetAwaiter().GetResult();
		public DeleteObjectsResponse DeleteObjects(string bucketName, List<KeyVersion> keyList, bool? bypassGovernanceRetention = null, bool? quiet = null)
		{
			var request = new DeleteObjectsRequest
			{
				BucketName = bucketName,
				Objects = keyList
			};
			if (bypassGovernanceRetention.HasValue) request.BypassGovernanceRetention = bypassGovernanceRetention.Value;
			if (quiet.HasValue) request.Quiet = quiet.Value;

			return DeleteObjects(request);
		}

		public GetObjectMetadataResponse GetObjectMetadata(GetObjectMetadataRequest request)
		=> _client.GetObjectMetadataAsync(request).GetAwaiter().GetResult();
		public GetObjectMetadataResponse GetObjectMetadata(string bucketName, string key, string? versionId = null)
		{
			var request = new GetObjectMetadataRequest()
			{
				BucketName = bucketName,
				Key = key
			};
			if (versionId != null) request.VersionId = versionId;

			return GetObjectMetadata(request);
		}

		public GetObjectAclResponse GetObjectAcl(GetObjectAclRequest request)
		=> _client.GetObjectAclAsync(request).GetAwaiter().GetResult();
		public GetObjectAclResponse GetObjectAcl(string bucketName, string key, string? versionId = null)
		{
			var request = new GetObjectAclRequest()
			{
				BucketName = bucketName,
				Key = key
			};
			if (versionId != null) request.VersionId = versionId;

			return GetObjectAcl(request);
		}

		public PutObjectAclResponse PutObjectAcl(PutObjectAclRequest request)
		=> _client.PutObjectAclAsync(request).GetAwaiter().GetResult();
		public PutObjectAclResponse PutObjectAcl(string bucketName, string key, S3CannedACL? acl = null, S3AccessControlList? accessControlPolicy = null)
		{
			var request = new PutObjectAclRequest()
			{
				BucketName = bucketName,
				Key = key
			};

			if (acl != null) request.ACL = acl;
			if (accessControlPolicy != null) request.AccessControlPolicy = accessControlPolicy;

			return PutObjectAcl(request);
		}

		public GetObjectTaggingResponse GetObjectTagging(GetObjectTaggingRequest request)
		=> _client.GetObjectTaggingAsync(request).GetAwaiter().GetResult();
		public GetObjectTaggingResponse GetObjectTagging(string bucketName, string key, string? versionId = null)
		{
			var request = new GetObjectTaggingRequest()
			{
				BucketName = bucketName,
				Key = key
			};
			if (versionId != null) request.VersionId = versionId;

			return GetObjectTagging(request);
		}

		public PutObjectTaggingResponse PutObjectTagging(PutObjectTaggingRequest request)
		=> _client.PutObjectTaggingAsync(request).GetAwaiter().GetResult();
		public PutObjectTaggingResponse PutObjectTagging(string bucketName, string key, Tagging tagging)
		{
			var request = new PutObjectTaggingRequest()
			{
				BucketName = bucketName,
				Key = key,
				Tagging = tagging
			};

			return PutObjectTagging(request);
		}

		public DeleteObjectTaggingResponse DeleteObjectTagging(DeleteObjectTaggingRequest request)
		=> _client.DeleteObjectTaggingAsync(request).GetAwaiter().GetResult();
		public DeleteObjectTaggingResponse DeleteObjectTagging(string bucketName, string key)
		{
			var request = new DeleteObjectTaggingRequest()
			{
				BucketName = bucketName,
				Key = key
			};

			return DeleteObjectTagging(request);
		}

		#endregion

		#region Multipart Functions

		public InitiateMultipartUploadResponse InitiateMultipartUpload(InitiateMultipartUploadRequest request)
		=> _client.InitiateMultipartUploadAsync(request).GetAwaiter().GetResult();
		public InitiateMultipartUploadResponse InitiateMultipartUpload(string bucketName, string key, string? contentType = null, List<KeyValuePair<string, string>>? metadataList = null)
		{
			var request = new InitiateMultipartUploadRequest()
			{
				BucketName = bucketName,
				Key = key
			};
			if (contentType != null) request.ContentType = contentType;
			if (metadataList != null) foreach (var item in metadataList) request.Metadata.Add(item.Key, item.Value);

			return InitiateMultipartUpload(request);
		}

		public UploadPartResponse UploadPart(UploadPartRequest request)
		=> _client.UploadPartAsync(request).GetAwaiter().GetResult();
		public UploadPartResponse UploadPart(string bucketName, string key, string uploadId, int partNumber, long partSize = -1, string? filePath = null, long filePosition = 0, Stream? inputStream = null)
		{
			var request = new UploadPartRequest()
			{
				BucketName = bucketName,
				Key = key,
				UploadId = uploadId,
				PartNumber = partNumber
			};
			if (filePath != null)
			{
				request.FilePosition = filePosition;
				request.FilePath = filePath;
			}
			if (partSize > 0) request.PartSize = partSize;
			if (inputStream != null) request.InputStream = inputStream;

			return UploadPart(request);
		}

		public CopyPartResponse CopyPart(CopyPartRequest request)
		=> _client.CopyPartAsync(request).GetAwaiter().GetResult();
		public CopyPartResponse CopyPart(string sourceBucket, string sourceKey, string destinationBucket, string destinationKey, string uploadId, int partNumber, long start, long end, string? versionId = null)
		{
			var request = new CopyPartRequest()
			{
				SourceBucket = sourceBucket,
				SourceKey = sourceKey,
				DestinationBucket = destinationBucket,
				DestinationKey = destinationKey,
				UploadId = uploadId,
				PartNumber = partNumber,
				FirstByte = start,
				LastByte = end,
			};
			if (versionId != null) request.SourceVersionId = versionId;

			return CopyPart(request);
		}

		public CompleteMultipartUploadResponse CompleteMultipartUpload(CompleteMultipartUploadRequest request)
		=> _client.CompleteMultipartUploadAsync(request).GetAwaiter().GetResult();
		public CompleteMultipartUploadResponse CompleteMultipartUpload(string bucketName, string key, string uploadId, List<PartETag> parts)
		{
			var request = new CompleteMultipartUploadRequest()
			{
				BucketName = bucketName,
				Key = key,
				UploadId = uploadId,
				PartETags = parts
			};

			return CompleteMultipartUpload(request);
		}

		public AbortMultipartUploadResponse AbortMultipartUpload(AbortMultipartUploadRequest request)
		=> _client.AbortMultipartUploadAsync(request).GetAwaiter().GetResult();
		public AbortMultipartUploadResponse AbortMultipartUpload(string bucketName, string key, string uploadId)
		{
			var request = new AbortMultipartUploadRequest()
			{
				BucketName = bucketName,
				Key = key,
				UploadId = uploadId
			};

			return AbortMultipartUpload(request);
		}

		public ListMultipartUploadsResponse ListMultipartUploads(ListMultipartUploadsRequest request)
		=> _client.ListMultipartUploadsAsync(request).GetAwaiter().GetResult();
		public ListMultipartUploadsResponse ListMultipartUploads(string bucketName, string? prefix = null, string? delimiter = null, int maxKeys = -1,
			string? uploadIdMarker = null, string? keyMarker = null)
		{
			var request = new ListMultipartUploadsRequest()
			{
				BucketName = bucketName
			};
			if (prefix != null) request.Prefix = prefix;
			if (delimiter != null) request.Delimiter = delimiter;
			if (maxKeys > 0) request.MaxUploads = maxKeys;
			if (uploadIdMarker != null) request.UploadIdMarker = uploadIdMarker;
			if (keyMarker != null) request.KeyMarker = keyMarker;

			return ListMultipartUploads(request);
		}

		public ListPartsResponse ListParts(ListPartsRequest request)
		=> _client.ListPartsAsync(request).GetAwaiter().GetResult();
		public ListPartsResponse ListParts(string bucketName, string key, string uploadId, int partNumberMarker = 0, int maxKeys = 0)
		{
			var request = new ListPartsRequest()
			{
				BucketName = bucketName,
				Key = key,
				UploadId = uploadId
			};
			if (partNumberMarker > 0) request.PartNumberMarker = partNumberMarker.ToString();
			if (maxKeys > 0) request.MaxParts = maxKeys;

			return ListParts(request);
		}

		#endregion

		#region TransferUtility Functions

		public void Upload(string bucketName, string key, string filePath, int threadCount = 10,
			long minSizeBeforePartUpload = 100 * 1024 * 1024, long partSize = 10 * 1024 * 1024,
			Stream? body = null, byte[]? byteBody = null, string? contentType = null,
			List<Tag>? tagSet = null,
			List<KeyValuePair<string, string>>? metadataList = null, List<KeyValuePair<string, string>>? headerList = null)
		{
			var utility = new TransferUtility(_client);
			var config = new TransferUtilityUploadRequest()
			{
				BucketName = bucketName,
				Key = key,
			};
			if (filePath != null) config.FilePath = filePath;
			if (body != null) config.InputStream = body;
			if (byteBody != null) config.InputStream = new MemoryStream(byteBody);
			if (contentType != null) config.ContentType = contentType;
			config.PartSize = partSize;
			if (tagSet != null)
			{
				var tags = new Tagging
				{
					TagSet = tagSet
				};
				config.TagSet = tags.TagSet;
			}
			if (metadataList != null) foreach (var item in metadataList) config.Metadata.Add(item.Key, item.Value);
			if (headerList != null)
			{
				_client.BeforeRequestEvent += delegate (object sender, RequestEventArgs e)
				{
					var requestEvent = e as WebServiceRequestEventArgs;
					if (requestEvent != null)
					{
						foreach (var header in headerList)
							requestEvent.Headers.Add(header.Key, header.Value);
					}
				};
			}

			utility.UploadAsync(config).GetAwaiter().GetResult();
		}

		public void Download(string bucketName, string key, string filePath, string? versionId = null)
		{
			var utility = new TransferUtility(_client);
			var config = new TransferUtilityDownloadRequest()
			{
				BucketName = bucketName,
				Key = key,
				FilePath = filePath
			};
			if (versionId != null) config.VersionId = versionId;

			utility.DownloadAsync(config).GetAwaiter().GetResult();
		}

		#endregion

		#region ETC Functions

		public RestoreObjectResponse RestoreObject(RestoreObjectRequest request)
		=> _client.RestoreObjectAsync(request).GetAwaiter().GetResult();

		public RestoreObjectResponse RestoreObject(string bucketName, string key, string? versionId = null, int days = -1)
		{
			var request = new RestoreObjectRequest()
			{
				BucketName = bucketName,
				Key = key
			};
			if (versionId != null) request.VersionId = versionId;
			if (days > 0) request.Days = days;

			return RestoreObject(request);
		}

		#endregion

		#region S3Util Function

		public bool DoesS3BucketExist(string bucketName)
		{
			try
			{
				var response = _client.ListBucketsAsync().GetAwaiter().GetResult();
				if (response == null) return false;
				return response.Buckets.Exists(x => x.BucketName == bucketName);
			}
			catch (Exception) { return false; }
		}

		public string GeneratePresignedURL(string bucketName, string key, DateTime expires, HttpVerb verb,
			ServerSideEncryptionMethod? sse_s3_method = null, string? contentType = null)
		{
			var request = new GetPreSignedUrlRequest()
			{
				BucketName = bucketName,
				Key = key,
				Expires = expires,
				Verb = verb,
			};
			if (sse_s3_method != null) request.ServerSideEncryptionMethod = sse_s3_method;
			if (contentType != null) request.ContentType = contentType;

			return _client.GetPreSignedURL(request);
		}

		#endregion
	}

	public class AmazonS3HttpClientFactory : HttpClientFactory
	{
		private readonly HttpClientHandler _clientHandler;

		public AmazonS3HttpClientFactory()
		{
			_clientHandler = new HttpClientHandler
			{
				ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
				{
					// SSL 인증서 검증 오류 무시
					// 주의: 보안상 위험하므로 개발 환경에서만 사용하세요!
					return true;
				}
			};
		}

		public override HttpClient CreateHttpClient(IClientConfig clientConfig)
		{
			return new HttpClient(_clientHandler);
		}
	}
}