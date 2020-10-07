using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using FileProvider = AndroidX.Core.Content.FileProvider;

namespace Vapolia.PicturePicker.PlatformLib
{
    /// <summary>
    /// https://github.com/xamarin/Essentials/blob/fe78dad8e2b0d78b7ba3ee5460862e1dbb2ed994/Xamarin.Essentials/Platform/Platform.android.cs
    /// </summary>
    [Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    class IntermediateActivity : Activity
    {
        const string LaunchedExtra = "launched";
        const string ActualIntentExtra = "actual_intent";
        const string GuidExtra = "guid";
        const string RequestCodeExtra = "request_code";
        const string OutputExtra = "output";

        public const string OutputUriExtra = "output_uri";

        static readonly ConcurrentDictionary<string, TaskCompletionSource<Intent?>> PendingTasks = new ConcurrentDictionary<string, TaskCompletionSource<Intent?>>();

        bool launched;
        Intent? actualIntent;
        string? guid;
        int requestCode = -1;
        string? output;
        global::Android.Net.Uri? outputUri;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var extras = savedInstanceState ?? Intent?.Extras;

            // read the values
            if (extras != null)
            {
                launched = extras.GetBoolean(LaunchedExtra, false);
                actualIntent = extras.GetParcelable(ActualIntentExtra) as Intent;
                guid = extras.GetString(GuidExtra);
                requestCode = extras.GetInt(RequestCodeExtra, -1);
                output = extras.GetString(OutputExtra, null);
            }

            if (!string.IsNullOrEmpty(output))
            {
                var javaFile = new Java.IO.File(output!);
                var providerAuthority = Xamarin.Essentials.Platform.AppContext.PackageName + ".fileProvider";
                outputUri = FileProvider.GetUriForFile(Xamarin.Essentials.Platform.AppContext, providerAuthority, javaFile);
                actualIntent?.PutExtra(MediaStore.ExtraOutput, outputUri);
            }

            // if this is the first time, launch the real activity
            if (!launched)
                StartActivityForResult(actualIntent, requestCode);
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            // make sure we mark this activity as launched
            outState.PutBoolean(LaunchedExtra, true);

            // save the values
            outState.PutParcelable(ActualIntentExtra, actualIntent);
            outState.PutString(GuidExtra, guid);
            outState.PutInt(RequestCodeExtra, requestCode);
            outState.PutString(OutputExtra, output);

            base.OnSaveInstanceState(outState);
        }

        protected override void OnActivityResult(int receivedRequestCode, Result resultCode, Intent? intent)
        {
            base.OnActivityResult(receivedRequestCode, resultCode, intent);

            // we have a valid GUID, so handle the task
            if (!string.IsNullOrEmpty(guid) && PendingTasks.TryRemove(guid!, out var tcs) && tcs != null)
            {
                if (resultCode == Result.Canceled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    if (outputUri != null)
                        intent?.PutExtra(OutputUriExtra, outputUri);

                    tcs.TrySetResult(intent);
                }
            }

            // close the intermediate activity
            Finish();
        }

        public static Task<Intent?> StartAsync(Intent intent, int requestCode, string? extraOutputPath = null)
        {
            // make sure we have the activity
            var activity = Xamarin.Essentials.Platform.CurrentActivity;

            var tcs = new TaskCompletionSource<Intent?>();

            // create a new task
            var guid = Guid.NewGuid().ToString();
            PendingTasks[guid] = tcs;

            // create the intermediate intent, and add the real intent to it
            var intermediateIntent = new Intent(activity, typeof(IntermediateActivity));
            intermediateIntent.PutExtra(ActualIntentExtra, intent);
            intermediateIntent.PutExtra(GuidExtra, guid);
            intermediateIntent.PutExtra(RequestCodeExtra, requestCode);

            if (extraOutputPath != null)
                intermediateIntent.PutExtra(OutputExtra, extraOutputPath);

            // start the intermediate activity
            activity.StartActivityForResult(intermediateIntent, requestCode);

            return tcs.Task;
        }
    }
}