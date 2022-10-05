﻿using System;
using System.IO;
using Java.IO;
using Android.Graphics;
using Android.App;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;
using Android.Widget;
using AndroidX.Core.Graphics.Drawable;
using AndroidX.DrawerLayout.Widget;
using System.Collections.Generic;

namespace IAPYX_INNOVATIONS_RETROFIT_FRIDGE_APP
{
    /// <summary>
    /// First Activity of the app. Handles login and fridge status UI stuff
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class MainActivity : AppCompatActivity
    {
        List<Status> recordedStatus;
        /// <summary>
        /// Acts as "Main" function. Is run the moment this activity is started (app is booted)
        /// </summary>
        /// <param name="savedInstanceState"></param>
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            //If "remember me" button is checked a file is written. If that file exists, it automatically logs in the user
            /*if (UserData.ReadLoginInfo())
            {
                Login();
            }
            //otherwise, it takes the user through the login/registration process.
            else
            {*/
                SetContentView(Resource.Layout.activity_main);
                //Log In Button variable
                Button loginButton = FindViewById<Button>(Resource.Id.LoginButton);
                loginButton.Click += (o, e) =>
                {
                    //When login button is clicked, it gets the username and password components, sets the credentials, checks the remember me box, and then attempts a login with the input credentials.
                    string user = FindViewById<TextView>(Resource.Id.username).Text;
                    string pass = FindViewById<TextView>(Resource.Id.password).Text;
                    UserData.SetCredentials(user, pass);
                    //Checks the state of Remember Me box, if it's checked, it writes a cred file, otherwise, it continues
                    CheckBox rm = FindViewById<CheckBox>(Resource.Id.RememberMe);
                    if (rm.Checked)
                    {
                        UserData.SaveLoginInfo();
                    }
                    Login();
                };
                //Registration Button variable
                Button registrationButton = FindViewById<Button>(Resource.Id.registration);
                registrationButton.Click += (o, e) =>
                {
                    //Opens new Registration Activity
                    StartActivity(typeof(RegistrationActivity));
                };
                Button testButton = FindViewById<Button>(Resource.Id.testButton);
                testButton.Click += (o,e) =>
                {
                    StartActivity(typeof(Test));
                };
            //}
        }

        /// <summary>
        /// Function is called back when a permission request is run. At the moment, it just grants results
        /// </summary>
        /// <param name="requestCode"></param>
        /// <param name="permissions"></param>
        /// <param name="grantResults"></param>
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        /// <summary>
        /// Attempts a login, a server error prints Connection Error, while incorrect login prints "Wrong info"
        /// </summary>
        private async void Login()
        {
            Dialog loading = new Dialog(this);
            loading.SetContentView(Resource.Layout.whole_screen_loading_symbol);
            loading.Show();
            //Check database
            bool canLogin = true;
            await DatabaseManager.GetAuthDBInfo();
            if (!DatabaseManager.isOnline)
            {
                //Throw login connection error
                TextView loginError = FindViewById<TextView>(Resource.Id.loginError);
                loginError.Text = "Connection Error";
                loginError.Visibility = Android.Views.ViewStates.Visible;
                canLogin = false;
            }
            else
                canLogin = await DatabaseManager.CheckLogin();

            //If login is successful, then it fetches the log container information and proceeds past login screen to status screen
            if (canLogin)
            {
                await DatabaseManager.GetLogDBInfo();
                ToContent();
            }
            //If it's unsuccessful and online, it resets all UserData instance values and informs user of incorrect issue.
            else if (DatabaseManager.isOnline)
            {
                UserData.username = "";
                UserData.password = "";
                //Throw login error
                TextView loginError = FindViewById<TextView>(Resource.Id.loginError);
                loginError.Text = "Username or Password is Incorrect";
                loginError.Visibility = Android.Views.ViewStates.Visible;
                
            }
            loading.Dismiss();
            loading.Hide();
        }

