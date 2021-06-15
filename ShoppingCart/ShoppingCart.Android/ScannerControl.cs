using System;
using System.Collections.Generic;

using Android.Content;
using Android.Graphics;
using Android.Widget;
using Com.Caverock.Androidsvg;
using Com.Cognex.Dataman.Sdk;
using Com.Cognex.Dataman.Sdk.Exceptions;
using Com.Cognex.Mobile.Barcode.Sdk;
using Java.Lang;
using Xamarin.Forms.Platform.Android;
using static Com.Cognex.Mobile.Barcode.Sdk.ReaderDevice;

[assembly: Xamarin.Forms.ExportRenderer(typeof(ShoppingCart.ScannerControl), typeof(ShoppingCart.Droid.ScannerControl))]
namespace ShoppingCart.Droid
{
    public class ScannerControl : ViewRenderer<ShoppingCart.ScannerControl, RelativeLayout>, IOnConnectionCompletedListener,
        IReaderDeviceListener, IOnSymbologyListener
    {
        private RelativeLayout rlMainContainer;

        private ReaderDevice readerDevice;
        private bool availabilityListenerStarted = false;

        private static Bitmap svgBitmap;

        public ScannerControl(Context context) : base(context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<ShoppingCart.ScannerControl> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null || Element == null)
            {
                return;
            }

            rlMainContainer = new RelativeLayout(Context)
            {
                LayoutParameters = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.MatchParent, RelativeLayout.LayoutParams.MatchParent)
            };

            if (Control == null)
                SetNativeControl(rlMainContainer);

            //Implement element event handlers. We will invoke them from portable project
            Element.OnDisconnect += (object sender, EventArgs args) =>
            {
                // stop listening to device availability to avoid resource leaks
                if (readerDevice != null)
                {
                    if (availabilityListenerStarted)
                    {
                        readerDevice.StopAvailabilityListening();
                        availabilityListenerStarted = false;
                    }

                    readerDevice.Disconnect();
                }
            };
            Element.OnConnect += (object sender, EventArgs args) =>
            {
                if (readerDevice != null
                    && readerDevice.ConnectionState != ConnectionState.Connecting
                    && readerDevice.ConnectionState != ConnectionState.Connected)
                {

                    //Listen when a MX device has became available/unavailable
                    if (readerDevice.DeviceClass == DataManDeviceClass.Mx && !availabilityListenerStarted)
                    {
                        readerDevice.StartAvailabilityListening();
                        availabilityListenerStarted = true;
                    }

                    readerDevice.Connect(this);
                }
            };
            Element.OnStopScanning += (object sender, EventArgs args) =>
            {
                if (readerDevice != null && readerDevice.ConnectionState == ConnectionState.Connected)
                {
                    readerDevice.StopScanning();
                }
            };
            Element.OnStartScanning += (object sender, EventArgs args) =>
            {
                if (readerDevice != null && readerDevice.ConnectionState == ConnectionState.Connected)
                    readerDevice.StartScanning();
            };
            Element.OnGetPhoneCameraDevice += (object sender, object[] args) =>
            {
                if (readerDevice != null)
                {
                    readerDevice.SetReaderDeviceListener(null);

                    if (availabilityListenerStarted)
                    {
                        readerDevice.StopAvailabilityListening();

                        availabilityListenerStarted = false;
                    }

                    readerDevice.Disconnect();
                    readerDevice.Dispose();
                    readerDevice = null;
                }

                if (args.Length > 3)
                    readerDevice = GetPhoneCameraDevice(Context, (int)args[0], (int)args[1], ((bool)args[2]) ? null : Control, args[3].ToString());
                else
                    readerDevice = GetPhoneCameraDevice(Context, (int)args[0], (int)args[1], ((bool)args[2]) ? null : Control);

                // set listeners and connect to device
                readerDevice.SetReaderDeviceListener(this);
            };
            Element.OnGetMXDevice += (object sender, EventArgs args) =>
            {
                if (readerDevice != null)
                {
                    readerDevice.SetReaderDeviceListener(null);

                    if (availabilityListenerStarted)
                    {
                        readerDevice.StopAvailabilityListening();

                        availabilityListenerStarted = false;
                    }

                    readerDevice.Disconnect();
                    readerDevice.Dispose();
                    readerDevice = null;
                }

                readerDevice = GetMXDevice(Context);

                //Listen when a MX device has became available/unavailable
                if (!availabilityListenerStarted)
                {
                    readerDevice.StartAvailabilityListening();
                    availabilityListenerStarted = true;
                }

                // set listeners and connect to device
                readerDevice.SetReaderDeviceListener(this);
            };
            Element.OnSdkVersion += (object sender, EventArgs args) =>
            {
                if (Element != null)
                    Element.OnGetSdkVersion(DataManSystem.Version);
            };
            Element.OnSetSymbologyEnabled += (object sender, object[] args) =>
            {
                if (readerDevice != null && readerDevice.ConnectionState == ConnectionState.Connected)
                    readerDevice.SetSymbologyEnabled(PortableToNativeSymbology((Symbology)args[0]), (bool)args[1], this);
            };
            Element.OnEnableImage += (object sender, bool enable) =>
            {
                if (readerDevice != null && readerDevice.ConnectionState == ConnectionState.Connected)
                    readerDevice.EnableImage(enable);
            };
            Element.OnEnableImageGraphics += (object sender, bool enable) =>
            {
                if (readerDevice != null && readerDevice.ConnectionState == ConnectionState.Connected)
                    readerDevice.EnableImageGraphics(enable);
            };
            Element.OnSendCommand += (object sender, string dmcc) =>
            {
                if (readerDevice != null && readerDevice.ConnectionState == ConnectionState.Connected)
                    readerDevice.DataManSystem.SendCommand(dmcc, new ResponseListener(Element, dmcc));
            };
            Element.OnSetParser += (object sender, ScannerParser parserType) =>
            {
                if (readerDevice != null && readerDevice.ConnectionState == ConnectionState.Connected)
                    readerDevice.Parser = PortableToNativeParser(parserType);
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (readerDevice != null)
            {
                readerDevice.SetReaderDeviceListener(null);
                readerDevice.StopAvailabilityListening();
                availabilityListenerStarted = false;
                readerDevice.Disconnect();
                readerDevice.Dispose();
                readerDevice = null;
            }

            base.Dispose(disposing);
        }

