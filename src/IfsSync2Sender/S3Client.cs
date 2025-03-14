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
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using IfsSync2Data;
using log4net;

namespace IfsSync2Sender
{
	public class S3Client
	{
		public const int S3_TIMEOUT = 3600;

		public static readonly string HEADER_DATA = "NONE";
		public static readonly string HEADER_BACKEND = "x-ifs-admin";
		public static readonly string HEADER_REPLICATION = "x-ifs-replication";
		public static readonly string HEADER_VERSION_ID = "x-ifs-version-id";

		public readonly AmazonS3Client _client = null;
		public S3Client(AmazonS3Client client) => _client = client;

		public S3Client(UserData user)
		{
			AWSCredentials credentials;
			AmazonS3Config s3Config = null;

			if (user == null) credentials = new AnonymousAWSCredentials();
			else
			{
				credentials = new BasicAWSCredentials(user.AccessKey, user.SecretKey);
				if (user.URL == "")
				{
					s3Config = new AmazonS3Config()
					{
						RegionEndpoint = RegionEndpoint.APNortheast2,
						Timeout = TimeSpan.FromSeconds(S3_TIMEOUT),
						MaxErrorRetry = 2,
						ForcePathStyle = true,
						UseHttp = true,
					};
				}
				else
				{
					s3Config = new AmazonS3Config
					{
						ServiceURL = user.URL,
						Timeout = TimeSpan.FromSeconds(S3_TIMEOUT),
						MaxErrorRetry = 2,
						ForcePathStyle = true,
						UseHttp = true,
					};
				}
			}

			_client = new AmazonS3Client(credentials, s3Config);
		}

		#region Bucket Function

		public ListBucketsResponse ListBuckets()
		{
			if (_client == null) return null;
			var response = _client.ListBucketsAsync();
			response.Wait();
			return response.Result;
		}

		public PutBucketResponse PutBucket(PutBucketRequest request)
		{
			if (_client == null) return null;
			var response = _client.PutBucketAsync(request);
			response.Wait();
			return response.Result;
		}
		public PutBucketResponse PutBucket(string bucketName, S3CannedACL acl = null, string regionName = null,
			List<S3Grant> grants = null, List<KeyValuePair<string, string>> headerList = null,
			bool? objectLockEnabledForBucket = null)
		{
			var request = new PutBucketRequest() { BucketName = bucketName };
			if (acl != null) request.CannedACL = acl;
			if (regionName != null) request.BucketRegionName = regionName;
			if (headerList != null)
			{
				_client.BeforeRequestEvent += delegate (object sender, RequestEventArgs e)
				{

					var requestEvent = e as WebServiceRequestEventArgs;
					foreach (var header in headerList)
						requestEvent.Headers.Add(header.Key, header.Value);
				};
			}
			if (grants != null) request.Grants = grants;
			if (objectLockEnabledForBucket.HasValue) request.ObjectLockEnabledForBucket = objectLockEnabledForBucket.Value;

			return PutBucket(request);
		}

		public DeleteBucketResponse DeleteBucket(DeleteBucketRequest request)
		{
			if (_client == null) return null;
			var response = _client.DeleteBucketAsync(request);
			response.Wait();
			return response.Result;
		}
		public DeleteBucketResponse DeleteBucket(string bucketName)
		{
			var request = new DeleteBucketRequest() { BucketName = bucketName };
			return DeleteBucket(request);
		}

		public GetBucketLocationResponse GetBucketLocation(GetBucketLocationRequest request)
		{
			if (_client == null) return null;
			var response = _client.GetBucketLocationAsync(request);
			response.Wait();
			return response.Result;
		}
		public GetBucketLocationResponse GetBucketLocation(string bucketName)
		{
			var request = new GetBucketLocationRequest() { BucketName = bucketName };
			return GetBucketLocation(request);
		}

		private PutBucketLoggingResponse PutBucketLogging(PutBucketLoggingRequest request)
		{
			if (_client == null) return null;
			var response = _client.PutBucketLoggingAsync(request);
			response.Wait();
			return response.Result;
		}
		public PutBucketLoggingResponse PutBucketLogging(string bucketName, S3BucketLoggingConfig config)
		{
			var request = new PutBucketLoggingRequest() { BucketName = bucketName, LoggingConfig = config };
			return PutBucketLogging(request);
		}
		private GetBucketLoggingResponse GetBucketLogging(GetBucketLoggingRequest request)
		{
			if (_client == null) return null;
			var response = _client.GetBucketLoggingAsync(request);
			response.Wait();
			return response.Result;
		}
		public GetBucketLoggingResponse GetBucketLogging(string bucketName)
		{
			var request = new GetBucketLoggingRequest() { BucketName = bucketName };
			return GetBucketLogging(request);
		}

