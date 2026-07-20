using Busylight;
using System;

namespace BusylightShiftLight
{
    /// <summary>
    /// Controls kuando Busylight hardware through Plenom's official SDK.
    /// </summary>
    public sealed class BusylightController : IDisposable
    {
        private readonly object _sync = new object();
        private SDK _sdk;
        private bool _isConnected;
        private bool _disposed;
        private DateTime _nextConnectionAttemptUtc = DateTime.MinValue;

        public bool IsConnected
        {
            get
            {
                lock (_sync)
                {
                    return _isConnected;
                }
            }
        }

        public bool Connect()
        {
            lock (_sync)
            {
                ThrowIfDisposed();

                try
                {
                    if (_sdk == null)
                    {
                        _sdk = new SDK();
                        _sdk.OnBusylightChanged += OnBusylightChanged;
                    }

                    _sdk.CheckUSB();
                    RefreshConnectionState();
                    _nextConnectionAttemptUtc = DateTime.UtcNow.AddSeconds(2);

                    if (_isConnected)
                    {
                        _sdk.Light(0, 0, 0);
                        SimHub.Logging.Current.Info("Busylight connected through the official Plenom SDK.");
                    }

                    return _isConnected;
                }
                catch (Exception ex)
                {
                    _isConnected = false;
                    SimHub.Logging.Current.Error($"Unable to initialize the Busylight SDK: {ex.Message}");
                    return false;
                }
            }
        }

        public bool SetColor(byte red, byte green, byte blue)
        {
            lock (_sync)
            {
                if (!EnsureConnected())
                {
                    return false;
                }

                try
                {
                    // Plenom's SDK uses the nonstandard parameter order red, blue, green.
                    _sdk.Light(red, blue, green);
                    return true;
                }
                catch (Exception ex)
                {
                    _isConnected = false;
                    SimHub.Logging.Current.Error($"Unable to set Busylight color: {ex.Message}");
                    return false;
                }
            }
        }

        public bool TurnOff()
        {
            lock (_sync)
            {
                if (!EnsureConnected())
                {
                    return false;
                }

                try
                {
                    _sdk.Light(0, 0, 0);
                    return true;
                }
                catch (Exception ex)
                {
                    _isConnected = false;
                    SimHub.Logging.Current.Error($"Unable to turn off Busylight: {ex.Message}");
                    return false;
                }
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed)
                {
                    return;
                }

                if (_sdk != null)
                {
                    try
                    {
                        _sdk.OnBusylightChanged -= OnBusylightChanged;
                        if (_isConnected)
                        {
                            _sdk.Light(0, 0, 0);
                        }

                        _sdk.Terminate();
                    }
                    catch (Exception ex)
                    {
                        SimHub.Logging.Current.Error($"Unable to shut down the Busylight SDK cleanly: {ex.Message}");
                    }
                }

                _sdk = null;
                _isConnected = false;
                _disposed = true;
            }
        }

        private bool EnsureConnected()
        {
            if (_disposed)
            {
                return false;
            }

            if (_isConnected)
            {
                return true;
            }

            if (_sdk == null)
            {
                return Connect();
            }

            if (DateTime.UtcNow < _nextConnectionAttemptUtc)
            {
                return false;
            }

            _nextConnectionAttemptUtc = DateTime.UtcNow.AddSeconds(2);
            try
            {
                _sdk.CheckUSB();
                RefreshConnectionState();
            }
            catch (Exception ex)
            {
                _isConnected = false;
                SimHub.Logging.Current.Debug($"Busylight reconnect attempt failed: {ex.Message}");
            }

            return _isConnected;
        }

        private void OnBusylightChanged()
        {
            lock (_sync)
            {
                if (!_disposed)
                {
                    RefreshConnectionState();
                }
            }
        }

        private void RefreshConnectionState()
        {
            IBusylightDevice[] devices = _sdk.GetAttachedBusylightDeviceList();
            _isConnected = _sdk.IsLightSupported && devices != null && devices.Length > 0;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(BusylightController));
            }
        }
    }
}
