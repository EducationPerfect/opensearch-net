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
	using System.Net;
	using Amazon;
	using Amazon.Runtime;

	public class AwsSigV4HttpConnection : HttpConnection
	{
		private readonly AWSCredentials _credentials;
		private readonly RegionEndpoint _region;

		public AwsSigV4HttpConnection(AWSCredentials credentials, RegionEndpoint region)
		{
			_credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
			_region = region ?? throw new ArgumentNullException(nameof(region));
		}

#if DOTNETCORE

		protected override System.Net.Http.HttpMessageHandler CreateHttpClientHandler(RequestData requestData) =>
			new AwsSigV4HttpClientHandler(_credentials, _region, base.CreateHttpClientHandler(requestData));

#else

		protected override HttpWebRequest CreateHttpWebRequest(RequestData requestData)
		{
			var request = base.CreateHttpWebRequest(requestData);
			AwsSigV4Util.SignRequest(request, requestData, _credentials.GetCredentials(), _region, DateTime.UtcNow);
			return request;
		}

#endif
	}
}