		private PutBucketNotificationResponse PutBucketNotification(PutBucketNotificationRequest request)
		{
			if (_client == null) return null;
			var response = _client.PutBucketNotificationAsync(request);
			response.Wait();
			return response.Result;
		}
		public PutBucketNotificationResponse PutBucketNotification(string bucketName, List<TopicConfiguration> topicConfigurations = null, List<QueueConfiguration> queueConfigurations = null, List<LambdaFunctionConfiguration> lambdaFunctionConfigurations = null, string expectedBucketOwner = null)
		{
			var request = new PutBucketNotificationRequest() { BucketName = bucketName };
			if (topicConfigurations != null) request.TopicConfigurations = topicConfigurations;
			if (queueConfigurations != null) request.QueueConfigurations = queueConfigurations;
			if (lambdaFunctionConfigurations != null) request.LambdaFunctionConfigurations = lambdaFunctionConfigurations;
			if (expectedBucketOwner != null) request.ExpectedBucketOwner = expectedBucketOwner;

			return PutBucketNotification(request);
		}
		private GetBucketNotificationResponse GetBucketNotification(GetBucketNotificationRequest request)
		{
			if (_client == null) return null;
			var response = _client.GetBucketNotificationAsync(request);
			response.Wait();
			return response.Result;
		}
		public GetBucketNotificationResponse GetBucketNotification(string bucketName)
		{
			var request = new GetBucketNotificationRequest() { BucketName = bucketName };
			return GetBucketNotification(request);
		}

		public PutBucketVersioningResponse PutBucketVersioning(PutBucketVersioningRequest request)
		{
			if (_client == null) return null;
			var response = _client.PutBucketVersioningAsync(request);
			response.Wait();
			return response.Result;
		}
		public PutBucketVersioningResponse PutBucketVersioning(string bucketName, VersionStatus status = null)
		{
			var request = new PutBucketVersioningRequest() { BucketName = bucketName, VersioningConfig = new S3BucketVersioningConfig() { Status = status } };
			return PutBucketVersioning(request);
		}

		public GetBucketVersioningResponse GetBucketVersioning(GetBucketVersioningRequest request)
		{
			if (_client == null) return null;
			var response = _client.GetBucketVersioningAsync(request);
			response.Wait();
			return response.Result;
		}
		public GetBucketVersioningResponse GetBucketVersioning(string bucketName)
		{
			var request = new GetBucketVersioningRequest() { BucketName = bucketName };

			return GetBucketVersioning(request);
		}

		public GetACLResponse GetACL(GetACLRequest request)
		{
			if (_client == null) return null;
			var response = _client.GetACLAsync(request);
			response.Wait();
			return response.Result;
		}
		public GetACLResponse GetBucketACL(string bucketName)
		{
			var request = new GetACLRequest()
			{
				BucketName = bucketName
			};
			return GetACL(request);
		}

		public PutACLResponse PutACL(PutACLRequest request)
		{
			if (_client == null) return null;
			var response = _client.PutACLAsync(request);
			response.Wait();
			return response.Result;
		}
		public PutACLResponse PutBucketACL(string bucketName, S3CannedACL acl = null, S3AccessControlList accessControlPolicy = null)
		{
			var request = new PutACLRequest()
			{
				BucketName = bucketName
			};

			if (acl != null) request.CannedACL = acl;
			if (accessControlPolicy != null) request.AccessControlList = accessControlPolicy;

			return PutACL(request);
		}

		public PutCORSConfigurationResponse PutCORSConfiguration(PutCORSConfigurationRequest request)
		{
			if (_client == null) return null;
			var response = _client.PutCORSConfigurationAsync(request);
			response.Wait();
			return response.Result;
		}
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
		{
			if (_client == null) return null;
			var response = _client.GetCORSConfigurationAsync(request);
			response.Wait();
			return response.Result;
		}
		public GetCORSConfigurationResponse GetCORSConfiguration(string bucketName)
		{
			var request = new GetCORSConfigurationRequest()
			{
				BucketName = bucketName,
			};

			return GetCORSConfiguration(request);
		}

		public DeleteCORSConfigurationResponse DeleteCORSConfiguration(DeleteCORSConfigurationRequest request)
		{
			if (_client == null) return null;
			var response = _client.DeleteCORSConfigurationAsync(request);
			response.Wait();
			return response.Result;
		}
		public DeleteCORSConfigurationResponse DeleteCORSConfiguration(string bucketName)
		{
			var request = new DeleteCORSConfigurationRequest()
			{
				BucketName = bucketName,
			};

			return DeleteCORSConfiguration(request);
		}

		public GetBucketTaggingResponse GetBucketTagging(GetBucketTaggingRequest request)
		{
			if (_client == null) return null;
			var response = _client.GetBucketTaggingAsync(request);
			response.Wait();
			return response.Result;
		}
		public GetBucketTaggingResponse GetBucketTagging(string bucketName)
		{
			var request = new GetBucketTaggingRequest()
			{
				BucketName = bucketName,
			};

			return GetBucketTagging(request);
		}

		public PutBucketTaggingResponse PutBucketTagging(PutBucketTaggingRequest request)
		{
			if (_client == null) return null;
			var response = _client.PutBucketTaggingAsync(request);
			response.Wait();
			return response.Result;
		}
		public PutBucketTaggingResponse PutBucketTagging(string bucketName, List<Tag> tagSet)
		{
			var request = new PutBucketTaggingRequest()
			{
				BucketName = bucketName,
				TagSet = tagSet,
			};

			return PutBucketTagging(request);
		}

		public DeleteBucketTaggingResponse DeleteBucketTagging(DeleteBucketTaggingRequest request)
		{
			if (_client == null) return null;
			var response = _client.DeleteBucketTaggingAsync(request);
			response.Wait();
			return response.Result;
		}
		public DeleteBucketTaggingResponse DeleteBucketTagging(string bucketName)
		{
			var request = new DeleteBucketTaggingRequest()
			{
				BucketName = bucketName,
			};

			return DeleteBucketTagging(request);
		}