        // This is called when a MX-1xxx device has became available (USB cable was plugged, or MX device was turned on),
        // or when a MX-1xxx that was previously available has become unavailable (USB cable was unplugged, turned off due to inactivity or battery drained)
        public void OnAvailabilityChanged(ReaderDevice reader)
        {
            if (Element != null)
            {
                if (reader.GetAvailability() == Availability.Available)
                {
                    Element.OnAvailabilityChanged(ScannerAvailability.Available);
                }
                else if (reader.GetAvailability() == Availability.Unavailable)
                {
                    Element.OnAvailabilityChanged(ScannerAvailability.Unavailable);
                }
            }
        }

        // The connect method has completed, here you can see whether there was an error with establishing the connection or not
        public void OnConnectionCompleted(ReaderDevice reader, Throwable error)
        {
            if (Element != null)
            {
                // If we have valid connection error param will be null,
                // otherwise here is error that inform us about issue that we have while connecting to reader device
                if (error != null)
                {
                    // ask for Camera Permission if necessary
                    if (error is CameraPermissionException)
                        Element.OnConnectionCompleted(ScannerExceptions.CameraPermissionException, error.LocalizedMessage);
                    else
                        Element.OnConnectionCompleted(ScannerExceptions.Other, error.LocalizedMessage);
                }
                else
                {
                    Element.OnConnectionCompleted(ScannerExceptions.NoException, null);
                }
            }
        }

        // This is called when a connection with the readerDevice has been changed.
        // The readerDevice is usable only in the "ConnectionState.Connected" state
        public void OnConnectionStateChanged(ReaderDevice reader)
        {
            if (Element != null)
            {
                if (reader.ConnectionState == ConnectionState.Connected)
                    Element.OnConnectionStateChanged(ScannerConnectionStatus.Connected);
                else if (reader.ConnectionState == ConnectionState.Connecting)
                    Element.OnConnectionStateChanged(ScannerConnectionStatus.Connecting);
                else if (reader.ConnectionState == ConnectionState.Disconnected)
                    Element.OnConnectionStateChanged(ScannerConnectionStatus.Disconnected);
                else if (reader.ConnectionState == ConnectionState.Disconnecting)
                    Element.OnConnectionStateChanged(ScannerConnectionStatus.Disconnecting);
            }
        }

