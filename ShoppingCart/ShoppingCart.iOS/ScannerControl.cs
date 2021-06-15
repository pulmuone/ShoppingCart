using CMBSDK;
using CoreGraphics;
using Foundation;
using System;
using System.Collections.Generic;
using UIKit;
using Xamarin.Forms.Platform.iOS;

[assembly: Xamarin.Forms.ExportRenderer(typeof(ShoppingCart.ScannerControl), typeof(ShoppingCart.iOS.ScannerControl))]
namespace ShoppingCart.iOS
{
    public class ScannerControl : ViewRenderer<ShoppingCart.ScannerControl, UIView>, ICMBReaderDeviceDelegate
    {
        private UIView container;

        private CMBReaderDevice readerDevice;
        private CDMCameraMode cameraMode = CDMCameraMode.NoAimer;

        private UIAlertController connectingAlert;

        private NSObject didBecomeActiveObserver;

        protected override void OnElementChanged(ElementChangedEventArgs<ShoppingCart.ScannerControl> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null || Element == null)
            {
                return;
            }

            container = new UIView();

            if (Control == null)
                SetNativeControl(container);

            //Implement element event handlers. We will invoke them from portable project
            Element.OnDisconnect += (object sender, EventArgs args) =>
            {
                if (readerDevice != null && readerDevice.ConnectionState == CMBConnectionState.Connected)
                {
                    readerDevice.Disconnect();
                }
            };
            Element.OnConnect += (object sender, EventArgs args) =>
            {
                ConnectToReaderDevice();
            };
            Element.OnStopScanning += (object sender, EventArgs args) =>
            {
                if (readerDevice != null && readerDevice.ConnectionState == CMBConnectionState.Connected)
                {
                    readerDevice.StopScanning();
                }
            };
            Element.OnStartScanning += (object sender, EventArgs args) =>
            {
                if (readerDevice != null && readerDevice.ConnectionState == CMBConnectionState.Connected)
                    readerDevice.StartScanning();
            };
            Element.OnGetPhoneCameraDevice += (object sender, object[] args) =>
            {
                if (readerDevice != null)
                {
                    readerDevice.WeakDelegate = null;
                    readerDevice.Disconnect();
                    readerDevice.Dispose();
                    readerDevice = null;
                }

                cameraMode = (CDMCameraMode)(int)args[0];

                if (args.Length > 3)
                    readerDevice = CMBReaderDevice.ReaderOfDeviceCameraWithCameraMode((CDMCameraMode)(int)args[0], (CDMPreviewOption)(int)args[1], ((bool)args[2]) ? null : Control, args[3].ToString());
                else
                    readerDevice = CMBReaderDevice.ReaderOfDeviceCameraWithCameraMode((CDMCameraMode)(int)args[0], (CDMPreviewOption)(int)args[1], ((bool)args[2]) ? null : Control);

                readerDevice.WeakDelegate = this;
            };
            Element.OnGetMXDevice += (object sender, EventArgs args) =>
            {
                if (readerDevice != null)
                {
                    readerDevice.WeakDelegate = null;
                    readerDevice.Disconnect();
                    readerDevice.Dispose();
                    readerDevice = null;
                }

                readerDevice = CMBReaderDevice.ReaderOfMXDevice();

                readerDevice.WeakDelegate = this;
            };
            Element.OnSdkVersion += (object sender, EventArgs args) =>
            {
                if (Element != null)
                    Element.OnGetSdkVersion(CDMDataManSystem.Version);
            };
            Element.OnSetSymbologyEnabled += (object sender, object[] args) =>
            {
                if (readerDevice != null && readerDevice.ConnectionState == CMBConnectionState.Connected)
                {
                    readerDevice.SetSymbology(PortableToNativeSymbology((Symbology)args[0]), (bool)args[1], (error) =>
                    {
                        if (Element != null)
                        {
                            Element.OnSymbologyEnabled((Symbology)args[0], (bool)args[1], error?.LocalizedDescription);
                        }
                    });
                }
            };
            Element.OnEnableImage += (object sender, bool enable) =>
            {
                if (readerDevice != null && readerDevice.ConnectionState == CMBConnectionState.Connected)
                    readerDevice.ImageResultEnabled = enable;
            };
            Element.OnEnableImageGraphics += (object sender, bool enable) =>
            {
                if (readerDevice != null && readerDevice.ConnectionState == CMBConnectionState.Connected)
                    readerDevice.SVGResultEnabled = enable;
            };
            Element.OnSendCommand += (object sender, string dmcc) =>
            {
                if (readerDevice != null && readerDevice.ConnectionState == CMBConnectionState.Connected)
                    readerDevice.DataManSystem.SendCommand(dmcc, (response) =>
                    {
                        if (Element != null)
                        {
                            Element.OnResponseReceived(response.Payload, (response.Status != CDMResponseStatus.DMCC_STATUS_NO_ERROR ? response.Status.ToString() : null), dmcc);
                        }
                    });
            };
            Element.OnSetParser += (object sender, ScannerParser parserType) =>
            {
                if (readerDevice != null && readerDevice.ConnectionState == CMBConnectionState.Connected)
                    readerDevice.Parser = (CMBResultParser)(int)parserType;
            };