		public PutLifecycleConfigurationResponse PutLifecycleConfiguration(PutLifecycleConfigurationRequest request)
		{
			if (_client == null) return null;
			var response = _client.PutLifecycleConfigurationAsync(request);
			response.Wait();
			return response.Result;
		}
		public PutLifecycleConfigurationResponse PutLifecycleConfiguration(string bucketName, LifecycleConfiguration configuration)
		{
			var request = new PutLifecycleConfigurationRequest()
			{
				BucketName = bucketName,
				Configuration = configuration,
			};

			return PutLifecycleConfiguration(request);
		}

		public GetLifecycleConfigurationResponse GetLifecycleConfiguration(GetLifecycleConfigurationRequest request)
		{
			if (_client == null) return null;
			var response = _client.GetLifecycleConfigurationAsync(request);
			response.Wait();
			return response.Result;
		}
		public GetLifecycleConfigurationResponse GetLifecycleConfiguration(string bucketName)
		{
			var request = new GetLifecycleConfigurationRequest()
			{
				BucketName = bucketName,
			};

			return GetLifecycleConfiguration(request);
		}

		public DeleteLifecycleConfigurationResponse DeleteLifecycleConfiguration(DeleteLifecycleConfigurationRequest request)
		{
			if (_client == null) return null;
			var response = _client.DeleteLifecycleConfigurationAsync(request);
			response.Wait();
			return response.Result;
		}
		public DeleteLifecycleConfigurationResponse DeleteLifecycleConfiguration(string bucketName)
		{
			var request = new DeleteLifecycleConfigurationRequest()
			{
				BucketName = bucketName,
			};

			return DeleteLifecycleConfiguration(request);
		}

		public PutBucketPolicyResponse PutBucketPolicy(PutBucketPolicyRequest request)
		{
			if (_client == null) return null;
			var response = _client.PutBucketPolicyAsync(request);
			response.Wait();
			return response.Result;
		}
		public PutBucketPolicyResponse PutBucketPolicy(string bucketName, string policy)
		{
			var request = new PutBucketPolicyRequest()
			{
				BucketName = bucketName,
				Policy = policy,
			};

			return PutBucketPolicy(request);
		}

		public GetBucketPolicyResponse GetBucketPolicy(GetBucketPolicyRequest request)
		{
			if (_client == null) return null;
			var response = _client.GetBucketPolicyAsync(request);
			response.Wait();
			return response.Result;
		}
		public GetBucketPolicyResponse GetBucketPolicy(string bucketName)
		{
			var request = new GetBucketPolicyRequest()
			{
				BucketName = bucketName,
			};

			return GetBucketPolicy(request);
		}

		public DeleteBucketPolicyResponse DeleteBucketPolicy(DeleteBucketPolicyRequest request)
		{
			if (_client == null) return null;
			var response = _client.DeleteBucketPolicyAsync(request);
			response.Wait();
			return response.Result;
		}
		public DeleteBucketPolicyResponse DeleteBucketPolicy(string bucketName)
		{
			var request = new DeleteBucketPolicyRequest()
			{
				BucketName = bucketName,
			};

			return DeleteBucketPolicy(request);
		}

		public GetBucketPolicyStatusResponse GetBucketPolicyStatus(GetBucketPolicyStatusRequest request)
		{
			if (_client == null) return null;
			var response = _client.GetBucketPolicyStatusAsync(request);
			response.Wait();
			return response.Result;
		}
		public GetBucketPolicyStatusResponse GetBucketPolicyStatus(string bucketName)
		{
			var request = new GetBucketPolicyStatusRequest()
			{
				BucketName = bucketName,
			};

			return GetBucketPolicyStatus(request);
		}

		public PutObjectLockConfigurationResponse PutObjectLockConfiguration(PutObjectLockConfigurationRequest request)
		{
			if (_client == null) return null;
			var response = _client.PutObjectLockConfigurationAsync(request);
			response.Wait();
			return response.Result;
		}
		public PutObjectLockConfigurationResponse PutObjectLockConfiguration(string bucketName, ObjectLockConfiguration objectLockConfiguration)
		{
			var request = new PutObjectLockConfigurationRequest()
			{
				BucketName = bucketName,
				ObjectLockConfiguration = objectLockConfiguration,
			};

			return PutObjectLockConfiguration(request);
		}

		public GetObjectLockConfigurationResponse GetObjectLockConfiguration(GetObjectLockConfigurationRequest request)
		{
			if (_client == null) return null;
			var response = _client.GetObjectLockConfigurationAsync(request);
			response.Wait();
			return response.Result;
		}
		public GetObjectLockConfigurationResponse GetObjectLockConfiguration(string bucketName)
		{
			var request = new GetObjectLockConfigurationRequest()
			{
				BucketName = bucketName,
			};

			return GetObjectLockConfiguration(request);
		}

		public PutPublicAccessBlockResponse PutPublicAccessBlock(PutPublicAccessBlockRequest request)
		{
			if (_client == null) return null;
			var response = _client.PutPublicAccessBlockAsync(request);
			response.Wait();
			return response.Result;
		}
		public PutPublicAccessBlockResponse PutPublicAccessBlock(string bucketName, PublicAccessBlockConfiguration publicAccessBlockConfiguration)
		{
			var request = new PutPublicAccessBlockRequest()
			{
				BucketName = bucketName,
				PublicAccessBlockConfiguration = publicAccessBlockConfiguration,
			};

			return PutPublicAccessBlock(request);
		}