        // This is called after scanning has completed, either by detecting a barcode, canceling the scan by using the on-screen button or a hardware trigger button, or if the scanning timed-out
        public void OnReadResultReceived(ReaderDevice reader, ReadResults results)
        {
            if (Element != null)
            {
                List<ScannedResult> resList = new List<ScannedResult>();

                if (results.SubResults != null && results.SubResults.Count > 0)
                {
                    foreach (ReadResult subResult in results.SubResults)
                    {
                        resList.Add(CreateResultItem(subResult));
                    }
                }
                else if (results.Count > 0)
                {
                    resList.Add(CreateResultItem(results.GetResultAt(0)));
                }

                Element.OnResultReceived(resList);
            }
        }

        private ScannedResult CreateResultItem(ReadResult result)
        {
            byte[] resultImageBytes = null;

            #region Create image result if enabled
            if (result.Image != null)
            {
                if (result.ImageGraphics == null)
                {
                    using (var stream = new System.IO.MemoryStream())
                    {
                        result.Image.Compress(Bitmap.CompressFormat.Png, 0, stream);
                        resultImageBytes = stream.ToArray();
                    }
                }
                else
                {
                    using (var stream = new System.IO.MemoryStream())
                    {
                        renderSvg(result.ImageGraphics, result.Image).Compress(Bitmap.CompressFormat.Png, 0, stream);
                        resultImageBytes = stream.ToArray();
                    }
                    svgBitmap.Dispose();
                    svgBitmap = null;
                }
            }
            else if (result.ImageGraphics != null)
            {
                using (var stream = new System.IO.MemoryStream())
                {
                    renderSvg(result.ImageGraphics, this.Width, this.Height).Compress(Bitmap.CompressFormat.Png, 0, stream);
                    resultImageBytes = stream.ToArray();
                }
                svgBitmap.Dispose();
                svgBitmap = null;
            }
            #endregion

            if (result.IsGoodRead)
            {
                if (result.Symbology != null)
                    return new ScannedResult(result.ParsedText ?? result.ReadString, result.Symbology.Name, result.IsGoodRead, resultImageBytes);
                else
                    return new ScannedResult(result.ParsedText ?? result.ReadString, "UNKNOWN SYMBOLOGY", result.IsGoodRead, resultImageBytes);
            }
            else
            {
                return new ScannedResult("NO READ", "", result.IsGoodRead, resultImageBytes);
            }
        }

        public void OnSymbologyEnabled(ReaderDevice reader, ReaderDevice.Symbology symbology, Java.Lang.Boolean enabled, Throwable error)
        {
            if (Element != null)
            {
                Element.OnSymbologyEnabled(NativeToPortableSymbology(symbology), (bool)enabled, error?.LocalizedMessage);
            }
        }

