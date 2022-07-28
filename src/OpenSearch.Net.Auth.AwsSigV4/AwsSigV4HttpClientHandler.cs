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

#if DOTNETCORE

namespace OpenSearch.Net.Auth.AwsSigV4
{
	using System;
	using System.Net.Http;
	using System.Threading;
	using System.Threading.Tasks;
	using Amazon;
	using Amazon.Runtime;

	internal class AwsSigV4HttpClientHandler : DelegatingHandler
	{
		private readonly AWSCredentials _credentials;
		private readonly RegionEndpoint _region;

		public AwsSigV4HttpClientHandler(AWSCredentials credentials, RegionEndpoint region, HttpMessageHandler innerHandler)
			: base(innerHandler)
		{
			_credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
			_region = region ?? throw new ArgumentNullException(nameof(region));
		}

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var credentials = await _credentials.GetCredentialsAsync().ConfigureAwait(false);

			await AwsSigV4Util.SignRequest(request, credentials, _region, DateTime.UtcNow).ConfigureAwait(false);

			return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
		}
	}
}

#endif