		public GetPublicAccessBlockResponse GetPublicAccessBlock(GetPublicAccessBlockRequest request)
		{
			if (_client == null) return null;
			var response = _client.GetPublicAccessBlockAsync(request);
			response.Wait();
			return response.Result;
		}
		public GetPublicAccessBlockResponse GetPublicAccessBlock(string bucketName)
		{
			var request = new GetPublicAccessBlockRequest()
			{
				BucketName = bucketName,
			};

			return GetPublicAccessBlock(request);
		}

		public DeletePublicAccessBlockResponse DeletePublicAccessBlock(DeletePublicAccessBlockRequest request)
		{
			if (_client == null) return null;
			var response = _client.DeletePublicAccessBlockAsync(request);
			response.Wait();
			return response.Result;
		}
		public DeletePublicAccessBlockResponse DeletePublicAccessBlock(string bucketName)
		{
			var request = new DeletePublicAccessBlockRequest()
			{
				BucketName = bucketName,
			};

			return DeletePublicAccessBlock(request);
		}

		public GetBucketEncryptionResponse GetBucketEncryption(GetBucketEncryptionRequest request)
		{
			if (_client == null) return null;
			var response = _client.GetBucketEncryptionAsync(request);
			response.Wait();
			return response.Result;
		}
		public GetBucketEncryptionResponse GetBucketEncryption(string bucketName)
		{
			var request = new GetBucketEncryptionRequest()
			{
				BucketName = bucketName
			};

			return GetBucketEncryption(request);
		}

		public PutBucketEncryptionResponse PutBucketEncryption(PutBucketEncryptionRequest request)
		{
			if (_client == null) return null;
			var response = _client.PutBucketEncryptionAsync(request);
			response.Wait();
			return response.Result;
		}
		public PutBucketEncryptionResponse PutBucketEncryption(string bucketName, ServerSideEncryptionConfiguration sseConfig)
		{
			var request = new PutBucketEncryptionRequest()
			{
				BucketName = bucketName,
				ServerSideEncryptionConfiguration = sseConfig,
			};

			return PutBucketEncryption(request);
		}

		public DeleteBucketEncryptionResponse DeleteBucketEncryption(DeleteBucketEncryptionRequest request)
		{
			if (_client == null) return null;
			var response = _client.DeleteBucketEncryptionAsync(request);
			response.Wait();
			return response.Result;
		}
		public DeleteBucketEncryptionResponse DeleteBucketEncryption(string bucketName)
		{
			var request = new DeleteBucketEncryptionRequest()
			{
				BucketName = bucketName
			};

			return DeleteBucketEncryption(request);
		}

		public GetBucketWebsiteResponse GetBucketWebsite(GetBucketWebsiteRequest request)
		{
			if (_client == null) return null;
			var response = _client.GetBucketWebsiteAsync(request);
			response.Wait();
			return response.Result;
		}
		public GetBucketWebsiteResponse GetBucketWebsite(string bucketName)
		{
			var request = new GetBucketWebsiteRequest()
			{
				BucketName = bucketName
			};

			return GetBucketWebsite(request);
		}

		public PutBucketWebsiteResponse PutBucketWebsite(PutBucketWebsiteRequest request)
		{
			if (_client == null) return null;
			var response = _client.PutBucketWebsiteAsync(request);
			response.Wait();
			return response.Result;
		}
		public PutBucketWebsiteResponse PutBucketWebsite(string bucketName, WebsiteConfiguration webConfig)
		{
			var request = new PutBucketWebsiteRequest()
			{
				BucketName = bucketName,
				WebsiteConfiguration = webConfig,
			};

			return PutBucketWebsite(request);
		}

		public DeleteBucketWebsiteResponse DeleteBucketWebsite(DeleteBucketWebsiteRequest request)
		{
			if (_client == null) return null;
			var response = _client.DeleteBucketWebsiteAsync(request);
			response.Wait();
			return response.Result;
		}
		public DeleteBucketWebsiteResponse DeleteBucketWebsite(string bucketName)
		{
			var request = new DeleteBucketWebsiteRequest()
			{
				BucketName = bucketName
			};

			return DeleteBucketWebsite(request);
		}

		public GetBucketInventoryConfigurationResponse GetBucketInventoryConfiguration(GetBucketInventoryConfigurationRequest request)
		{
			if (_client == null) return null;
			var response = _client.GetBucketInventoryConfigurationAsync(request);
			response.Wait();
			return response.Result;
		}

		public GetBucketInventoryConfigurationResponse GetBucketInventoryConfiguration(string bucketName, string id)
		{
			var request = new GetBucketInventoryConfigurationRequest()
			{
				BucketName = bucketName,
				InventoryId = id,
			};

			return GetBucketInventoryConfiguration(request);
		}
		public ListBucketInventoryConfigurationsResponse ListBucketInventoryConfigurations(ListBucketInventoryConfigurationsRequest request)
		{
			if (_client == null) return null;
			var response = _client.ListBucketInventoryConfigurationsAsync(request);
			response.Wait();
			return response.Result;
		}
		public ListBucketInventoryConfigurationsResponse ListBucketInventoryConfigurations(string bucketName)
		{
			var request = new ListBucketInventoryConfigurationsRequest()
			{
				BucketName = bucketName,
			};

			return ListBucketInventoryConfigurations(request);
		}

