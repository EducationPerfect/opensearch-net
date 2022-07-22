/* SPDX-License-Identifier: Apache-2.0
*
* The OpenSearch Contributors require contributions made to
* this file be licensed under the Apache-2.0 license or a
* compatible open source license.
*
* Modifications Copyright OpenSearch Contributors. See
* GitHub history for details.
*
*  Licensed to Elasticsearch B.V. under one or more contributor
*  license agreements. See the NOTICE file distributed with
*  this work for additional information regarding copyright
*  ownership. Elasticsearch B.V. licenses this file to you under
*  the Apache License, Version 2.0 (the "License"); you may
*  not use this file except in compliance with the License.
*  You may obtain a copy of the License at
*
* 	http://www.apache.org/licenses/LICENSE-2.0
*
*  Unless required by applicable law or agreed to in writing,
*  software distributed under the License is distributed on an
*  "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
*  KIND, either express or implied.  See the License for the
*  specific language governing permissions and limitations
*  under the License.
*/

namespace OpenSearch.Net.AwsAuth
{
	using System;
#if DOTNETCORE
	using System.Net.Http;
	using System.Threading.Tasks;
#else
	using System.Net;
#endif
	using Amazon;
	using Amazon.Runtime;
	using Amazon.Runtime.Internal.Auth;

	public static class AwsSigV4Util
	{
#if DOTNETCORE
		public static async Task SignRequest(
			HttpRequestMessage request,
			ImmutableCredentials credentials,
			RegionEndpoint region,
			DateTime signingTime)
		{
			var canonicalRequest = await CanonicalRequest.From(request, credentials, signingTime).ConfigureAwait(false);

			var signature = AWS4Signer.ComputeSignature(credentials, region.SystemName, signingTime, "es", canonicalRequest.SignedHeaders,
				canonicalRequest.ToString());

			request.Headers.TryAddWithoutValidation("x-amz-date", canonicalRequest.XAmzDate);
			request.Headers.TryAddWithoutValidation("authorization", signature.ForAuthorizationHeader);
			if (!string.IsNullOrEmpty(canonicalRequest.XAmzSecurityToken)) request.Headers.TryAddWithoutValidation("x-amz-security-token", canonicalRequest.XAmzSecurityToken);
		}
#else
		public static void SignRequest(
			HttpWebRequest request,
			RequestData requestData,
			ImmutableCredentials credentials,
			RegionEndpoint region,
			DateTime signingTime)
		{
			var canonicalRequest = CanonicalRequest.From(request, requestData, credentials, signingTime);

			var signature = AWS4Signer.ComputeSignature(credentials, region.SystemName, signingTime, "es", canonicalRequest.SignedHeaders,
				canonicalRequest.ToString());

			request.Headers["x-amz-date"] = canonicalRequest.XAmzDate;
			request.Headers["authorization"] = signature.ForAuthorizationHeader;
			if (!string.IsNullOrEmpty(canonicalRequest.XAmzSecurityToken))
				request.Headers["x-amz-security-token"] = canonicalRequest.XAmzSecurityToken;
		}
#endif
	}
}
