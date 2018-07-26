using Xamarin.Forms;
using Xamarin.Android.Lite.Sample.Views;
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

			MainPage = new MainPage();
		}
	}
}
