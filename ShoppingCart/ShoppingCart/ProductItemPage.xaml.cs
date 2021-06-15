using ShoppingCart.Model;
using Newtonsoft.Json;
using Rg.Plugins.Popup.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.IO;
using Xamarin.Essentials;

namespace ShoppingCart
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProductItemPage : ContentPage
    {
        private bool appearingFirstTime = true;

        private ObservableCollection<MainListItem> allList;
        private int productsIndex;
        private int scanningItemID = -1;

        private bool isScanning;

        public ProductItemPage(ObservableCollection<MainListItem> _allList, int _index)
        {
            InitializeComponent();

            allList = _allList;
            productsIndex = _index;

            lstProductsList.ItemsSource = allList[productsIndex].ProductsList ?? new ObservableCollection<ProductListItem>();

            this.Title = allList[productsIndex].DisplayName;
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            if (appearingFirstTime)
            {
                appearingFirstTime = false;

                // Get cmbSDK version number
                scannerControl.SdkVersion();

                CreateScannerDevice();
            }
            else
            {
                //----------------------------------------------------------------------------
                // When an page is disappeared, the connection to the scanning device needs
                // to be closed; thus when we are resumed (Appear this page again) we
                // have to restore the connection (assuming we had one).
                // This is used for android only.
                // For iOS we use Observer that is created in ScannerControl class in iOS platform specific project
                //----------------------------------------------------------------------------
                if (Device.RuntimePlatform == Device.Android && await Permissions.CheckStatusAsync<Permissions.Camera>() == PermissionStatus.Granted)
                {
                    scannerControl.Connect();
                }
            }
        }

        protected override void OnDisappearing()
        {
            // This is used for android only.
            // For iOS we use Observer that is created in ScannerControl class in iOS platform specific project
            // If we have connection to a scanner, disconnect
            scannerControl.Disconnect();

            base.OnDisappearing();
        }

        // Create a scanner device
        private void CreateScannerDevice()
        {
            //***************************************************************************************
            // Create a camera scanner
            //
            // NOTE: SDK requires a license key. Refer to
            //       the SDK's documentation on obtaining a license key as well as the methods for
            //       passing the key to the SDK (in this example, we're relying on an entry in
            //       plist.info and androidmanifest.xml--also sdk key can be passed
            //       as a parameter in this (GetPhoneCameraDevice) constructor).
            //***************************************************************************************
            scannerControl.GetPhoneCameraDevice(ScannerCameraMode.NoAimer, ScannerPreviewOption.Defaults, false);

            // Connect to device
            scannerControl.Connect();
        }

        //----------------------------------------------------------------------------
        // This is an example of configuring the device. In this sample application, we
        // configure the device every time the connection state changes to connected (see
        // the OnConnectionStateChanged delegate below), as this is the best
        // way to garentee it is setup the way we want it.
        //
        // These are just example settings; in your own application you will want to
        // consider which setting changes are optimal for your application. It is
        // important to note that the different supported devices have different, out
        // of the box defaults:
        //
        //    * camera scanner has NO symbologies enabled by default
        //
        // For the best scanning performance, it is recommended to only have the barcode
        // symbologies enabled that your application actually needs to scan.
        //----------------------------------------------------------------------------
        private void ConfigureScannerDevice()
        {
            //----------------------------------------------
            // Explicitly enable the symbologies we need
            //----------------------------------------------
            scannerControl.SetSymbologyEnabled(Symbology.Datamatrix, true);
            scannerControl.SetSymbologyEnabled(Symbology.C128, true);
            scannerControl.SetSymbologyEnabled(Symbology.UpcEan, true);


            ////-------------------------------------------------------
            //// Explicitly disable symbologies we know we don't need
            ////-------------------------------------------------------
            scannerControl.SetSymbologyEnabled(Symbology.Codabar, false);
            scannerControl.SetSymbologyEnabled(Symbology.C93, false);


            ////---------------------------------------------------------------------------
            //// We are going to explicitly turn on image results
            ////---------------------------------------------------------------------------
            scannerControl.EnableImage(true);
            scannerControl.EnableImageGraphics(true);
        }

        #region ScannerDevice listener implementations

        // The connect method has completed, here you can see whether there was an error with establishing the connection or not
        // (args: ScannerExceptions exception, string errorMessage)
        public void OnConnectionCompleted(object sender, object[] args)
        {
            // If we have valid connection error param will be null,
            // otherwise here is error that inform us about issue that we have while connecting to scanner
            if ((ScannerExceptions)args[0] != ScannerExceptions.NoException)
            {
                // ask for Camera Permission if necessary (android only, for iOS we handle permission from SDK)
                if ((ScannerExceptions)args[0] == ScannerExceptions.CameraPermissionException)
                    RequestCameraPermission();
                else
                {
                    UpdateUIByConnectionState(ScannerConnectionStatus.Disconnected);
                }
            }
        }

        // Update the UI of the app (scan button, connection state label) depending on the current connection state
        private void UpdateUIByConnectionState(ScannerConnectionStatus connectionStatus)
        {
            lblStatus.Text = " " + connectionStatus.ToString() + " ";

            if (connectionStatus == ScannerConnectionStatus.Connected)
                lblStatus.BackgroundColor = Color.FromHex("#669900");
            else
                lblStatus.BackgroundColor = Color.FromHex("#ff4444");
        }

        private async void RequestCameraPermission()
        {
            var result = await Permissions.RequestAsync<Permissions.Camera>();

            // Check result from permission request. If it is allowed by the user, connect to scanner
            if (result == PermissionStatus.Granted)
            {
                scannerControl.Connect();
            }
            else
            {
                if (Permissions.ShouldShowRationale<Permissions.Camera>())
                {
                    if (await DisplayAlert(null, "You need to allow access to the Camera", "OK", "Cancel"))
                        RequestCameraPermission();
                }
            }
        }

        // This is called when a connection with the scanner has been changed.
        // The scanner is usable only in the "Connected" state
        public void OnConnectionStateChanged(object sender, ScannerConnectionStatus status)
        {
            if (status == ScannerConnectionStatus.Connected)
            {
                // We just connected, so now configure the device how we want it
                ConfigureScannerDevice();
            }

            isScanning = false;
            UpdateUIByConnectionState(status);
        }

        // This is called after scanning has completed, either by detecting a barcode, canceling the scan by using the on-screen button or if the scanning timed-out
        public async void OnReadResultReceived(object sender, List<ScannedResult> results)
        {
            isScanning = false;
            resultImage.Source = null;

            if (results != null && results.Count > 0 && results[0].IsGoodRead)
            {
                if (scanningItemID > -1)
                {
                    int selectedIndex = -1;
                    try { selectedIndex = allList[productsIndex].ProductsList.IndexOf(allList[productsIndex].ProductsList.First<ProductListItem>(x => x.ID == scanningItemID)); } catch { }

                    if (selectedIndex >= 0)
                        allList[productsIndex].ProductsList[selectedIndex] = new ProductListItem(allList[productsIndex].ProductsList[selectedIndex].ID, allList[productsIndex].ProductsList[selectedIndex].DisplayName, results[0].ResultCode, results[0].ResultImage);
                }
                else
                {
                    int allProductsSourceMaxID = 0;
                    if (allList[productsIndex].ProductsList.Count > 0)
                        allProductsSourceMaxID = allList[productsIndex].ProductsList.Max(x => x.ID);

                    allList[productsIndex].ProductsList.Add(new ProductListItem(allProductsSourceMaxID + 1, "", results[0].ResultCode, results[0].ResultImage));
                }

                if (results[0].ResultImage != null)
                    resultImage.Source = ImageSource.FromStream(() => new MemoryStream(results[0].ResultImage));

                Application.Current.Properties[App.Current.Resources["AllListsSource"].ToString()] = JsonConvert.SerializeObject(allList);
                await Application.Current.SavePropertiesAsync();
            }

            scanningItemID = -1;
        }

        // SDK version received
        public void SdkVersionReceived(object sender, string version)
        {
            lblSdkVersion.Text = version;
        }

        #endregion

        private void ToggleScanner(int itemID)
        {
            scanningItemID = itemID;

            if (isScanning)
                scannerControl.StopScanning();
            else
                scannerControl.StartScanning();

            isScanning = !isScanning;
        }

        private void LstProductsList_Tapped(object sender, EventArgs e)
        {
            resultImage.Source = ImageSource.FromStream(() => new MemoryStream((byte[])((TappedEventArgs)e).Parameter));
        }

        private async void Edit_Tapped(object sender, EventArgs e)
        {
            int selectedItemIndex = -1;
            try { selectedItemIndex = allList[productsIndex].ProductsList.IndexOf(allList[productsIndex].ProductsList.First<ProductListItem>(x => x.ID == (int)((TappedEventArgs)e).Parameter)); } catch { }

            if (selectedItemIndex > -1)
            {
                IsEnabled = false;
                await Navigation.PushPopupAsync(new EditModalPage(allList, productsIndex, selectedItemIndex), true);
                IsEnabled = true;
            }
        }

        private async void Delete_Tapped(object sender, EventArgs e)
        {
            ProductListItem selectedItem = allList[productsIndex].ProductsList.First<ProductListItem>(x => x.ID == (int)((TappedEventArgs)e).Parameter);

            if (selectedItem != null && selectedItem.ID > 0 && await DisplayAlert("Delete", "Are you sure you want to delete " + selectedItem.DisplayName, "OK", "CANCEL"))
            {
                allList[productsIndex].ProductsList.Remove(selectedItem);
                resultImage.Source = null;

                Application.Current.Properties[App.Current.Resources["AllListsSource"].ToString()] = JsonConvert.SerializeObject(allList);
                await Application.Current.SavePropertiesAsync();
            }
        }

        private void Scan_Tapped(object sender, EventArgs e)
        {
            ToggleScanner((int)((TappedEventArgs)e).Parameter);
        }

        private async void TxtNewProduct_Completed(object sender, EventArgs e)
        {
            if (((Entry)sender).Text != null && ((Entry)sender).Text.Trim().Length > 0)
            {
                int allProductsSourceMaxID = 0;
                if (allList[productsIndex].ProductsList.Count > 0)
                    allProductsSourceMaxID = allList[productsIndex].ProductsList.Max(x => x.ID);

                allList[productsIndex].ProductsList.Add(new ProductListItem(allProductsSourceMaxID + 1, ((Entry)sender).Text, "", null));

                ((Entry)sender).Text = "";

                Application.Current.Properties[App.Current.Resources["AllListsSource"].ToString()] = JsonConvert.SerializeObject(allList);
                await Application.Current.SavePropertiesAsync();
            }
        }

        private void AddNew_Tapped(object sender, EventArgs e)
        {
            ToggleScanner(-1);
        }
    }
}