		public PutBucketInventoryConfigurationResponse PutBucketInventoryConfiguration(PutBucketInventoryConfigurationRequest request)
		{
			if (_client == null) return null;
			var response = _client.PutBucketInventoryConfigurationAsync(request);
			response.Wait();
			return response.Result;
		}
		public PutBucketInventoryConfigurationResponse PutBucketInventoryConfiguration(string bucketName, string id, InventoryConfiguration config)
		{
			var request = new PutBucketInventoryConfigurationRequest()
			{
				BucketName = bucketName,
				InventoryId = id,
				InventoryConfiguration = config,
			};

			return PutBucketInventoryConfiguration(request);
		}
		#endregion

		#region Object Function
		public PutObjectResponse PutObject(PutObjectRequest request)
		{
			if (_client == null) return null;
			var response = _client.PutObjectAsync(request);
			response.Wait();
			return response.Result;
		}
		public PutObjectResponse PutObject(string bucketName, string key, string filePath = null, string body = null, byte[] byteBody = null, Stream inputStream = null)
		{
			var request = new PutObjectRequest()
			{
				BucketName = bucketName,
				Key = key,
			};

			if (body != null) request.ContentBody = body;
			if (byteBody != null)
			{
				Stream myStream = new MemoryStream(byteBody);
				request.InputStream = myStream;
			}
			if (inputStream != null) request.InputStream = inputStream;
			if (filePath != null) request.FilePath = filePath;

			return PutObject(request);
		}

		public GetObjectResponse GetObject(GetObjectRequest request)
		{
			if (_client == null) return null;
			var response = _client.GetObjectAsync(request);
			response.Wait();
			return response.Result;
		}
		public GetObjectResponse GetObject(string bucketName, string key, string versionId = null, ByteRange range = null)
		{
			var request = new GetObjectRequest()
			{
				BucketName = bucketName,
				Key = key,
			};

			if (versionId != null) request.VersionId = versionId;
			if (range != null) request.ByteRange = range;

			return GetObject(request);
		}

		public CopyObjectResponse CopyObject(CopyObjectRequest request)
		{
			if (_client == null) return null;
			var response = _client.CopyObjectAsync(request);
			response.Wait();
			return response.Result;
		}
		public CopyObjectResponse CopyObject(string sourceBucket, string sourceKey, string destinationBucket, string destinationKey,
			List<KeyValuePair<string, string>> metadataList = null, S3MetadataDirective metadataDirective = S3MetadataDirective.COPY, ServerSideEncryptionMethod sse_s3_method = null,
			string versionId = null, S3CannedACL acl = null, string eTagToMatch = null, string eTagToNotMatch = null, string contentType = null)
		{
			var request = new CopyObjectRequest()
			{
				SourceBucket = sourceBucket,
				SourceKey = sourceKey,
				DestinationBucket = destinationBucket,
				DestinationKey = destinationKey,
				MetadataDirective = metadataDirective,
			};
			if (acl != null) request.CannedACL = acl;
			if (contentType != null) request.ContentType = contentType;
			if (metadataList != null)
			{
				foreach (var metaData in metadataList)
					request.Metadata[metaData.Key] = metaData.Value;
			}
			if (versionId != null) request.SourceVersionId = versionId;
			if (eTagToMatch != null) request.ETagToMatch = eTagToMatch;
			if (eTagToNotMatch != null) request.ETagToNotMatch = eTagToNotMatch;

			//SSE-S3
			if (sse_s3_method != null) request.ServerSideEncryptionMethod = sse_s3_method;

			return CopyObject(request);
		}

		public ListObjectsResponse ListObjects(ListObjectsRequest request)
		{
			if (_client == null) return null;
			var response = _client.ListObjectsAsync(request);
			response.Wait();
			return response.Result;
		}
		public ListObjectsResponse ListObjects(string bucketName, string delimiter = null, string marker = null,
											int maxKeys = -1, string prefix = null, string encodingTypeName = null)
		{
			var request = new ListObjectsRequest() { BucketName = bucketName };

			if (delimiter != null) request.Delimiter = delimiter;
			if (marker != null) request.Marker = marker;
			if (prefix != null) request.Prefix = prefix;
			if (encodingTypeName != null) request.Encoding = new EncodingType(encodingTypeName);

			if (maxKeys >= 0) request.MaxKeys = maxKeys;

			return ListObjects(request);

		}

		public ListObjectsV2Response ListObjectsV2(ListObjectsV2Request request)
		{
			if (_client == null) return null;
			var response = _client.ListObjectsV2Async(request);
			response.Wait();
			return response.Result;
		}
		public ListObjectsV2Response ListObjectsV2(string bucketName, string delimiter = null, string continuationToken = null,
					int maxKeys = -1, string prefix = null, string startAfter = null, string encodingTypeName = null,
					bool? fetchOwner = null)
		{
			var request = new ListObjectsV2Request() { BucketName = bucketName };

			if (delimiter != null) request.Delimiter = delimiter;
			if (continuationToken != null) request.ContinuationToken = continuationToken;
			if (prefix != null) request.Prefix = prefix;
			if (startAfter != null) request.StartAfter = startAfter;
			if (encodingTypeName != null) request.Encoding = new EncodingType(encodingTypeName);
			if (fetchOwner != null) request.FetchOwner = fetchOwner.Value;

			if (maxKeys >= 0) request.MaxKeys = maxKeys;

			return ListObjectsV2(request);

		}

