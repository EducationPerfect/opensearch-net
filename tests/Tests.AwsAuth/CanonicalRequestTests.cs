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

namespace Tests.AwsAuth
{
	using System;
	using System.Linq;
	using System.Net.Http;
	using System.Threading.Tasks;
	using System.Web;
	using Amazon.Runtime;
	using FluentAssertions;
	using OpenSearch.Net.AwsAuth;
	using OpenSearch.OpenSearch.Xunit.XunitPlumbing;

	public class CanonicalRequestTests
	{
		private static readonly ImmutableCredentials TestCredentials = new("test-access-key", "test-secret-key", null);
		private static readonly DateTime TestSigningTime = new(2021, 05, 11, 15, 40, 45, DateTimeKind.Utc);

		[U] public async Task TestDoubleEncodePath()
		{
			var request = new HttpRequestMessage(HttpMethod.Post,
				"https://tj9n5r0m12.execute-api.us-east-1.amazonaws.com/test/@connections/JBDvjfGEIAMCERw%3D");

			await TestCanonicalRequest(request, @"POST
/test/%40connections/JBDvjfGEIAMCERw%253D

host:tj9n5r0m12.execute-api.us-east-1.amazonaws.com
x-amz-date:20210511T154045Z

host;x-amz-date
e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
		}

		[U] public async Task TestDoubleUrlEncode()
		{
			var request = new HttpRequestMessage(HttpMethod.Post,
				"https://lambda.us-east-2.amazonaws.com/2015-03-31/functions/arn%3Aaws%3Alambda%3Aus-west-2%3A892717189312%3Afunction%3Amy-rusty-fun/invocations");

			await TestCanonicalRequest(request, @"POST
/2015-03-31/functions/arn%253Aaws%253Alambda%253Aus-west-2%253A892717189312%253Afunction%253Amy-rusty-fun/invocations

host:lambda.us-east-2.amazonaws.com
x-amz-date:20210511T154045Z

host;x-amz-date
e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
		}

		[U] public async Task TestTildeInUri()
		{
			var request = new HttpRequestMessage(HttpMethod.Get,
				"https://s3.us-east-1.amazonaws.com/my-bucket?list-type=2&prefix=~objprefix&single&k=&unreserved=-_.~");

			await TestCanonicalRequest(request, @"GET
/my-bucket
k=&list-type=2&prefix=~objprefix&single=&unreserved=-_.~
host:s3.us-east-1.amazonaws.com
x-amz-date:20210511T154045Z

host;x-amz-date
e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
		}

		[U] public async Task TestQueryParamMultipleValues()
		{
			var request = new HttpRequestMessage(HttpMethod.Get,
				"https://s3.us-east-1.amazonaws.com/my-bucket?list-type=2&list-type=1");

			await TestCanonicalRequest(request, @"GET
/my-bucket
list-type=1&list-type=2
host:s3.us-east-1.amazonaws.com
x-amz-date:20210511T154045Z

host;x-amz-date
e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
		}

		[U] public static async Task TestAllPrintableAsciiQueryParam()
		{
			var printableAscii = string.Concat(Enumerable.Range(32, 95).Select(c => (char)c));
			var queryParams = HttpUtility.ParseQueryString(string.Empty);
			queryParams["list-type"] = "2";
			queryParams["prefix"] = printableAscii;

			var uri = new UriBuilder
				{
					Scheme = "https",
					Host = "s3.us-east-1.amazonaws.com",
					Path = "/my-bucket",
					Query = queryParams.ToString() ?? string.Empty
				}
				.Uri;

			var request = new HttpRequestMessage(HttpMethod.Get, uri);

			await TestCanonicalRequest(request, @"GET
/my-bucket
list-type=2&prefix=%20%21%22%23%24%25%26%27%28%29%2A%2B%2C-.%2F0123456789%3A%3B%3C%3D%3E%3F%40ABCDEFGHIJKLMNOPQRSTUVWXYZ%5B%5C%5D%5E_%60abcdefghijklmnopqrstuvwxyz%7B%7C%7D~
host:s3.us-east-1.amazonaws.com
x-amz-date:20210511T154045Z

host;x-amz-date
e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
		}

		private static async Task TestCanonicalRequest(HttpRequestMessage request, string expected)
		{
			var canonicalRequest = await CanonicalRequest.From(request, TestCredentials, TestSigningTime);

			canonicalRequest
				.ToString()
				.Should()
				.Be(expected.Replace("\r\n", "\n"));
		}
	}
}
