using System;
using System.ComponentModel;

using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using ShoppingCart.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android.AppCompat;

[assembly: ExportRenderer(typeof(NavigationPage), typeof(CustomNavigationRenderer))]
namespace ShoppingCart.Droid
{
    public class CustomNavigationRenderer : NavigationPageRenderer
    {
        public CustomNavigationRenderer(Context context) : base(context)
        {
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            try
            {
                Android.Support.V7.Widget.Toolbar bar = ((Activity)Context).FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);

                if (((NavigationPage)sender).CurrentPage is MainPage)
                    bar.FindViewById<ImageView>(Resource.Id.logoImageView).Visibility = ViewStates.Visible;
                else
                    bar.FindViewById<ImageView>(Resource.Id.logoImageView).Visibility = ViewStates.Gone;
            }
            catch { }
        }
    }
}