		public ListVersionsResponse ListVersions(ListVersionsRequest request)
		{
			if (_client == null) return null;
			var response = _client.ListVersionsAsync(request);
			response.Wait();
			return response.Result;
		}
		public ListVersionsResponse ListVersions(string bucketName, string nextKeyMarker = null, string nextVersionIdMarker = null, string prefix = null, string delimiter = null, int maxKeys = 1000)
		{
			var request = new ListVersionsRequest()
			{
				BucketName = bucketName,
				MaxKeys = maxKeys
			};
			if (nextKeyMarker != null) request.KeyMarker = nextKeyMarker;
			if (nextVersionIdMarker != null) request.VersionIdMarker = nextVersionIdMarker;
			if (prefix != null) request.Prefix = prefix;
			if (delimiter != null) request.Delimiter = delimiter;

			return ListVersions(request);
		}

		public DeleteObjectResponse DeleteObject(DeleteObjectRequest request)
		{
			if (_client == null) return null;
			var response = _client.DeleteObjectAsync(request);
			response.Wait();
			return response.Result;
		}
		public DeleteObjectResponse DeleteObject(string bucketName, string key, string versionId = null, bool bypassGovernanceRetention = true)
		{
			var request = new DeleteObjectRequest()
			{
				BucketName = bucketName,
				Key = key,
				VersionId = versionId,
				BypassGovernanceRetention = bypassGovernanceRetention
			};

			return DeleteObject(request);
		}

		public DeleteObjectsResponse DeleteObjects(DeleteObjectsRequest request)
		{
			if (_client == null) return null;
			var response = _client.DeleteObjectsAsync(request);
			response.Wait();
			return response.Result;
		}
		public DeleteObjectsResponse DeleteObjects(string bucketName, List<KeyVersion> keyList, bool? bypassGovernanceRetention = null, bool? quiet = null)
		{
			var request = new DeleteObjectsRequest()
			{
				BucketName = bucketName,
				Objects = keyList
			};

			if (bypassGovernanceRetention.HasValue) request.BypassGovernanceRetention = bypassGovernanceRetention.Value;
			if (quiet.HasValue) request.Quiet = quiet.Value;

			return DeleteObjects(request);
		}

		public GetObjectMetadataResponse GetObjectMetadata(GetObjectMetadataRequest request)
		{
			if (_client == null) return null;
			var response = _client.GetObjectMetadataAsync(request);
			response.Wait();
			return response.Result;
		}
		public GetObjectMetadataResponse GetObjectMetadata(string bucketName, string key, string versionId = null)
		{
			var request = new GetObjectMetadataRequest()
			{
				BucketName = bucketName,
				Key = key
			};

			//Version
			if (versionId != null) request.VersionId = versionId;

			return GetObjectMetadata(request);
		}

		public GetACLResponse GetObjectACL(string bucketName, string key, string versionId = null)
		{
			var request = new GetACLRequest()
			{
				BucketName = bucketName,
				Key = key
			};

			if (versionId != null) request.VersionId = versionId;

			return GetACL(request);
		}

		public PutACLResponse PutObjectACL(string bucketName, string key, S3CannedACL acl = null, S3AccessControlList accessControlPolicy = null)
		{
			var request = new PutACLRequest()
			{
				BucketName = bucketName,
				Key = key
			};
			if (acl != null) request.CannedACL = acl;
			if (accessControlPolicy != null) request.AccessControlList = accessControlPolicy;

			return PutACL(request);
		}

		public GetObjectTaggingResponse GetObjectTagging(GetObjectTaggingRequest request)
		{
			if (_client == null) return null;
			var response = _client.GetObjectTaggingAsync(request);
			response.Wait();
			return response.Result;
		}
		public GetObjectTaggingResponse GetObjectTagging(string bucketName, string key, string versionId = null)
		{
			var request = new GetObjectTaggingRequest()
			{
				BucketName = bucketName,
				Key = key,
			};

			if (versionId != null) request.VersionId = versionId;

			return GetObjectTagging(request);
		}

		public PutObjectTaggingResponse PutObjectTagging(PutObjectTaggingRequest request)
		{
			if (_client == null) return null;
			var response = _client.PutObjectTaggingAsync(request);
			response.Wait();
			return response.Result;
		}
		public PutObjectTaggingResponse PutObjectTagging(string bucketName, string key, Tagging tagging)
		{
			var request = new PutObjectTaggingRequest()
			{
				BucketName = bucketName,
				Key = key,
				Tagging = tagging,
			};

			return PutObjectTagging(request);
		}

		public DeleteObjectTaggingResponse DeleteObjectTagging(DeleteObjectTaggingRequest request)
		{
			if (_client == null) return null;
			var response = _client.DeleteObjectTaggingAsync(request);
			response.Wait();
			return response.Result;
		}
		public DeleteObjectTaggingResponse DeleteObjectTagging(string bucketName, string key)
		{
			var request = new DeleteObjectTaggingRequest()
			{
				BucketName = bucketName,
				Key = key,
			};

			return DeleteObjectTagging(request);
		}

		public GetObjectRetentionResponse GetObjectRetention(GetObjectRetentionRequest request)
		{
			if (_client == null) return null;
			var response = _client.GetObjectRetentionAsync(request);
			response.Wait();
			return response.Result;
		}
		public GetObjectRetentionResponse GetObjectRetention(string bucketName, string key, string versionId = null)
		{
			var request = new GetObjectRetentionRequest()
			{
				BucketName = bucketName,
				Key = key,
			};

			if (versionId != null) request.VersionId = versionId;

			return GetObjectRetention(request);
		}

