#region Copyright & License Information
/*
 * Copyright 2019-2021 The OpenSA Developers (see CREDITS)
 * This file is part of OpenSA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.ComponentModel;
using System.Net;
using OpenRA;

namespace OpenSA
{
	public class DownloadWithAgent
	{
		readonly object syncObject = new object();
		WebClient wc;

		public static string FormatErrorMessage(Exception e)
		{
			var ex = e as WebException;
			if (ex == null)
				return e.Message;

			switch (ex.Status)
			{
				case WebExceptionStatus.RequestCanceled:
					return "Cancelled";
				case WebExceptionStatus.NameResolutionFailure:
					return "DNS lookup failed";
				case WebExceptionStatus.Timeout:
					return "Connection timeout";
				case WebExceptionStatus.ConnectFailure:
					return "Cannot connect to remote server";
				case WebExceptionStatus.ProtocolError:
					return "File not found on remote server";
				default:
					return ex.Message;
			}
		}

		void EnableTLS12OnWindows()
		{
			// Enable TLS 1.2 on Windows: .NET 4.7 on Windows 10 only supports obsolete protocols by default
			// SecurityProtocolType.Tls12 is not defined in the .NET 4.5 reference dlls used by mono,
			// so we must use the enum's constant value directly
			if (Platform.CurrentPlatform == PlatformType.Windows)
				ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
		}

		public DownloadWithAgent(string url, string path, Action<DownloadProgressChangedEventArgs> onProgress, Action<AsyncCompletedEventArgs> onComplete)
		{
			EnableTLS12OnWindows();

			lock (syncObject)
			{
				var metadata = Game.ModData.Manifest.Metadata;
				wc = new WebClient { Proxy = null };
				wc.Headers.Add("user-agent", "{0} {1}".F(metadata.Title, metadata.Version));
				wc.DownloadProgressChanged += (_, a) => onProgress(a);
				wc.DownloadFileCompleted += (_, a) => { DisposeWebClient(); onComplete(a); };
				wc.DownloadFileAsync(new Uri(url), path);
			}
		}

		public DownloadWithAgent(string url, Action<DownloadProgressChangedEventArgs> onProgress, Action<DownloadDataCompletedEventArgs> onComplete)
		{
			EnableTLS12OnWindows();

			lock (syncObject)
			{
				var metadata = Game.ModData.Manifest.Metadata;
				wc = new WebClient { Proxy = null };
				wc.Headers.Add("user-agent", "{0} {1}".F(metadata.Title, metadata.Version));
				wc.DownloadProgressChanged += (_, a) => onProgress(a);
				wc.DownloadDataCompleted += (_, a) => { DisposeWebClient(); onComplete(a); };
				wc.DownloadDataAsync(new Uri(url));
			}
		}

		void DisposeWebClient()
		{
			lock (syncObject)
			{
				wc.Dispose();
				wc = null;
			}
		}

		public void CancelAsync()
		{
			lock (syncObject)
				wc?.CancelAsync();
		}
	}
}
