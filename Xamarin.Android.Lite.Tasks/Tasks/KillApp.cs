using Microsoft.Build.Framework;

namespace Xamarin.Android.Lite.Tasks
{
	public class KillApp : BroadcastTask
	{
		[Required]
		public string PackageName { get; set; }

		const string IntentAction = "mono.android.intent.action.SEPPUKU";
		const string Category = "mono.android.intent.category.SEPPUKU";
		const string Data = "mono.android.Seppuku";

		protected override string GenerateBroadcastCommand ()
		{
			return $"-a {IntentAction} -c {Category}.{PackageName} -n \"{PackageName}/{Data}\"";
		}

		protected override void OnData (string data)
		{
			//Don't need to do anything
		}
	}
}