            didBecomeActiveObserver = NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidBecomeActiveNotification, (ntf) =>
            {
                ConnectToReaderDevice();
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (didBecomeActiveObserver != null)
            {
                NSNotificationCenter.DefaultCenter.RemoveObserver(didBecomeActiveObserver);
                didBecomeActiveObserver.Dispose();
                didBecomeActiveObserver = null;
            }

            if (readerDevice != null)
            {
                readerDevice.WeakDelegate = null;
                readerDevice.Disconnect();
                readerDevice.Dispose();
                readerDevice = null;
            }

            base.Dispose(disposing);
        }

        private void ConnectToReaderDevice()
        {
            if (readerDevice != null && readerDevice.ConnectionState != CMBConnectionState.Connecting && readerDevice.ConnectionState != CMBConnectionState.Connected)
            {
                if (readerDevice.DeviceClass == DataManDeviceClass.PhoneCamera && cameraMode == CDMCameraMode.ActiveAimer)
                {
                    connectingAlert = UIAlertController.Create("Connecting", null, UIAlertControllerStyle.Alert);
                    UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(connectingAlert, true, null);
                }

                readerDevice.ConnectWithCompletion((error) =>
                {
                    if (Element != null)
                    {
                        // If we have valid connection error param will be null,
                        // otherwise here is error that inform us about issue that we have while connecting to reader device
                        if (error != null)
                        {
                            Element.OnConnectionCompleted(ScannerExceptions.Other, "Failed to connect");
                        }
                        else
                        {
                            Element.OnConnectionCompleted(ScannerExceptions.NoException, null);
                        }
                    }
                });
            }
        }

        // This is called when a MX-1xxx device has became available (USB cable was plugged, or MX device was turned on),
        // or when a MX-1xxx that was previously available has become unavailable (USB cable was unplugged, turned off due to inactivity or battery drained)
        [Export("availabilityDidChangeOfReader:")]
        public void AvailabilityDidChangeOfReader(CMBReaderDevice reader)
        {
            if (Element != null)
            {
                if (reader.Availability == CMBReaderAvailibility.Available)
                {
                    Element.OnAvailabilityChanged(ScannerAvailability.Available);
                }
                else if (reader.Availability == CMBReaderAvailibility.Unavailable)
                {
                    Element.OnAvailabilityChanged(ScannerAvailability.Unavailable);
                }
            }
        }

        // This is called when a connection with the readerDevice has been changed.
        // The readerDevice is usable only in the "CMBConnectionStateConnected" state
        [Export("connectionStateDidChangeOfReader:")]
        public void ConnectionStateDidChangeOfReader(CMBReaderDevice reader)
        {
            if (Element != null)
            {
                if (reader.ConnectionState == CMBConnectionState.Connected)
                {
                    if (connectingAlert != null)
                    {
                        connectingAlert.DismissViewController(true, () =>
                        {
                            connectingAlert = null;
                        });
                    }

                    Element.OnConnectionStateChanged(ScannerConnectionStatus.Connected);

                    if (reader.DeviceClass == DataManDeviceClass.PhoneCamera)
                        reader.DataManSystem.SendCommand("SET IMAGE.SIZE 0");
                }
                else if (reader.ConnectionState == CMBConnectionState.Connecting)
                    Element.OnConnectionStateChanged(ScannerConnectionStatus.Connecting);
                else if (reader.ConnectionState == CMBConnectionState.Disconnected)
                {
                    if (connectingAlert != null)
                    {
                        connectingAlert.DismissViewController(true, () =>
                        {
                            connectingAlert = null;
                        });
                    }

                    Element.OnConnectionStateChanged(ScannerConnectionStatus.Disconnected);
                }
                else if (reader.ConnectionState == CMBConnectionState.Disconnecting)
                    Element.OnConnectionStateChanged(ScannerConnectionStatus.Disconnecting);
            }
        }

