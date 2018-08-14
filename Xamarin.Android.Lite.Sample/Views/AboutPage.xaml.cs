using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Xamarin.Android.Lite.Sample.Views
{
	public partial class AboutPage : ContentPage
	{
		public AboutPage ()
		{
			InitializeComponent ();

			var assembly = GetType ().Assembly.GetName ().Name;
			xamarin_logo.Source = ImageSource.FromResource (assembly + ".xamarin_logo.png", typeof (App));
		}
	}
}