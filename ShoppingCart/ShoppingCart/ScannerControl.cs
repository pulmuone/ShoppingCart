using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace ShoppingCart
{
    public class ScannerControl : View
    {
        public ScannerControl()
        {
        }

        public event EventHandler OnDisconnect;
        public void Disconnect()
        {
            OnDisconnect?.Invoke(this, null);
        }

        public event EventHandler OnConnect;
        public void Connect()
        {
            OnConnect?.Invoke(this, null);
        }

        public event EventHandler OnStopScanning;
        public void StopScanning()
        {
            OnStopScanning?.Invoke(this, null);
        }

        public event EventHandler OnStartScanning;
        public void StartScanning()
        {
            OnStartScanning?.Invoke(this, null);
        }

        public event EventHandler<object[]> OnGetPhoneCameraDevice;
        public void GetPhoneCameraDevice(ScannerCameraMode cameraMode, ScannerPreviewOption previewOption, bool fullScreen)
        {
            OnGetPhoneCameraDevice?.Invoke(this, new object[] { cameraMode, previewOption, fullScreen });
        }

        public void GetPhoneCameraDevice(ScannerCameraMode cameraMode, ScannerPreviewOption previewOption, bool fullScreen, string registrationKey)
        {
            OnGetPhoneCameraDevice?.Invoke(this, new object[] { cameraMode, previewOption, fullScreen, registrationKey });
        }

        public event EventHandler OnGetMXDevice;
        public void GetMXDevice()
        {
            OnGetMXDevice?.Invoke(this, null);
        }

        public event EventHandler<ScannerAvailability> AvailabilityChanged;
        public void OnAvailabilityChanged(ScannerAvailability args)
        {
            AvailabilityChanged?.Invoke(this, args);
        }

        public event EventHandler<object[]> ConnectionCompleted;
        public void OnConnectionCompleted(ScannerExceptions type, string message)
        {
            ConnectionCompleted?.Invoke(this, new object[] { type, message });
        }

        public event EventHandler<ScannerConnectionStatus> ConnectionStateChanged;
        public void OnConnectionStateChanged(ScannerConnectionStatus args)
        {
            ConnectionStateChanged?.Invoke(this, args);
        }

        public event EventHandler<List<ScannedResult>> ResultReceived;
        public void OnResultReceived(List<ScannedResult> args)
        {
            ResultReceived?.Invoke(this, args);
        }

        public event EventHandler OnSdkVersion;
        public void SdkVersion()
        {
            OnSdkVersion?.Invoke(this, null);
        }

        public event EventHandler<string> GetSdkVersion;
        public void OnGetSdkVersion(string args)
        {
            GetSdkVersion?.Invoke(this, args);
        }

        public event EventHandler<object[]> OnSetSymbologyEnabled;
        public void SetSymbologyEnabled(Symbology symbology, bool enable)
        {
            OnSetSymbologyEnabled?.Invoke(this, new object[] { symbology, enable });
        }

        public event EventHandler<object[]> SymbologyEnabled;
        public void OnSymbologyEnabled(Symbology symbology, bool isEnabled, string error)
        {
            SymbologyEnabled?.Invoke(this, new object[] { symbology, isEnabled, error });
        }

        public event EventHandler<bool> OnEnableImage;
        public void EnableImage(bool enable)
        {
            OnEnableImage?.Invoke(this, enable);
        }

        public event EventHandler<bool> OnEnableImageGraphics;
        public void EnableImageGraphics(bool enable)
        {
            OnEnableImageGraphics?.Invoke(this, enable);
        }

        public event EventHandler<string> OnSendCommand;
        public void SendCommand(string dmcc)
        {
            OnSendCommand?.Invoke(this, dmcc);
        }

        public event EventHandler<object[]> ResponseReceived;
        public void OnResponseReceived(string payload, string error, string dmcc)
        {
            ResponseReceived?.Invoke(this, new object[] { payload, error, dmcc });
        }
        public event EventHandler<ScannerParser> OnSetParser;
        public void SetParser(ScannerParser parserType)
        {
            OnSetParser?.Invoke(this, parserType);
        }
    }

    public class SetSymbologyEnabledArgs : EventArgs
    {

    }

    public class ScannedResult
    {
        private string resultCode;
        private string resultSymbology;
        private bool isGoodRead;
        private byte[] resultImage;

        public ScannedResult() { }

        public ScannedResult(string _resultCode, string _resultSymbology, bool _isGoodRead, byte[] _resultImage)
        {
            this.resultCode = _resultCode;
            this.resultSymbology = _resultSymbology;
            this.isGoodRead = _isGoodRead;
            this.resultImage = _resultImage;
        }

        public string ResultCode
        {
            get { return resultCode; }
            set { resultCode = value; }
        }

        public string ResultSymbology
        {
            get { return resultSymbology; }
            set { resultSymbology = value; }
        }

        public bool IsGoodRead
        {
            get { return isGoodRead; }
            set { isGoodRead = value; }
        }

        public byte[] ResultImage
        {
            get { return resultImage; }
            set { resultImage = value; }
        }
    }

    public enum Symbology
    {
        I2o5,
        Telepen,
        Rpc,
        Qr,
        Postnet,
        Planet,
        Pharmacode,
        Pdf417,
        Ocr,
        Msi,
        Micropdf417,
        Maxicode,
        Vericode,
        FourStateUpu,
        FourStateRmc,
        Unknown,
        FourStateJap,
        FourStateAus,
        EanUcc,
        Dotcode,
        Datamatrix,
        Databar,
        Codabar,
        C93,
        C39ConvertToC32,
        C39,
        C25,
        C128,
        C11,
        Azteccode,
        FourStateImb,
        UpcEan
    }

    public enum ScannerDevice
    {
        MXScanner,
        PhoneCamera
    }

    public enum ScannerConnectionStatus
    {
        Connected,
        Connecting,
        Disconnected,
        Disconnecting
    }

    public enum ScannerCameraMode
    {
        NoAimer,
        PassiveAimer,
        ActiveAimer,
        FrontCamera
    }

    public enum ScannerPreviewOption
    {
        Defaults = 0,
        NoZoomButton = 1,
        NoIlluminationButton = 2,
        HardwareTrigger = 4,
        Paused = 8,
        AlwaysShow = 16,
        PessimisticCaching = 32,
        HighResolution = 64,
        HighFrameRate = 128,
        ShowCloseButton = 256
    }

    public enum ScannerAvailability
    {
        Available,
        Unavailable,
        Unknown
    }

    public enum ScannerExceptions
    {
        NoException,
        CameraPermissionException,
        Other
    }

    public enum ScannerParser
    {
        NONE,
        AUTO,
        AAMVA,
        GS1,
        HIBC,
        ISBT128,
        IUID,
        SCM
    }
}