        // This is called after scanning has completed, either by detecting a barcode, canceling the scan by using the on-screen button or a hardware trigger button, or if the scanning timed-out
        [Export("didReceiveReadResultFromReader:results:")]
        public void DidReceiveReadResultFromReader(CMBReaderDevice reader, CMBReadResults readResults)
        {
            if (Element != null)
            {
                List<ScannedResult> resList = new List<ScannedResult>();

                if (readResults.SubReadResults != null && readResults.SubReadResults.Length > 0)
                {
                    foreach (CMBReadResult subResult in readResults.SubReadResults)
                    {
                        resList.Add(CreateResultItem(subResult));
                    }
                }
                else if (readResults.ReadResults.Length > 0)
                {
                    resList.Add(CreateResultItem((CMBReadResult)readResults.ReadResults[0]));
                }

                Element.OnResultReceived(resList);
            }
        }

        private ScannedResult CreateResultItem(CMBReadResult result)
        {
            byte[] resultImageBytes = null;

            #region Create image result if enabled
            if (result.Image != null)
            {
                if (result.ImageGraphics == null)
                {
                    using (NSData imageData = result.Image.AsPNG())
                    {
                        resultImageBytes = new byte[imageData.Length];
                        resultImageBytes = new byte[imageData.Length];
                        System.Runtime.InteropServices.Marshal.Copy(imageData.Bytes, resultImageBytes, 0, Convert.ToInt32(imageData.Length));
                    }
                }
                else
                {
                    CGSize size = new CGSize(result.Image.Size.Width, result.Image.Size.Height);

                    var parser = new CMBSVG.CDMSVGParser(result.ImageGraphics);
                    var svgData = parser.Parse;
                    var renderer = new CMBSVG.CDMSVGRenderer(svgData);

                    UIGraphics.BeginImageContextWithOptions(size, false, 0);
                    result.Image.Draw(new CGRect(0, 0, size.Width, result.Image.Size.Height));
                    renderer.ImageFromSVGWithSize(result.Image.Size, new UIImage()).Draw(new CGRect(0, 0, size.Width, result.Image.Size.Height));
                    UIImage finalImage = UIGraphics.GetImageFromCurrentImageContext();
                    UIGraphics.EndImageContext();

                    using (NSData imageData = finalImage.AsPNG())
                    {
                        resultImageBytes = new byte[imageData.Length];
                        System.Runtime.InteropServices.Marshal.Copy(imageData.Bytes, resultImageBytes, 0, Convert.ToInt32(imageData.Length));
                    }

                    finalImage.Dispose();
                    finalImage = null;
                }
            }
            else if (result.ImageGraphics != null)
            {
                var parser = new CMBSVG.CDMSVGParser(result.ImageGraphics);
                var svgData = parser.Parse;
                var renderer = new CMBSVG.CDMSVGRenderer(svgData);

                using (NSData imageData = renderer.ImageFromSVGWithSize(svgData.Size, new UIImage()).AsPNG())
                {
                    resultImageBytes = new byte[imageData.Length];
                    System.Runtime.InteropServices.Marshal.Copy(imageData.Bytes, resultImageBytes, 0, Convert.ToInt32(imageData.Length));
                }
            }
            #endregion

            if (result.GoodRead)
            {
                return new ScannedResult(result.ParsedText ?? result.ReadString, DisplayStringForSymbology(result.Symbology), result.GoodRead, resultImageBytes);
            }
            else
            {
                return new ScannedResult("NO READ", "", result.GoodRead, resultImageBytes);
            }
        }

