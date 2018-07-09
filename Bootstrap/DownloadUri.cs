using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using Microsoft.Build.Framework;
using Task = System.Threading.Tasks.Task;

namespace Xamarin.Android.Lite.Bootstrap
{
	public class DownloadUri : Microsoft.Build.Utilities.Task, ICancelableTask
	{
		[Required]
		public string [] SourceUris { get; set; }

		[Required]
		public ITaskItem [] DestinationFiles { get; set; }

		CancellationTokenSource cancellationTokenSource;

		public void Cancel ()
		{
			cancellationTokenSource?.Cancel ();
		}

		public override bool Execute ()
		{
			Log.LogMessage (MessageImportance.Low, "DownloadUri:");
			Log.LogMessage (MessageImportance.Low, "  SourceUris:");
			foreach (var uri in SourceUris) {
				Log.LogMessage (MessageImportance.Low, "    {0}", uri);
			}
			Log.LogMessage (MessageImportance.Low, "  DestinationFiles:");
			foreach (var dest in DestinationFiles) {
				Log.LogMessage (MessageImportance.Low, "    {0}", dest.ItemSpec);
			}

			if (SourceUris.Length != DestinationFiles.Length) {
				Log.LogError ("SourceUris.Length must equal DestinationFiles.Length.");
				return false;
			}

			var source = cancellationTokenSource = new CancellationTokenSource ();
			var tasks = new Task [SourceUris.Length];
			using (var client = new HttpClient ()) {
				client.Timeout = TimeSpan.FromHours (1);
				for (int i = 0; i < SourceUris.Length; ++i) {
					tasks [i] = DownloadFile (client, source, SourceUris [i], DestinationFiles [i].ItemSpec);
				}
				Task.WaitAll (tasks, source.Token);
			}

			return !Log.HasLoggedErrors;
		}

		async Task DownloadFile (HttpClient client, CancellationTokenSource source, string uri, string destinationFile)
		{
			var dp = Path.GetDirectoryName (destinationFile);
			var dn = Path.GetFileName (destinationFile);
			var tempPath = Path.Combine (dp, "." + dn + ".download");
			Directory.CreateDirectory (dp);

			Log.LogMessage (MessageImportance.Normal, $"Downloading `{uri}` to `{tempPath}`.");
			try {
				using (var r = await client.GetAsync (uri, source.Token)) {
					r.EnsureSuccessStatusCode ();
					using (var s = await r.Content.ReadAsStreamAsync ())
					using (var o = File.OpenWrite (tempPath)) {
						await s.CopyToAsync (o, 4096, source.Token);
					}
				}
				Log.LogMessage (MessageImportance.Low, $"mv '{tempPath}' '{destinationFile}'.");
				File.Delete (destinationFile);
				File.Move (tempPath, destinationFile);
			} catch (Exception e) {
				Log.LogError ("Unable to download URL `{0}` to `{1}`: {2}", uri, destinationFile, e.Message);
				Log.LogErrorFromException (e);
			}
		}
	}
}