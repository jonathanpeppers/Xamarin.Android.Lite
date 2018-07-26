using Microsoft.Build.Framework;

namespace Xamarin.Android.Lite.Tasks
{
	public class GetExternalStorageDirectory : BroadcastTask
	{
		[Required]
		public string PackageName { get; set; }

		[Output]
		public string[] Directories { get; set; }

		[Output]
		public string ExternalStorageDirectory { get; set; }

		const string IntentAction = "mono.android.intent.action.EXTERNAL_STORAGE_DIRECTORY";
		const string Data = "com.xamarin.mono.android.ExternalStorageDirectory";

		public override bool Execute ()
		{
			bool result = base.Execute ();
			if (result && string.IsNullOrEmpty (ExternalStorageDirectory)) {
				Log.LogError ($"Unable to determine {nameof (ExternalStorageDirectory)}!");
				return false;
			}
			return result;
		}

		protected override string GenerateBroadcastCommand ()
		{
			return $"-a {IntentAction} -n \"{Utils.DebugRuntime}/{Data}\"";
		}

		protected override void OnData (string data)
		{
			string external = data.Trim ();
			Directories = new [] {
				$"{external}/Android/data/{PackageName}",
				$"{external}/Android/data/{PackageName}/files",
				ExternalStorageDirectory = $"{external}/Android/data/{PackageName}/files/.__override__"
			};
		}
	}
}