		public PutObjectRetentionResponse PutObjectRetention(PutObjectRetentionRequest request)
		{
			if (_client == null) return null;
			var response = _client.PutObjectRetentionAsync(request);
			response.Wait();
			return response.Result;
		}
		public PutObjectRetentionResponse PutObjectRetention(string bucketName, string key, ObjectLockRetention retention,
			string contentMD5 = null, string versionId = null, bool bypassGovernanceRetention = false)
		{
			var request = new PutObjectRetentionRequest()
			{
				BucketName = bucketName,
				Key = key,
				Retention = retention,
				BypassGovernanceRetention = bypassGovernanceRetention
			};
			if (contentMD5 != null) request.ContentMD5 = contentMD5;
			if (versionId != null) request.VersionId = versionId;

			return PutObjectRetention(request);
		}


		public PutObjectLegalHoldResponse PutObjectLegalHold(PutObjectLegalHoldRequest request)
		{
			if (_client == null) return null;
			var response = _client.PutObjectLegalHoldAsync(request);
			response.Wait();
			return response.Result;
		}
		public PutObjectLegalHoldResponse PutObjectLegalHold(string bucketName, string key, ObjectLockLegalHold legalHold)
		{
			var request = new PutObjectLegalHoldRequest()
			{
				BucketName = bucketName,
				Key = key,
				LegalHold = legalHold,
			};

			return PutObjectLegalHold(request);
		}

		public GetObjectLegalHoldResponse GetObjectLegalHold(GetObjectLegalHoldRequest request)
		{
			if (_client == null) return null;
			var response = _client.GetObjectLegalHoldAsync(request);
			response.Wait();
			return response.Result;
		}
		public GetObjectLegalHoldResponse GetObjectLegalHold(string bucketName, string key, string versionId = null)
		{
			var request = new GetObjectLegalHoldRequest()
			{
				BucketName = bucketName,
				Key = key,
			};

			if (versionId != null) request.VersionId = versionId;

			return GetObjectLegalHold(request);
		}


		public GetBucketReplicationResponse GetBucketReplication(GetBucketReplicationRequest request)
		{
			if (_client == null) return null;
			var response = _client.GetBucketReplicationAsync(request);
			response.Wait();
			return response.Result;
		}
		public GetBucketReplicationResponse GetBucketReplication(string bucketName)
		{
			var request = new GetBucketReplicationRequest()
			{
				BucketName = bucketName,
			};

			return GetBucketReplication(request);
		}

		public PutBucketReplicationResponse PutBucketReplication(PutBucketReplicationRequest request)
		{
			if (_client == null) return null;
			var response = _client.PutBucketReplicationAsync(request);
			response.Wait();
			return response.Result;
		}
		public PutBucketReplicationResponse PutBucketReplication(string bucketName, ReplicationConfiguration configuration, string token = null, string expectedBucketOwner = null)
		{
			var request = new PutBucketReplicationRequest()
			{
				BucketName = bucketName,
				Configuration = configuration
			};

			if (!string.IsNullOrWhiteSpace(token)) request.Token = token;

			return PutBucketReplication(request);
		}

		public DeleteBucketReplicationResponse DeleteBucketReplication(DeleteBucketReplicationRequest request)
		{
			if (_client == null) return null;
			var response = _client.DeleteBucketReplicationAsync(request);
			response.Wait();
			return response.Result;
		}
		public DeleteBucketReplicationResponse DeleteBucketReplication(string bucketName)
		{
			var request = new DeleteBucketReplicationRequest()
			{
				BucketName = bucketName,
			};

			return DeleteBucketReplication(request);
		}
		#endregion

		#region Multipart Function
		public InitiateMultipartUploadResponse InitiateMultipartUpload(InitiateMultipartUploadRequest request)
		{
			if (_client == null) return null;
			var response = _client.InitiateMultipartUploadAsync(request);
			response.Wait();
			return response.Result;
		}
		public InitiateMultipartUploadResponse InitiateMultipartUpload(string bucketName, string key, string contentType = null, List<KeyValuePair<string, string>> metadataList = null)
		{
			var request = new InitiateMultipartUploadRequest()
			{
				BucketName = bucketName,
				Key = key
			};
			if (metadataList != null)
			{
				foreach (var metaData in metadataList)
					request.Metadata[metaData.Key] = metaData.Value;
			}
			if (contentType != null) request.ContentType = contentType;

			return InitiateMultipartUpload(request);
		}

		public UploadPartResponse UploadPart(UploadPartRequest request)
		{
			if (_client == null) return null;
			var response = _client.UploadPartAsync(request);
			response.Wait();
			return response.Result;
		}
		public UploadPartResponse UploadPart(string bucketName, string key, string uploadId, int partNumber, long partSize = -1, string filePath = null, long filePosition = 0, Stream inputStream = null)
		{
			var request = new UploadPartRequest()
			{
				BucketName = bucketName,
				Key = key,
				PartNumber = partNumber,
				UploadId = uploadId,
			};

			if (partSize >= 0) request.PartSize = partSize;

			if (filePath != null)
			{
				request.FilePath = filePath;
				request.FilePosition = filePosition;
			}
			if (inputStream != null) request.InputStream = inputStream;

			return UploadPart(request);
		}

