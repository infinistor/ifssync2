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

namespace IfsSync2UI
{
	public class S3Client
	{
		public const int S3_TIMEOUT = 3600;

		public static readonly string HEADER_DATA = "NONE";
		public static readonly string HEADER_BACKEND = "x-ifs-admin";
		public static readonly string HEADER_REPLICATION = "x-ifs-replication";
		public static readonly string HEADER_VERSION_ID = "x-ifs-version-id";

		public readonly AmazonS3Client Client = null;
		public S3Client(AmazonS3Client Client) => this.Client = Client;

		public S3Client(UserData User)
		{
			AWSCredentials Credentials;
			AmazonS3Config S3Config = null;

			if (User == null) Credentials = new AnonymousAWSCredentials();
			else
			{
				Credentials = new BasicAWSCredentials(User.AccessKey, User.SecretKey);
				if (User.URL == "")
				{
					S3Config = new AmazonS3Config()
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
					S3Config = new AmazonS3Config
					{
						ServiceURL = User.URL,
						Timeout = TimeSpan.FromSeconds(S3_TIMEOUT),
						MaxErrorRetry = 2,
						ForcePathStyle = true,
						UseHttp = true,
					};
				}
			}

			Client = new AmazonS3Client(Credentials, S3Config);
		}

		#region Bucket Function

		public ListBucketsResponse ListBuckets()
		{
			if (Client == null) return null;
			var Response = Client.ListBucketsAsync();
			Response.Wait();
			return Response.Result;
		}