        private Symbology NativeToPortableSymbology(ReaderDevice.Symbology symbology)
        {
            if (symbology == ReaderDevice.Symbology.I2o5)
                return Symbology.I2o5;
            else if (symbology == ReaderDevice.Symbology.Telepen)
                return Symbology.Telepen;
            else if (symbology == ReaderDevice.Symbology.Rpc)
                return Symbology.Rpc;
            else if (symbology == ReaderDevice.Symbology.Qr)
                return Symbology.Qr;
            else if (symbology == ReaderDevice.Symbology.Postnet)
                return Symbology.Postnet;
            else if (symbology == ReaderDevice.Symbology.Planet)
                return Symbology.Planet;
            else if (symbology == ReaderDevice.Symbology.Pharmacode)
                return Symbology.Pharmacode;
            else if (symbology == ReaderDevice.Symbology.Pdf417)
                return Symbology.Pdf417;
            else if (symbology == ReaderDevice.Symbology.Ocr)
                return Symbology.Ocr;
            else if (symbology == ReaderDevice.Symbology.Msi)
                return Symbology.Msi;
            else if (symbology == ReaderDevice.Symbology.Micropdf417)
                return Symbology.Micropdf417;
            else if (symbology == ReaderDevice.Symbology.Maxicode)
                return Symbology.Maxicode;
            else if (symbology == ReaderDevice.Symbology.Vericode)
                return Symbology.Vericode;
            else if (symbology == ReaderDevice.Symbology.FourStateUpu)
                return Symbology.FourStateUpu;
            else if (symbology == ReaderDevice.Symbology.FourStateRmc)
                return Symbology.FourStateRmc;
            else if (symbology == ReaderDevice.Symbology.Unknown)
                return Symbology.Unknown;
            else if (symbology == ReaderDevice.Symbology.FourStateJap)
                return Symbology.FourStateJap;
            else if (symbology == ReaderDevice.Symbology.FourStateAus)
                return Symbology.FourStateAus;
            else if (symbology == ReaderDevice.Symbology.EanUcc)
                return Symbology.EanUcc;
            else if (symbology == ReaderDevice.Symbology.Dotcode)
                return Symbology.Dotcode;
            else if (symbology == ReaderDevice.Symbology.Datamatrix)
                return Symbology.Datamatrix;
            else if (symbology == ReaderDevice.Symbology.Databar)
                return Symbology.Databar;
            else if (symbology == ReaderDevice.Symbology.Codabar)
                return Symbology.Codabar;
            else if (symbology == ReaderDevice.Symbology.C93)
                return Symbology.C93;
            else if (symbology == ReaderDevice.Symbology.C39ConvertToC32)
                return Symbology.C39ConvertToC32;
            else if (symbology == ReaderDevice.Symbology.C39)
                return Symbology.C39;
            else if (symbology == ReaderDevice.Symbology.C25)
                return Symbology.C25;
            else if (symbology == ReaderDevice.Symbology.C128)
                return Symbology.C128;
            else if (symbology == ReaderDevice.Symbology.C11)
                return Symbology.C11;
            else if (symbology == ReaderDevice.Symbology.Azteccode)
                return Symbology.Azteccode;
            else if (symbology == ReaderDevice.Symbology.FourStateImb)
                return Symbology.FourStateImb;
            else if (symbology == ReaderDevice.Symbology.UpcEan)
                return Symbology.UpcEan;
            else
                return Symbology.Unknown;
        }

        private ReaderDevice.Symbology PortableToNativeSymbology(Symbology symbology)
        {
            switch (symbology)
            {
                case Symbology.I2o5:
                    return ReaderDevice.Symbology.I2o5;
                case Symbology.Telepen:
                    return ReaderDevice.Symbology.Telepen;
                case Symbology.Rpc:
                    return ReaderDevice.Symbology.Rpc;
                case Symbology.Qr:
                    return ReaderDevice.Symbology.Qr;
                case Symbology.Postnet:
                    return ReaderDevice.Symbology.Postnet;
                case Symbology.Planet:
                    return ReaderDevice.Symbology.Planet;
                case Symbology.Pharmacode:
                    return ReaderDevice.Symbology.Pharmacode;
                case Symbology.Pdf417:
                    return ReaderDevice.Symbology.Pdf417;
                case Symbology.Ocr:
                    return ReaderDevice.Symbology.Ocr;
                case Symbology.Msi:
                    return ReaderDevice.Symbology.Msi;
                case Symbology.Micropdf417:
                    return ReaderDevice.Symbology.Micropdf417;
                case Symbology.Maxicode:
                    return ReaderDevice.Symbology.Maxicode;
                case Symbology.Vericode:
                    return ReaderDevice.Symbology.Vericode;
                case Symbology.FourStateUpu:
                    return ReaderDevice.Symbology.FourStateUpu;
                case Symbology.FourStateRmc:
                    return ReaderDevice.Symbology.FourStateRmc;
                default:
                case Symbology.Unknown:
                    return ReaderDevice.Symbology.Unknown;
                case Symbology.FourStateJap:
                    return ReaderDevice.Symbology.FourStateJap;
                case Symbology.FourStateAus:
                    return ReaderDevice.Symbology.FourStateAus;
                case Symbology.EanUcc:
                    return ReaderDevice.Symbology.EanUcc;
                case Symbology.Dotcode:
                    return ReaderDevice.Symbology.Dotcode;
                case Symbology.Datamatrix:
                    return ReaderDevice.Symbology.Datamatrix;
                case Symbology.Databar:
                    return ReaderDevice.Symbology.Databar;
                case Symbology.Codabar:
                    return ReaderDevice.Symbology.Codabar;
                case Symbology.C93:
                    return ReaderDevice.Symbology.C93;
                case Symbology.C39ConvertToC32:
                    return ReaderDevice.Symbology.C39ConvertToC32;
                case Symbology.C39:
                    return ReaderDevice.Symbology.C39;
                case Symbology.C25:
                    return ReaderDevice.Symbology.C25;
                case Symbology.C128:
                    return ReaderDevice.Symbology.C128;
                case Symbology.C11:
                    return ReaderDevice.Symbology.C11;
                case Symbology.Azteccode:
                    return ReaderDevice.Symbology.Azteccode;
                case Symbology.FourStateImb:
                    return ReaderDevice.Symbology.FourStateImb;
                case Symbology.UpcEan:
                    return ReaderDevice.Symbology.UpcEan;
            }
        }

