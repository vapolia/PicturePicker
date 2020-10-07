using Android.App;
using Android.Content.PM;
using Android.OS;
using MvvmCross.Platforms.Android.Core;
using MvvmCross.Platforms.Android.Views;
using Xamarin.Essentials;

namespace PicturePickerTests.Droid
{
    [Activity(
        Label = "@string/app_name"
        , Theme = "@style/AppTheme.NoActionBar"
        , MainLauncher = true
        , NoHistory = true
        , ScreenOrientation = ScreenOrientation.Portrait)]
	public class SplashScreen : MvxSplashScreenActivity<MvxAndroidSetup<App>, App>
	{
	    public SplashScreen() : base(Resource.Layout.splashscreen)
	    {
	    }

	    protected override void OnCreate(Bundle bundle)
	    {
		    base.OnCreate(bundle);
		    Platform.Init(this, bundle);
	    }
	}
}