		public CopyPartResponse CopyPart(CopyPartRequest request)
		{
			if (_client == null) return null;
			var response = _client.CopyPartAsync(request);
			response.Wait();
			return response.Result;
		}
		public CopyPartResponse CopyPart(string sourceBucket, string sourceKey, string destinationBucket, string destinationKey, string uploadId, int partNumber, long start, long end, string versionId = null)
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
		{
			if (_client == null) return null;
			var response = _client.CompleteMultipartUploadAsync(request);
			response.Wait();
			return response.Result;
		}
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
		{
			if (_client == null) return null;
			var response = _client.AbortMultipartUploadAsync(request);
			response.Wait();
			return response.Result;
		}
		public AbortMultipartUploadResponse AbortMultipartUpload(string bucketName, string key, string uploadId)
		{
			var request = new AbortMultipartUploadRequest()
			{
				BucketName = bucketName,
				Key = key,
				UploadId = uploadId,
			};

			return AbortMultipartUpload(request);
		}

		public ListMultipartUploadsResponse ListMultipartUploads(ListMultipartUploadsRequest request)
		{
			if (_client == null) return null;
			var response = _client.ListMultipartUploadsAsync(request);
			response.Wait();
			return response.Result;
		}
		public ListMultipartUploadsResponse ListMultipartUploads(string bucketName, string prefix = null, string delimiter = null, int maxKeys = -1,
			string uploadIdMarker = null, string keyMarker = null)
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
		{
			if (_client == null) return null;
			var response = _client.ListPartsAsync(request);
			response.Wait();
			return response.Result;
		}
		public ListPartsResponse ListParts(string bucketName, string key, string uploadId, int partNumberMarker = 0, int maxKeys = 0)
		{
			var request = new ListPartsRequest()
			{
				BucketName = bucketName,
				Key = key,
				UploadId = uploadId,
			};
			if (partNumberMarker > 0) request.PartNumberMarker = partNumberMarker.ToString();
			if (maxKeys > 0) request.MaxParts = maxKeys;

			return ListParts(request);
		}
		#endregion

		#region TransferUtility Function
		public void Upload(string bucketName, string key, string filePath, int threadCount = 10,
			long minSizeBeforePartUpload = 100 * 1024 * 1024, long partSize = 10 * 1024 * 1024,
			Stream body = null, byte[] byteBody = null, string contentType = null,
			List<Tag> tagSet = null,
			List<KeyValuePair<string, string>> metadataList = null, List<KeyValuePair<string, string>> headerList = null)
		{
			var transferUtilityConfig = new TransferUtilityConfig()
			{
				MinSizeBeforePartUpload = minSizeBeforePartUpload,
				ConcurrentServiceRequests = threadCount,
			};
			var transfer = new TransferUtility(_client, transferUtilityConfig);

			var request = new TransferUtilityUploadRequest()
			{
				BucketName = bucketName,
				FilePath = filePath,
				PartSize = partSize,
				ChecksumAlgorithm = ChecksumAlgorithm.CRC32C,
			};

			if (key != null) request.Key = key;
			if (body != null) request.InputStream = body;
			if (byteBody != null)
			{
				Stream myStream = new MemoryStream(byteBody);
				request.InputStream = myStream;
			}
			if (filePath != null) request.FilePath = filePath;
			if (contentType != null) request.ContentType = contentType;
			if (metadataList != null)
			{
				foreach (var metaData in metadataList)
					request.Metadata[metaData.Key] = metaData.Value;
			}
			if (headerList != null)
			{
				foreach (var header in headerList)
					request.Headers[header.Key] = header.Value;
			}

			//Tag
			if (tagSet != null) request.TagSet = tagSet;

			transfer.Upload(request);
		}

		public void Download(string bucketName, string key, string filePath, string versionId = null)
		{
			TransferUtility transfer = new(_client);

			var request = new TransferUtilityDownloadRequest()
			{
				BucketName = bucketName,
				Key = key,
				FilePath = filePath,
			};

			if (versionId != null) request.VersionId = versionId;

			transfer.Download(request);
		}

		public RestoreObjectResponse RestoreObject(RestoreObjectRequest request)
		{
			if (_client == null) return null;
			var response = _client.RestoreObjectAsync(request);

			response.Wait();
			return response.Result;
		}

		public RestoreObjectResponse RestoreObject(string bucketName, string key, string versionId = null, int days = -1)
		{
			var request = new RestoreObjectRequest()
			{
				BucketName = bucketName,
				Key = key,
			};
			if (versionId != null) request.VersionId = versionId;
			if (days > 0) request.Days = days;

			return RestoreObject(request);
		}


		#endregion

		#region S3Util
		public bool DoesS3BucketExist(string bucketName)
		{
			try { return AmazonS3Util.DoesS3BucketExistV2Async(_client, bucketName).Result; }
			catch (Exception) { return false; }

		}
		#endregion

		#region ETC Function
		public string GeneratePresignedURL(string bucketName, string key, DateTime expires, HttpVerb verb,
			ServerSideEncryptionMethod sse_s3_method = null, string contentType = null)
		{
			var request = new GetPreSignedUrlRequest()
			{
				BucketName = bucketName,
				Key = key,
				Expires = expires,
				Verb = verb,
				Protocol = Protocol.HTTP
			};

			if (sse_s3_method != null) request.ServerSideEncryptionMethod = sse_s3_method;
			if (contentType != null) request.ContentType = contentType;

			return _client.GetPreSignedURL(request);
		}
		#endregion
	}
}