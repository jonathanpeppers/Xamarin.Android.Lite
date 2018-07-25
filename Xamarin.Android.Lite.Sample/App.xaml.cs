using System;
using Xamarin.Forms;
using Xamarin.Android.Lite.Sample.Views;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation (XamlCompilationOptions.Compile)]
namespace Xamarin.Android.Lite.Sample
{
	public partial class App : Application
	{
		public App ()
		{
			InitializeComponent();

			MainPage = new MainPage();
		}
	}
}