        public static string DisplayStringForSymbology(CMBSymbology symbology)
        {
            switch (symbology)
            {

                case CMBSymbology.DataMatrix:
                    return "DATAMATRIX";

                case CMBSymbology.Qr:
                    return "QR";

                case CMBSymbology.C128:
                    return "C128";

                case CMBSymbology.UpcEan:
                    return "UPC-EAN";

                case CMBSymbology.C39:
                    return "C39";

                case CMBSymbology.C93:
                    return "C93";

                case CMBSymbology.C11:
                    return "C11";

                case CMBSymbology.I2o5:
                    return "I2O5";

                case CMBSymbology.CodaBar:
                    return "CODABAR";

                case CMBSymbology.EanUcc:
                    return "EAN-UCC";

                case CMBSymbology.PharmaCode:
                    return "PHARMACODE";

                case CMBSymbology.Maxicode:
                    return "MAXICODE";

                case CMBSymbology.Pdf417:
                    return "PDF417";

                case CMBSymbology.Micropdf417:
                    return "MICROPDF417";

                case CMBSymbology.Databar:
                    return "DATABAR";

                case CMBSymbology.Postnet:
                    return "POSTNET";

                case CMBSymbology.Planet:
                    return "PLANET";

                case CMBSymbology.FourStateJap:
                    return "4STATE-JAP";

                case CMBSymbology.FourStateAus:
                    return "4STATE-AUS";

                case CMBSymbology.FourStateUpu:
                    return "4STATE-UPU";

                case CMBSymbology.FourStateImb:
                    return "4STATE-IMB";

                case CMBSymbology.Vericode:
                    return "VERICODE";

                case CMBSymbology.Rpc:
                    return "RPC";

                case CMBSymbology.Msi:
                    return "MSI";

                case CMBSymbology.Azteccode:
                    return "AZTECCODE";

                case CMBSymbology.Dotcode:
                    return "DOTCODE";

                case CMBSymbology.C25:
                    return "C25";

                case CMBSymbology.C39ConvertToC32:
                    return "C39-CONVERT-TO-C32";

                case CMBSymbology.Ocr:
                    return "OCR";

                case CMBSymbology.FourStateRmc:
                    return "4STATE-RMC";

                case CMBSymbology.Telepen:
                    return "TELEPEN";

                default:
                    return "UNKNOWN";
            }
        }

        private CMBSymbology PortableToNativeSymbology(Symbology symbology)
        {
            switch (symbology)
            {
                default:
                case Symbology.Unknown:
                    return CMBSymbology.Unknown;
                case Symbology.Datamatrix:
                    return CMBSymbology.DataMatrix;
                case Symbology.Qr:
                    return CMBSymbology.Qr;
                case Symbology.C128:
                    return CMBSymbology.C128;
                case Symbology.UpcEan:
                    return CMBSymbology.UpcEan;
                case Symbology.C11:
                    return CMBSymbology.C11;
                case Symbology.C39:
                    return CMBSymbology.C39;
                case Symbology.C93:
                    return CMBSymbology.C93;
                case Symbology.I2o5:
                    return CMBSymbology.I2o5;
                case Symbology.Codabar:
                    return CMBSymbology.CodaBar;
                case Symbology.EanUcc:
                    return CMBSymbology.EanUcc;
                case Symbology.Pharmacode:
                    return CMBSymbology.PharmaCode;
                case Symbology.Maxicode:
                    return CMBSymbology.Maxicode;
                case Symbology.Pdf417:
                    return CMBSymbology.Pdf417;
                case Symbology.Micropdf417:
                    return CMBSymbology.Micropdf417;
                case Symbology.Databar:
                    return CMBSymbology.Databar;
                case Symbology.Planet:
                    return CMBSymbology.Planet;
                case Symbology.Postnet:
                    return CMBSymbology.Postnet;
                case Symbology.FourStateJap:
                    return CMBSymbology.FourStateJap;
                case Symbology.FourStateAus:
                    return CMBSymbology.FourStateAus;
                case Symbology.FourStateUpu:
                    return CMBSymbology.FourStateUpu;
                case Symbology.FourStateImb:
                    return CMBSymbology.FourStateImb;
                case Symbology.Vericode:
                    return CMBSymbology.Vericode;
                case Symbology.Rpc:
                    return CMBSymbology.Rpc;
                case Symbology.Msi:
                    return CMBSymbology.Msi;
                case Symbology.Azteccode:
                    return CMBSymbology.Azteccode;
                case Symbology.Dotcode:
                    return CMBSymbology.Dotcode;
                case Symbology.C25:
                    return CMBSymbology.C25;
                case Symbology.C39ConvertToC32:
                    return CMBSymbology.C39ConvertToC32;
                case Symbology.Ocr:
                    return CMBSymbology.Ocr;
                case Symbology.FourStateRmc:
                    return CMBSymbology.FourStateRmc;
                case Symbology.Telepen:
                    return CMBSymbology.Telepen;
            }
        }
    }
}