		public PutBucketResponse PutBucket(PutBucketRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.PutBucketAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public PutBucketResponse PutBucket(string BucketName, S3CannedACL ACL = null, string RegionName = null,
			List<S3Grant> Grants = null, List<KeyValuePair<string, string>> HeaderList = null,
			bool? ObjectLockEnabledForBucket = null)
		{
			var Request = new PutBucketRequest() { BucketName = BucketName };
			if (ACL != null) Request.CannedACL = ACL;
			if (RegionName != null) Request.BucketRegionName = RegionName;
			if (HeaderList != null)
			{
				Client.BeforeRequestEvent += delegate (object sender, RequestEventArgs e)
				{

					var requestEvent = e as WebServiceRequestEventArgs;
					foreach (var Header in HeaderList)
						requestEvent.Headers.Add(Header.Key, Header.Value);
				};
			}
			if (Grants != null) Request.Grants = Grants;
			if (ObjectLockEnabledForBucket.HasValue) Request.ObjectLockEnabledForBucket = ObjectLockEnabledForBucket.Value;

			return PutBucket(Request);
		}

		public DeleteBucketResponse DeleteBucket(DeleteBucketRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.DeleteBucketAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public DeleteBucketResponse DeleteBucket(string BucketName)
		{
			var Request = new DeleteBucketRequest() { BucketName = BucketName };
			return DeleteBucket(Request);
		}

		public GetBucketLocationResponse GetBucketLocation(GetBucketLocationRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.GetBucketLocationAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public GetBucketLocationResponse GetBucketLocation(string BucketName)
		{
			var Request = new GetBucketLocationRequest() { BucketName = BucketName };
			return GetBucketLocation(Request);
		}

		private PutBucketLoggingResponse PutBucketLogging(PutBucketLoggingRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.PutBucketLoggingAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public PutBucketLoggingResponse PutBucketLogging(string BucketName, S3BucketLoggingConfig Config)
		{
			var Request = new PutBucketLoggingRequest() { BucketName = BucketName, LoggingConfig = Config };
			return PutBucketLogging(Request);
		}
		private GetBucketLoggingResponse GetBucketLogging(GetBucketLoggingRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.GetBucketLoggingAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public GetBucketLoggingResponse GetBucketLogging(string BucketName)
		{
			var Request = new GetBucketLoggingRequest() { BucketName = BucketName };
			return GetBucketLogging(Request);
		}

		private PutBucketNotificationResponse PutBucketNotification(PutBucketNotificationRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.PutBucketNotificationAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public PutBucketNotificationResponse PutBucketNotification(string BucketName, List<TopicConfiguration> TopicConfigurations = null, List<QueueConfiguration> QueueConfigurations = null, List<LambdaFunctionConfiguration> LambdaFunctionConfigurations = null, string ExpectedBucketOwner = null)
		{
			var Request = new PutBucketNotificationRequest() { BucketName = BucketName };
			if (TopicConfigurations != null) Request.TopicConfigurations = TopicConfigurations;
			if (QueueConfigurations != null) Request.QueueConfigurations = QueueConfigurations;
			if (LambdaFunctionConfigurations != null) Request.LambdaFunctionConfigurations = LambdaFunctionConfigurations;
			if (ExpectedBucketOwner != null) Request.ExpectedBucketOwner = ExpectedBucketOwner;

			return PutBucketNotification(Request);
		}
		private GetBucketNotificationResponse GetBucketNotification(GetBucketNotificationRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.GetBucketNotificationAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public GetBucketNotificationResponse GetBucketNotification(string BucketName)
		{
			var Request = new GetBucketNotificationRequest() { BucketName = BucketName };
			return GetBucketNotification(Request);
		}

		public PutBucketVersioningResponse PutBucketVersioning(PutBucketVersioningRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.PutBucketVersioningAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public PutBucketVersioningResponse PutBucketVersioning(string BucketName, VersionStatus Status = null)
		{
			var Request = new PutBucketVersioningRequest() { BucketName = BucketName, VersioningConfig = new S3BucketVersioningConfig() { Status = Status } };
			return PutBucketVersioning(Request);
		}

		public GetBucketVersioningResponse GetBucketVersioning(GetBucketVersioningRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.GetBucketVersioningAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public GetBucketVersioningResponse GetBucketVersioning(string BucketName)
		{
			var Request = new GetBucketVersioningRequest() { BucketName = BucketName };

			return GetBucketVersioning(Request);
		}

		public GetACLResponse GetACL(GetACLRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.GetACLAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public GetACLResponse GetBucketACL(string BucketName)
		{
			var Request = new GetACLRequest()
			{
				BucketName = BucketName
			};
			return GetACL(Request);
		}

		public PutACLResponse PutACL(PutACLRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.PutACLAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public PutACLResponse PutBucketACL(string BucketName, S3CannedACL ACL = null, S3AccessControlList AccessControlPolicy = null)
		{
			var Request = new PutACLRequest()
			{
				BucketName = BucketName
			};

			if (ACL != null) Request.CannedACL = ACL;
			if (AccessControlPolicy != null) Request.AccessControlList = AccessControlPolicy;

			return PutACL(Request);
		}

		public PutCORSConfigurationResponse PutCORSConfiguration(PutCORSConfigurationRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.PutCORSConfigurationAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public PutCORSConfigurationResponse PutCORSConfiguration(string BucketName, CORSConfiguration Configuration)
		{
			var Request = new PutCORSConfigurationRequest()
			{
				BucketName = BucketName,
				Configuration = Configuration,
			};

			return PutCORSConfiguration(Request);
		}

		public GetCORSConfigurationResponse GetCORSConfiguration(GetCORSConfigurationRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.GetCORSConfigurationAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public GetCORSConfigurationResponse GetCORSConfiguration(string BucketName)
		{
			var Request = new GetCORSConfigurationRequest()
			{
				BucketName = BucketName,
			};

			return GetCORSConfiguration(Request);
		}

		public DeleteCORSConfigurationResponse DeleteCORSConfiguration(DeleteCORSConfigurationRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.DeleteCORSConfigurationAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public DeleteCORSConfigurationResponse DeleteCORSConfiguration(string BucketName)
		{
			var Request = new DeleteCORSConfigurationRequest()
			{
				BucketName = BucketName,
			};

			return DeleteCORSConfiguration(Request);
		}

		public GetBucketTaggingResponse GetBucketTagging(GetBucketTaggingRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.GetBucketTaggingAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public GetBucketTaggingResponse GetBucketTagging(string BucketName)
		{
			var Request = new GetBucketTaggingRequest()
			{
				BucketName = BucketName,
			};

			return GetBucketTagging(Request);
		}

		public PutBucketTaggingResponse PutBucketTagging(PutBucketTaggingRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.PutBucketTaggingAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public PutBucketTaggingResponse PutBucketTagging(string BucketName, List<Tag> TagSet)
		{
			var Request = new PutBucketTaggingRequest()
			{
				BucketName = BucketName,
				TagSet = TagSet,
			};

			return PutBucketTagging(Request);
		}

		public DeleteBucketTaggingResponse DeleteBucketTagging(DeleteBucketTaggingRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.DeleteBucketTaggingAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public DeleteBucketTaggingResponse DeleteBucketTagging(string BucketName)
		{
			var Request = new DeleteBucketTaggingRequest()
			{
				BucketName = BucketName,
			};

			return DeleteBucketTagging(Request);
		}

		public PutLifecycleConfigurationResponse PutLifecycleConfiguration(PutLifecycleConfigurationRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.PutLifecycleConfigurationAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public PutLifecycleConfigurationResponse PutLifecycleConfiguration(string BucketName, LifecycleConfiguration Configuration)
		{
			var Request = new PutLifecycleConfigurationRequest()
			{
				BucketName = BucketName,
				Configuration = Configuration,
			};

			return PutLifecycleConfiguration(Request);
		}

		public GetLifecycleConfigurationResponse GetLifecycleConfiguration(GetLifecycleConfigurationRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.GetLifecycleConfigurationAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public GetLifecycleConfigurationResponse GetLifecycleConfiguration(string BucketName)
		{
			var Request = new GetLifecycleConfigurationRequest()
			{
				BucketName = BucketName,
			};

			return GetLifecycleConfiguration(Request);
		}

		public DeleteLifecycleConfigurationResponse DeleteLifecycleConfiguration(DeleteLifecycleConfigurationRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.DeleteLifecycleConfigurationAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public DeleteLifecycleConfigurationResponse DeleteLifecycleConfiguration(string BucketName)
		{
			var Request = new DeleteLifecycleConfigurationRequest()
			{
				BucketName = BucketName,
			};

			return DeleteLifecycleConfiguration(Request);
		}

		public PutBucketPolicyResponse PutBucketPolicy(PutBucketPolicyRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.PutBucketPolicyAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public PutBucketPolicyResponse PutBucketPolicy(string BucketName, string Policy)
		{
			var Request = new PutBucketPolicyRequest()
			{
				BucketName = BucketName,
				Policy = Policy,
			};

			return PutBucketPolicy(Request);
		}

		public GetBucketPolicyResponse GetBucketPolicy(GetBucketPolicyRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.GetBucketPolicyAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public GetBucketPolicyResponse GetBucketPolicy(string BucketName)
		{
			var Request = new GetBucketPolicyRequest()
			{
				BucketName = BucketName,
			};

			return GetBucketPolicy(Request);
		}

		public DeleteBucketPolicyResponse DeleteBucketPolicy(DeleteBucketPolicyRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.DeleteBucketPolicyAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public DeleteBucketPolicyResponse DeleteBucketPolicy(string BucketName)
		{
			var Request = new DeleteBucketPolicyRequest()
			{
				BucketName = BucketName,
			};

			return DeleteBucketPolicy(Request);
		}

		public GetBucketPolicyStatusResponse GetBucketPolicyStatus(GetBucketPolicyStatusRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.GetBucketPolicyStatusAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public GetBucketPolicyStatusResponse GetBucketPolicyStatus(string BucketName)
		{
			var Request = new GetBucketPolicyStatusRequest()
			{
				BucketName = BucketName,
			};

			return GetBucketPolicyStatus(Request);
		}

		public PutObjectLockConfigurationResponse PutObjectLockConfiguration(PutObjectLockConfigurationRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.PutObjectLockConfigurationAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public PutObjectLockConfigurationResponse PutObjectLockConfiguration(string BucketName, ObjectLockConfiguration ObjectLockConfiguration)
		{
			var Request = new PutObjectLockConfigurationRequest()
			{
				BucketName = BucketName,
				ObjectLockConfiguration = ObjectLockConfiguration,
			};

			return PutObjectLockConfiguration(Request);
		}

		public GetObjectLockConfigurationResponse GetObjectLockConfiguration(GetObjectLockConfigurationRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.GetObjectLockConfigurationAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public GetObjectLockConfigurationResponse GetObjectLockConfiguration(string BucketName)
		{
			var Request = new GetObjectLockConfigurationRequest()
			{
				BucketName = BucketName,
			};

			return GetObjectLockConfiguration(Request);
		}

		public PutPublicAccessBlockResponse PutPublicAccessBlock(PutPublicAccessBlockRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.PutPublicAccessBlockAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public PutPublicAccessBlockResponse PutPublicAccessBlock(string BucketName, PublicAccessBlockConfiguration PublicAccessBlockConfiguration)
		{
			var Request = new PutPublicAccessBlockRequest()
			{
				BucketName = BucketName,
				PublicAccessBlockConfiguration = PublicAccessBlockConfiguration,
			};

			return PutPublicAccessBlock(Request);
		}

		public GetPublicAccessBlockResponse GetPublicAccessBlock(GetPublicAccessBlockRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.GetPublicAccessBlockAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public GetPublicAccessBlockResponse GetPublicAccessBlock(string BucketName)
		{
			var Request = new GetPublicAccessBlockRequest()
			{
				BucketName = BucketName,
			};

			return GetPublicAccessBlock(Request);
		}

		public DeletePublicAccessBlockResponse DeletePublicAccessBlock(DeletePublicAccessBlockRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.DeletePublicAccessBlockAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public DeletePublicAccessBlockResponse DeletePublicAccessBlock(string BucketName)
		{
			var Request = new DeletePublicAccessBlockRequest()
			{
				BucketName = BucketName,
			};

			return DeletePublicAccessBlock(Request);
		}

		public GetBucketEncryptionResponse GetBucketEncryption(GetBucketEncryptionRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.GetBucketEncryptionAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public GetBucketEncryptionResponse GetBucketEncryption(string BucketName)
		{
			var Request = new GetBucketEncryptionRequest()
			{
				BucketName = BucketName
			};

			return GetBucketEncryption(Request);
		}

		public PutBucketEncryptionResponse PutBucketEncryption(PutBucketEncryptionRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.PutBucketEncryptionAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public PutBucketEncryptionResponse PutBucketEncryption(string BucketName, ServerSideEncryptionConfiguration SSEConfig)
		{
			var Request = new PutBucketEncryptionRequest()
			{
				BucketName = BucketName,
				ServerSideEncryptionConfiguration = SSEConfig,
			};

			return PutBucketEncryption(Request);
		}

		public DeleteBucketEncryptionResponse DeleteBucketEncryption(DeleteBucketEncryptionRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.DeleteBucketEncryptionAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public DeleteBucketEncryptionResponse DeleteBucketEncryption(string BucketName)
		{
			var Request = new DeleteBucketEncryptionRequest()
			{
				BucketName = BucketName
			};

			return DeleteBucketEncryption(Request);
		}

		public GetBucketWebsiteResponse GetBucketWebsite(GetBucketWebsiteRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.GetBucketWebsiteAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public GetBucketWebsiteResponse GetBucketWebsite(string BucketName)
		{
			var Request = new GetBucketWebsiteRequest()
			{
				BucketName = BucketName
			};

			return GetBucketWebsite(Request);
		}

		public PutBucketWebsiteResponse PutBucketWebsite(PutBucketWebsiteRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.PutBucketWebsiteAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public PutBucketWebsiteResponse PutBucketWebsite(string BucketName, WebsiteConfiguration WebConfig)
		{
			var Request = new PutBucketWebsiteRequest()
			{
				BucketName = BucketName,
				WebsiteConfiguration = WebConfig,
			};

			return PutBucketWebsite(Request);
		}

		public DeleteBucketWebsiteResponse DeleteBucketWebsite(DeleteBucketWebsiteRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.DeleteBucketWebsiteAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public DeleteBucketWebsiteResponse DeleteBucketWebsite(string BucketName)
		{
			var Request = new DeleteBucketWebsiteRequest()
			{
				BucketName = BucketName
			};

			return DeleteBucketWebsite(Request);
		}

		public GetBucketInventoryConfigurationResponse GetBucketInventoryConfiguration(GetBucketInventoryConfigurationRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.GetBucketInventoryConfigurationAsync(Request);
			Response.Wait();
			return Response.Result;
		}

		public GetBucketInventoryConfigurationResponse GetBucketInventoryConfiguration(string BucketName, string Id)
		{
			var Request = new GetBucketInventoryConfigurationRequest()
			{
				BucketName = BucketName,
				InventoryId = Id,
			};

			return GetBucketInventoryConfiguration(Request);
		}
		public ListBucketInventoryConfigurationsResponse ListBucketInventoryConfigurations(ListBucketInventoryConfigurationsRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.ListBucketInventoryConfigurationsAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public ListBucketInventoryConfigurationsResponse ListBucketInventoryConfigurations(string BucketName)
		{
			var Request = new ListBucketInventoryConfigurationsRequest()
			{
				BucketName = BucketName,
			};

			return ListBucketInventoryConfigurations(Request);
		}

		public PutBucketInventoryConfigurationResponse PutBucketInventoryConfiguration(PutBucketInventoryConfigurationRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.PutBucketInventoryConfigurationAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public PutBucketInventoryConfigurationResponse PutBucketInventoryConfiguration(string BucketName, string Id, InventoryConfiguration Config)
		{
			var Request = new PutBucketInventoryConfigurationRequest()
			{
				BucketName = BucketName,
				InventoryId = Id,
				InventoryConfiguration = Config,
			};

			return PutBucketInventoryConfiguration(Request);
		}
		#endregion

		#region Object Function
		public PutObjectResponse PutObject(PutObjectRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.PutObjectAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public PutObjectResponse PutObject(string BucketName, string Key, string FilePath = null, string Body = null, byte[] ByteBody = null, Stream InputStream = null)
		{
			var Request = new PutObjectRequest()
			{
				BucketName = BucketName,
				Key = Key,
			};

			if (Body != null) Request.ContentBody = Body;
			if (ByteBody != null)
			{
				Stream MyStream = new MemoryStream(ByteBody);
				Request.InputStream = MyStream;
			}
			if (InputStream != null) Request.InputStream = InputStream;
			if (FilePath != null) Request.FilePath = FilePath;

			return PutObject(Request);
		}

		public GetObjectResponse GetObject(GetObjectRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.GetObjectAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public GetObjectResponse GetObject(string BucketName, string Key, string VersionId = null, ByteRange Range = null)
		{
			var Request = new GetObjectRequest()
			{
				BucketName = BucketName,
				Key = Key,
			};

			if (VersionId != null) Request.VersionId = VersionId;
			if (Range != null) Request.ByteRange = Range;

			return GetObject(Request);
		}

		public CopyObjectResponse CopyObject(CopyObjectRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.CopyObjectAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public CopyObjectResponse CopyObject(string SourceBucket, string SourceKey, string DestinationBucket, string DestinationKey,
			List<KeyValuePair<string, string>> MetadataList = null, S3MetadataDirective MetadataDirective = S3MetadataDirective.COPY, ServerSideEncryptionMethod SSE_S3_Method = null,
			string VersionId = null, S3CannedACL ACL = null, string ETagToMatch = null, string ETagToNotMatch = null, string ContentType = null)
		{
			var Request = new CopyObjectRequest()
			{
				SourceBucket = SourceBucket,
				SourceKey = SourceKey,
				DestinationBucket = DestinationBucket,
				DestinationKey = DestinationKey,
				MetadataDirective = MetadataDirective,
			};
			if (ACL != null) Request.CannedACL = ACL;
			if (ContentType != null) Request.ContentType = ContentType;
			if (MetadataList != null)
			{
				foreach (var MetaData in MetadataList)
					Request.Metadata[MetaData.Key] = MetaData.Value;
			}
			if (VersionId != null) Request.SourceVersionId = VersionId;
			if (ETagToMatch != null) Request.ETagToMatch = ETagToMatch;
			if (ETagToNotMatch != null) Request.ETagToNotMatch = ETagToNotMatch;

			//SSE-S3
			if (SSE_S3_Method != null) Request.ServerSideEncryptionMethod = SSE_S3_Method;

			return CopyObject(Request);
		}

		public ListObjectsResponse ListObjects(ListObjectsRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.ListObjectsAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public ListObjectsResponse ListObjects(string BucketName, string Delimiter = null, string Marker = null,
											int MaxKeys = -1, string Prefix = null, string EncodingTypeName = null)
		{
			var Request = new ListObjectsRequest() { BucketName = BucketName };

			if (Delimiter != null) Request.Delimiter = Delimiter;
			if (Marker != null) Request.Marker = Marker;
			if (Prefix != null) Request.Prefix = Prefix;
			if (EncodingTypeName != null) Request.Encoding = new EncodingType(EncodingTypeName);

			if (MaxKeys >= 0) Request.MaxKeys = MaxKeys;

			return ListObjects(Request);

		}

		public ListObjectsV2Response ListObjectsV2(ListObjectsV2Request Request)
		{
			if (Client == null) return null;
			var Response = Client.ListObjectsV2Async(Request);
			Response.Wait();
			return Response.Result;
		}
		public ListObjectsV2Response ListObjectsV2(string BucketName, string Delimiter = null, string ContinuationToken = null,
					int MaxKeys = -1, string Prefix = null, string StartAfter = null, string EncodingTypeName = null,
					bool? FetchOwner = null)
		{
			var Request = new ListObjectsV2Request() { BucketName = BucketName };

			if (Delimiter != null) Request.Delimiter = Delimiter;
			if (ContinuationToken != null) Request.ContinuationToken = ContinuationToken;
			if (Prefix != null) Request.Prefix = Prefix;
			if (StartAfter != null) Request.StartAfter = StartAfter;
			if (EncodingTypeName != null) Request.Encoding = new EncodingType(EncodingTypeName);
			if (FetchOwner != null) Request.FetchOwner = FetchOwner.Value;

			if (MaxKeys >= 0) Request.MaxKeys = MaxKeys;

			return ListObjectsV2(Request);

		}

		public ListVersionsResponse ListVersions(ListVersionsRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.ListVersionsAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public ListVersionsResponse ListVersions(string BucketName, string NextKeyMarker = null, string NextVersionIdMarker = null, string Prefix = null, string Delimiter = null, int MaxKeys = 1000)
		{
			var Request = new ListVersionsRequest()
			{
				BucketName = BucketName,
				MaxKeys = MaxKeys
			};
			if (NextKeyMarker != null) Request.KeyMarker = NextKeyMarker;
			if (NextVersionIdMarker != null) Request.VersionIdMarker = NextVersionIdMarker;
			if (Prefix != null) Request.Prefix = Prefix;
			if (Delimiter != null) Request.Delimiter = Delimiter;

			return ListVersions(Request);
		}

		public DeleteObjectResponse DeleteObject(DeleteObjectRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.DeleteObjectAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public DeleteObjectResponse DeleteObject(string BucketName, string Key, string VersionId = null, bool BypassGovernanceRetention = true)
		{
			var Request = new DeleteObjectRequest()
			{
				BucketName = BucketName,
				Key = Key,
				VersionId = VersionId,
				BypassGovernanceRetention = BypassGovernanceRetention
			};

			return DeleteObject(Request);
		}

		public DeleteObjectsResponse DeleteObjects(DeleteObjectsRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.DeleteObjectsAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public DeleteObjectsResponse DeleteObjects(string BucketName, List<KeyVersion> KeyList, bool? BypassGovernanceRetention = null, bool? Quiet = null)
		{
			var Request = new DeleteObjectsRequest()
			{
				BucketName = BucketName,
				Objects = KeyList
			};

			if (BypassGovernanceRetention.HasValue) Request.BypassGovernanceRetention = BypassGovernanceRetention.Value;
			if (Quiet.HasValue) Request.Quiet = Quiet.Value;

			return DeleteObjects(Request);
		}

		public GetObjectMetadataResponse GetObjectMetadata(GetObjectMetadataRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.GetObjectMetadataAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public GetObjectMetadataResponse GetObjectMetadata(string BucketName, string Key, string VersionId = null)
		{
			var Request = new GetObjectMetadataRequest()
			{
				BucketName = BucketName,
				Key = Key
			};

			//Version
			if (VersionId != null) Request.VersionId = VersionId;

			return GetObjectMetadata(Request);
		}

		public GetACLResponse GetObjectACL(string BucketName, string Key, string VersionId = null)
		{
			var Request = new GetACLRequest()
			{
				BucketName = BucketName,
				Key = Key
			};

			if (VersionId != null) Request.VersionId = VersionId;

			return GetACL(Request);
		}

		public PutACLResponse PutObjectACL(string BucketName, string Key, S3CannedACL ACL = null, S3AccessControlList AccessControlPolicy = null)
		{
			var Request = new PutACLRequest()
			{
				BucketName = BucketName,
				Key = Key
			};
			if (ACL != null) Request.CannedACL = ACL;
			if (AccessControlPolicy != null) Request.AccessControlList = AccessControlPolicy;

			return PutACL(Request);
		}

		public GetObjectTaggingResponse GetObjectTagging(GetObjectTaggingRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.GetObjectTaggingAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public GetObjectTaggingResponse GetObjectTagging(string BucketName, string Key, string VersionId = null)
		{
			var Request = new GetObjectTaggingRequest()
			{
				BucketName = BucketName,
				Key = Key,
			};

			if (VersionId != null) Request.VersionId = VersionId;

			return GetObjectTagging(Request);
		}

		public PutObjectTaggingResponse PutObjectTagging(PutObjectTaggingRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.PutObjectTaggingAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public PutObjectTaggingResponse PutObjectTagging(string BucketName, string Key, Tagging Tagging)
		{
			var Request = new PutObjectTaggingRequest()
			{
				BucketName = BucketName,
				Key = Key,
				Tagging = Tagging,
			};

			return PutObjectTagging(Request);
		}

		public DeleteObjectTaggingResponse DeleteObjectTagging(DeleteObjectTaggingRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.DeleteObjectTaggingAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public DeleteObjectTaggingResponse DeleteObjectTagging(string BucketName, string Key)
		{
			var Request = new DeleteObjectTaggingRequest()
			{
				BucketName = BucketName,
				Key = Key,
			};

			return DeleteObjectTagging(Request);
		}

		public GetObjectRetentionResponse GetObjectRetention(GetObjectRetentionRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.GetObjectRetentionAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public GetObjectRetentionResponse GetObjectRetention(string BucketName, string Key, string VersionId = null)
		{
			var Request = new GetObjectRetentionRequest()
			{
				BucketName = BucketName,
				Key = Key,
			};

			if (VersionId != null) Request.VersionId = VersionId;

			return GetObjectRetention(Request);
		}

		public PutObjectRetentionResponse PutObjectRetention(PutObjectRetentionRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.PutObjectRetentionAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public PutObjectRetentionResponse PutObjectRetention(string BucketName, string Key, ObjectLockRetention Retention,
			string ContentMD5 = null, string VersionId = null, bool BypassGovernanceRetention = false)
		{
			var Request = new PutObjectRetentionRequest()
			{
				BucketName = BucketName,
				Key = Key,
				Retention = Retention,
				BypassGovernanceRetention = BypassGovernanceRetention
			};
			if (ContentMD5 != null) Request.ContentMD5 = ContentMD5;
			if (VersionId != null) Request.VersionId = VersionId;

			return PutObjectRetention(Request);
		}


		public PutObjectLegalHoldResponse PutObjectLegalHold(PutObjectLegalHoldRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.PutObjectLegalHoldAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public PutObjectLegalHoldResponse PutObjectLegalHold(string BucketName, string Key, ObjectLockLegalHold LegalHold)
		{
			var Request = new PutObjectLegalHoldRequest()
			{
				BucketName = BucketName,
				Key = Key,
				LegalHold = LegalHold,
			};

			return PutObjectLegalHold(Request);
		}

		public GetObjectLegalHoldResponse GetObjectLegalHold(GetObjectLegalHoldRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.GetObjectLegalHoldAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public GetObjectLegalHoldResponse GetObjectLegalHold(string BucketName, string Key, string VersionId = null)
		{
			var Request = new GetObjectLegalHoldRequest()
			{
				BucketName = BucketName,
				Key = Key,
			};

			if (VersionId != null) Request.VersionId = VersionId;

			return GetObjectLegalHold(Request);
		}


		public GetBucketReplicationResponse GetBucketReplication(GetBucketReplicationRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.GetBucketReplicationAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public GetBucketReplicationResponse GetBucketReplication(string BucketName)
		{
			var Request = new GetBucketReplicationRequest()
			{
				BucketName = BucketName,
			};

			return GetBucketReplication(Request);
		}

		public PutBucketReplicationResponse PutBucketReplication(PutBucketReplicationRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.PutBucketReplicationAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public PutBucketReplicationResponse PutBucketReplication(string BucketName, ReplicationConfiguration Configuration, string Token = null, string ExpectedBucketOwner = null)
		{
			var Request = new PutBucketReplicationRequest()
			{
				BucketName = BucketName,
				Configuration = Configuration
			};

			if (!string.IsNullOrWhiteSpace(Token)) Request.Token = Token;

			return PutBucketReplication(Request);
		}

		public DeleteBucketReplicationResponse DeleteBucketReplication(DeleteBucketReplicationRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.DeleteBucketReplicationAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public DeleteBucketReplicationResponse DeleteBucketReplication(string BucketName)
		{
			var Request = new DeleteBucketReplicationRequest()
			{
				BucketName = BucketName,
			};

			return DeleteBucketReplication(Request);
		}
		#endregion

		#region Multipart Function
		public InitiateMultipartUploadResponse InitiateMultipartUpload(InitiateMultipartUploadRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.InitiateMultipartUploadAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public InitiateMultipartUploadResponse InitiateMultipartUpload(string BucketName, string Key, string ContentType = null, List<KeyValuePair<string, string>> MetadataList = null)
		{
			var Request = new InitiateMultipartUploadRequest()
			{
				BucketName = BucketName,
				Key = Key
			};
			if (MetadataList != null)
			{
				foreach (var MetaData in MetadataList)
					Request.Metadata[MetaData.Key] = MetaData.Value;
			}
			if (ContentType != null) Request.ContentType = ContentType;

			return InitiateMultipartUpload(Request);
		}

		public UploadPartResponse UploadPart(UploadPartRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.UploadPartAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public UploadPartResponse UploadPart(string BucketName, string Key, string UploadId, int PartNumber, long PartSize = -1, string FilePath = null, long FilePosition = 0, Stream InputStream = null)
		{
			var Request = new UploadPartRequest()
			{
				BucketName = BucketName,
				Key = Key,
				PartNumber = PartNumber,
				UploadId = UploadId,
			};

			if (PartSize >= 0) Request.PartSize = PartSize;

			if (FilePath != null)
			{
				Request.FilePath = FilePath;
				Request.FilePosition = FilePosition;
			}
			if (InputStream != null) Request.InputStream = InputStream;

			return UploadPart(Request);
		}

		public CopyPartResponse CopyPart(CopyPartRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.CopyPartAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public CopyPartResponse CopyPart(string SourceBucket, string SourceKey, string DestinationBucket, string DestinationKey, string UploadId, int PartNumber, long Start, long End, string VersionId = null)
		{
			var Request = new CopyPartRequest()
			{
				SourceBucket = SourceBucket,
				SourceKey = SourceKey,
				DestinationBucket = DestinationBucket,
				DestinationKey = DestinationKey,
				UploadId = UploadId,
				PartNumber = PartNumber,
				FirstByte = Start,
				LastByte = End,
			};

			if (VersionId != null) Request.SourceVersionId = VersionId;

			return CopyPart(Request);
		}
		public CompleteMultipartUploadResponse CompleteMultipartUpload(CompleteMultipartUploadRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.CompleteMultipartUploadAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public CompleteMultipartUploadResponse CompleteMultipartUpload(string BucketName, string Key, string UploadId, List<PartETag> Parts)
		{
			var Request = new CompleteMultipartUploadRequest()
			{
				BucketName = BucketName,
				Key = Key,
				UploadId = UploadId,
				PartETags = Parts
			};

			return CompleteMultipartUpload(Request);
		}

		public AbortMultipartUploadResponse AbortMultipartUpload(AbortMultipartUploadRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.AbortMultipartUploadAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public AbortMultipartUploadResponse AbortMultipartUpload(string BucketName, string Key, string UploadId)
		{
			var Request = new AbortMultipartUploadRequest()
			{
				BucketName = BucketName,
				Key = Key,
				UploadId = UploadId,
			};

			return AbortMultipartUpload(Request);
		}

		public ListMultipartUploadsResponse ListMultipartUploads(ListMultipartUploadsRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.ListMultipartUploadsAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public ListMultipartUploadsResponse ListMultipartUploads(string BucketName, string Prefix = null, string Delimiter = null, int MaxKeys = -1,
			string UploadIdMarker = null, string KeyMarker = null)
		{
			var Request = new ListMultipartUploadsRequest()
			{
				BucketName = BucketName
			};

			if (Prefix != null) Request.Prefix = Prefix;
			if (Delimiter != null) Request.Delimiter = Delimiter;
			if (MaxKeys > 0) Request.MaxUploads = MaxKeys;
			if (UploadIdMarker != null) Request.UploadIdMarker = UploadIdMarker;
			if (KeyMarker != null) Request.KeyMarker = KeyMarker;

			return ListMultipartUploads(Request);
		}

		public ListPartsResponse ListParts(ListPartsRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.ListPartsAsync(Request);
			Response.Wait();
			return Response.Result;
		}
		public ListPartsResponse ListParts(string BucketName, string Key, string UploadId, int PartNumberMarker = 0, int MaxKeys = 0)
		{
			var Request = new ListPartsRequest()
			{
				BucketName = BucketName,
				Key = Key,
				UploadId = UploadId,
			};
			if (PartNumberMarker > 0) Request.PartNumberMarker = PartNumberMarker.ToString();
			if (MaxKeys > 0) Request.MaxParts = MaxKeys;

			return ListParts(Request);
		}
		#endregion

		#region TransferUtility Function
		public void Upload(string BucketName, string Key, string FilePath, long PartSize = 10 * 1024 * 1024, int ThreadCount = 10,
			Stream Body = null, byte[] ByteBody = null, string ContentType = null,
			List<Tag> TagSet = null, 
			List<KeyValuePair<string, string>> MetadataList = null, List<KeyValuePair<string, string>> HeaderList = null)
		{
			var TransferUtilityConfig = new TransferUtilityConfig()
			{
				MinSizeBeforePartUpload = PartSize,
				ConcurrentServiceRequests = ThreadCount,

			};
			var Transfer = new TransferUtility(Client, TransferUtilityConfig);

			var Request = new TransferUtilityUploadRequest()
			{
				BucketName = BucketName,
				FilePath = FilePath,
			};

			if (Key != null) Request.Key = Key;
			if (Body != null) Request.InputStream = Body;
			if (ByteBody != null)
			{
				Stream MyStream = new MemoryStream(ByteBody);
				Request.InputStream = MyStream;
			}
			if (FilePath != null) Request.FilePath = FilePath;
			if (ContentType != null) Request.ContentType = ContentType;
			if (MetadataList != null)
			{
				foreach (var MetaData in MetadataList)
					Request.Metadata[MetaData.Key] = MetaData.Value;
			}
			if (HeaderList != null)
			{
				foreach (var Header in HeaderList)
					Request.Headers[Header.Key] = Header.Value;
			}

			//Tag
			if (TagSet != null) Request.TagSet = TagSet;

			Transfer.Upload(Request);
		}

		public void Download(string BucketName, string Key, string FilePath, string VersionId = null)
		{
			TransferUtility Transfer = new(Client);

			var Request = new TransferUtilityDownloadRequest()
			{
				BucketName = BucketName,
				Key = Key,
				FilePath = FilePath,
			};

			if (VersionId != null) Request.VersionId = VersionId;

			Transfer.Download(Request);
		}

		public RestoreObjectResponse RestoreObject(RestoreObjectRequest Request)
		{
			if (Client == null) return null;
			var Response = Client.RestoreObjectAsync(Request);

			Response.Wait();
			return Response.Result;
		}

		public RestoreObjectResponse RestoreObject(string BucketName, string Key, string VersionId = null, int Days = -1)
		{
			var Request = new RestoreObjectRequest()
			{
				BucketName = BucketName,
				Key = Key,
			};
			if (VersionId != null) Request.VersionId = VersionId;
			if (Days > 0) Request.Days = Days;

			return RestoreObject(Request);
		}


		#endregion

		#region S3Util
		public bool DoesS3BucketExist(string BucketName)
		{
			try { return AmazonS3Util.DoesS3BucketExistV2Async(Client, BucketName).Result; }
			catch (Exception) { return false; }

		}
		#endregion

		#region ETC Function
		public string GeneratePresignedURL(string BucketName, string Key, DateTime Expires, HttpVerb Verb,
			ServerSideEncryptionMethod SSE_S3_Method = null, string ContentType = null)
		{
			var Request = new GetPreSignedUrlRequest()
			{
				BucketName = BucketName,
				Key = Key,
				Expires = Expires,
				Verb = Verb,
				Protocol = Protocol.HTTP
			};

			if (SSE_S3_Method != null) Request.ServerSideEncryptionMethod = SSE_S3_Method;
			if (ContentType != null) Request.ContentType = ContentType;

			return Client.GetPreSignedURL(Request);
		}
		#endregion
	}
}