        /// <summary>
        /// Sets the XML from login page to status page
        /// </summary>
        private void ToContent()
        {
            SetContentView(Resource.Layout.info_screen);
            Toolbar tb = FindViewById<Toolbar>(Resource.Id.mainToolbar);
            SetActionBar(tb);

            //Sets user's pfp to the ImageButton
            ImageButton pfp = FindViewById<ImageButton>(Resource.Id.pfp);
            if (UserData.pfp != null)
            {
                RoundedBitmapDrawable rbmpd = RoundedBitmapDrawableFactory.Create(Resources, UserData.pfp);
                rbmpd.Circular = true;
                pfp.SetImageDrawable(rbmpd);
            }
            else
            {
                Bitmap bmp = BitmapFactory.DecodeResource(Resources ,Resource.Drawable.blank_profile_picture);
                RoundedBitmapDrawable rbmpd = RoundedBitmapDrawableFactory.Create(Resources, bmp);
                rbmpd.Circular = true;
                pfp.SetImageDrawable(rbmpd);
            }

            //Sets up inventoryButton event to start inventory list activity
            Button invBtn = FindViewById<Button>(Resource.Id.ToListButton);
            invBtn.Click += (o, e) =>
            {
                //When clicked, it starts new Inventory Activity
                StartActivity(new Android.Content.Intent(this, typeof(InventoryActivity)));
            };

            //Sets up Bluetooth button setup button for onboarding process
            Button bluetoothSetup = FindViewById<Button>(Resource.Id.bluetooth);
            bluetoothSetup.Click += (o, e) =>
            {
                StartActivity(typeof(OnboardingActivity));
            };

            //Gets the current status values for the fridge
            FetchStatusFromDB();
        }

        /// <summary>
        /// Gets Status values for Fridge from Database
        /// </summary>
        private async void FetchStatusFromDB()
        {
            StatusDB databaseList;
            //Fetches items from database
            databaseList = await DatabaseManager.ReadStatusFromDB();
            //Sets items into the global variable for use
            recordedStatus = databaseList.loggedStatus;
            if (recordedStatus == null)
                recordedStatus = new List<Status>();

            UpdateView();
        }

        /// <summary>
        /// Makes sure all items are correctly up-to-date when called
        /// </summary>
        public void UpdateView()
        {
            TextView temperature = FindViewById<TextView>(Resource.Id.temperature);
            TextView doorStatus = FindViewById<TextView>(Resource.Id.doorStatus);
            ImageView shelf = FindViewById<ImageView>(Resource.Id.shelfPic);
            int? temp=null;
            bool door=false;
            string pic=null;

            //Sifts through recordedStatus and assigns the values needed for all Views
            for (int i = 0; i < recordedStatus.Count;i++)
            {
                //Each Status should have name and value. This checks and assigns the correct values by names
                Status analyzingLog = recordedStatus[i];
                switch (analyzingLog.dataName) {
                    //
                    case "Temperature":
                        temp = Convert.ToInt32(analyzingLog.value);
                        break;
                    case "Door Open Status":
                        door = Convert.ToBoolean(analyzingLog.value);
                        break;
                    case "Picture":
                        pic = Convert.ToString(analyzingLog.value);
                        break;
                }
            }

            //Formats the Views to a more readable format with the new values
            temperature.Text = "Temperature: "+temp+" F";
            if (door)
                doorStatus.Text = "Door is Open";
            else
                doorStatus.Text = "Door is Closed";

            //Converts pic string to Bitmap for ImageView
            if (pic != null) {
                byte[] bytes = Convert.FromBase64String(pic);
                Bitmap bmp = BitmapFactory.DecodeByteArray(bytes,0,bytes.Length);
                ImageView iv = FindViewById<ImageView>(Resource.Id.shelfPic);
                iv.SetImageBitmap(bmp);
            }
        }
    }
}