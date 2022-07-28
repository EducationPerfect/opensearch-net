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

namespace OpenSearch.Net.Auth.AwsSigV4
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Web;
	using Amazon.Runtime;
	using Amazon.Runtime.Internal.Auth;
	using Amazon.Util;

#if DOTNETCORE
	using System.Net.Http;
	using System.Net.Http.Headers;
	using System.Threading.Tasks;

#else
	using System.Collections.Specialized;
	using System.IO;
	using System.Net;
#endif

	public class CanonicalRequest
	{
		private static readonly IComparer<KeyValuePair<string, string>> StringPairComparer = new KeyValuePairComparer<string, string>();

		private readonly string _method;
		private readonly string _path;
		private readonly string _params;
		private readonly SortedDictionary<string, List<string>> _headers;
		private readonly string _contentSha256;

		public string XAmzDate { get; }

		public string XAmzSecurityToken { get; }

		public string SignedHeaders { get; }

		private CanonicalRequest(string method, string path, string queryParams, SortedDictionary<string, List<string>> headers,
			string contentSha256, string xAmzDate, string xAmzSecurityToken
		)
		{
			_method = method;
			_path = path;
			_params = queryParams;
			_headers = headers;
			_contentSha256 = contentSha256;
			SignedHeaders = string.Join(";", _headers.Keys);
			XAmzDate = xAmzDate;
			XAmzSecurityToken = xAmzSecurityToken;
		}

#if DOTNETCORE
		public static async Task<CanonicalRequest> From(HttpRequestMessage request, ImmutableCredentials credentials, DateTime signingTime)
#else
		public static CanonicalRequest From(HttpWebRequest request, RequestData requestData, ImmutableCredentials credentials, DateTime signingTime)
#endif
		{
			var path = AWSSDKUtils.CanonicalizeResourcePath(request.RequestUri, null, false);

#if DOTNETCORE
			var bodyBytes = await GetBodyBytes(request).ConfigureAwait(false);
#else
			var bodyBytes = GetBodyBytes(requestData);
#endif

			var contentSha256 = AWSSDKUtils.ToHex(AWS4Signer.ComputeHash(bodyBytes), true);

			var xAmzDate = AWS4Signer.FormatDateTime(signingTime, "yyyyMMddTHHmmssZ");

			var canonicalHeaders = new SortedDictionary<string, List<string>>();

			CanonicalizeHeaders(canonicalHeaders, request.Headers);
#if DOTNETCORE
			CanonicalizeHeaders(canonicalHeaders, request.Content?.Headers);
#endif

			canonicalHeaders["host"] = new List<string> { request.RequestUri.Authority };
			canonicalHeaders["x-amz-date"] = new List<string> { xAmzDate };

			string xAmzSecurityToken = null;
			if (credentials.UseToken)
			{
				xAmzSecurityToken = credentials.Token;
				canonicalHeaders["x-amz-security-token"] = new List<string> { xAmzSecurityToken };
			}

			var queryParams = HttpUtility.ParseQueryString(request.RequestUri.Query);

			var orderedParams = queryParams
				.AllKeys
				.SelectMany(k => queryParams.GetValues(k)
						?.Select(v => !string.IsNullOrEmpty(k)
							? new KeyValuePair<string, string>(k, v)
							: new KeyValuePair<string, string>(v, string.Empty))
					?? Enumerable.Empty<KeyValuePair<string, string>>())
				.OrderBy(pair => pair, StringPairComparer)
				.Select(pair => $"{AWSSDKUtils.UrlEncode(pair.Key, false)}={AWSSDKUtils.UrlEncode(pair.Value, false)}");

			var paramString = string.Join("&", orderedParams);

#if DOTNETCORE
			var method = request.Method.ToString();
#else
			var method = request.Method;
#endif

			return new CanonicalRequest(method, path, paramString, canonicalHeaders, contentSha256, xAmzDate, xAmzSecurityToken);
		}

#if DOTNETCORE
		private static async Task<byte[]> GetBodyBytes(HttpRequestMessage request)
		{
			if (request.Content == null) return Array.Empty<byte>();

			var body = await request.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

			if (request.Content is ByteArrayContent) return body;

			var content = new ByteArrayContent(body);
			foreach (var pair in request.Content.Headers)
			{
				if (string.Equals(pair.Key, "Content-Length", StringComparison.OrdinalIgnoreCase)) continue;

				content.Headers.TryAddWithoutValidation(pair.Key, pair.Value);
			}
			request.Content = content;

			return body;
		}

#else
		private static byte[] GetBodyBytes(RequestData requestData)
		{
			if (requestData.PostData == null) return Array.Empty<byte>();
			if (requestData.PostData.WrittenBytes is { } data) return data;

			using var ms = new MemoryStream();
			requestData.PostData.Write(ms, requestData.ConnectionSettings);
			return ms.ToArray();
		}
#endif

		private static void CanonicalizeHeaders(
			IDictionary<string, List<string>> canonicalHeaders,
#if DOTNETCORE
			HttpHeaders headers
#else
			NameValueCollection headers
#endif
		)
		{
			if (headers == null) return;

#if DOTNETCORE
			foreach (var pair in headers)
#else
			foreach (var pair in headers.AllKeys.Select(k => new KeyValuePair<string, IEnumerable<string>>(k, headers.GetValues(k))))
#endif
			{
				if (pair.Value == null) continue;

				var key = pair.Key.ToLowerInvariant();

				if (key == "user-agent") continue;

				if (!canonicalHeaders.TryGetValue(key, out var dictValues))
					dictValues = canonicalHeaders[key] = new List<string>();

				dictValues.AddRange(pair.Value.Select(v => AWSSDKUtils.CompressSpaces(v).Trim()));
			}
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.Append($"{_method}\n");
			sb.Append($"{_path}\n");
			sb.Append($"{_params}\n");
			foreach (var header in _headers) sb.Append($"{header.Key}:{string.Join(",", header.Value)}\n");
			sb.Append('\n');
			sb.Append($"{SignedHeaders}\n");
			sb.Append(_contentSha256);

			return sb.ToString();
		}

		private class KeyValuePairComparer<TKey, TValue> : IComparer<KeyValuePair<TKey, TValue>>
			where TKey : IComparable<TKey>
			where TValue : IComparable<TValue>
		{
			public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
			{
				var keyComparison = x.Key.CompareTo(y.Key);
				return keyComparison != 0 ? keyComparison : x.Value.CompareTo(y.Value);
			}
		}
	}
}