        private ResultParser PortableToNativeParser(ScannerParser parser)
        {
            switch (parser)
            {
                default:
                case ScannerParser.NONE:
                    return ResultParser.None;
                case ScannerParser.AUTO:
                    return ResultParser.Auto;
                case ScannerParser.AAMVA:
                    return ResultParser.Aamva;
                case ScannerParser.GS1:
                    return ResultParser.Gs1;
                case ScannerParser.HIBC:
                    return ResultParser.Hibc;
                case ScannerParser.ISBT128:
                    return ResultParser.Isbt128;
                case ScannerParser.IUID:
                    return ResultParser.Iuid;
                case ScannerParser.SCM:
                    return ResultParser.Scm;
            }
        }

        // SVG
        private static Bitmap renderSvg(System.String svgString, Bitmap bitmap)
        {
            try
            {
                if (svgBitmap != null)
                {
                    svgBitmap.Dispose();
                    svgBitmap = null;
                }

                SVG svg = SVG.GetFromString(svgString);
                svg.SetDocumentHeight(bitmap.Height.ToString());
                svg.SetDocumentWidth(bitmap.Width.ToString());

                svgBitmap = bitmap.Copy(bitmap.GetConfig(), true);
                Canvas canvas = new Canvas(svgBitmap);
                svg.RenderToCanvas(canvas);

                return svgBitmap;
            }
            catch (Java.Lang.Exception e)
            {
                e.PrintStackTrace();
            }

            return bitmap;
        }

        private static Bitmap renderSvg(System.String svgString, int width, int height)
        {
            try
            {
                if (svgBitmap != null)
                {
                    svgBitmap.Dispose();
                    svgBitmap = null;
                }

                SVG svg = SVG.GetFromString(svgString);
                svg.SetDocumentHeight(height.ToString());
                svg.SetDocumentWidth(width.ToString());

                svgBitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb4444);
                Canvas canvas = new Canvas(svgBitmap);
                svg.RenderToCanvas(canvas);

                return svgBitmap;
            }
            catch (Java.Lang.Exception e)
            {
                e.PrintStackTrace();
            }

            return null;
        }
    }

    class ResponseListener : Java.Lang.Object, DataManSystem.IOnResponseReceivedListener
    {
        private ShoppingCart.ScannerControl element;
        private string resultForDMCC;

        public ResponseListener(ShoppingCart.ScannerControl element, string resultForDMCC)
        {
            this.element = element;
            this.resultForDMCC = resultForDMCC;
        }

        public void OnResponseReceived(DataManSystem dataManSystem, DmccResponse response)
        {
            if (element != null)
            {
                element.OnResponseReceived(response.PayLoad, response.Error?.LocalizedMessage, resultForDMCC);
            }
        }
    }
}
