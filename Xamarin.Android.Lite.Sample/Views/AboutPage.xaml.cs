using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Xamarin.Android.Lite.Sample.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class AboutPage : ContentPage
	{
		public AboutPage ()
		{
			InitializeComponent ();

			xamarin_logo.Source = ImageSource.FromResource ("Xamarin.Android.Lite.Sample.xamarin_logo.png", typeof (App));
		}
	}
}