using Microsoft.Build.Framework;

namespace Xamarin.Android.Lite.Tasks
{
	public class DetectPackages : BroadcastTask
	{
		[Required]
		public string ApiLevel { get; set; }

		[Required]
		public string PackageName { get; set; }

		[Output]
		public string RuntimeVersion { get; set; }

		[Output]
		public string PlatformRuntimeVersion { get; set; }

		[Output]
		public string PackageVersion { get; set; }

		const string IntentAction = "mono.android.intent.action.PACKAGE_VERSIONS";
		const string Data = "com.xamarin.mono.android.PackageVersions";

		protected override string GenerateBroadcastCommand ()
		{
			return $"-a {IntentAction} -e packages \"{Utils.DebugRuntime},{Utils.PlatformRuntime}_{ApiLevel},{PackageName}\" -n \"{Utils.DebugRuntime}/{Data}\"";
		}

		protected override void OnData (string data)
		{
			// Broadcasting: Intent { act=mono.android.intent.action.PACKAGE_VERSIONS flg=0x400000 cmp=Mono.Android.DebugRuntime/com.xamarin.mono.android.PackageVersions (has extras) }
			// Broadcast completed: result=0, data="Mono.Android.DebugRuntime=1529065494,Mono.Android.Platform.ApiLevel_27=1531422460,com.xamarin.android.lite.sample=1"
			foreach (var pair in data.Split (',')) {
				var split = pair.Split ('=');
				if (split.Length == 2) {
					var key = split [0];
					var value = split [1];
					if (key == Utils.DebugRuntime) {
						RuntimeVersion = value;
					} else if (key == Utils.PlatformRuntime + "_" + ApiLevel) {
						PlatformRuntimeVersion = value;
					} else if (key == PackageName) {
						PackageVersion = value;
					}
				}
			}
		}
	}
}
