using System;
using Xamarin.Android.Lite.Sample.Views;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

#if DEBUG
[assembly: XamlCompilation (XamlCompilationOptions.Skip)]
#else
[assembly: XamlCompilation (XamlCompilationOptions.Compile)]
#endif

namespace Xamarin.Android.Lite.Sample
{
	public partial class App : Application
	{
		public App ()
		{
			InitializeComponent();

			Connectivity.ConnectivityChanged += e => {
				Console.WriteLine ("ConnectivityChanged, NetworkAccess: {0}", e.NetworkAccess);
			};

			MainPage = new MainPage();
		}
	}
}
