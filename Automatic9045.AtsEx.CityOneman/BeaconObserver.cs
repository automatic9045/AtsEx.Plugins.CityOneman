using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AtsEx.PluginHost;
using AtsEx.PluginHost.Native;

namespace Automatic9045.AtsEx.CityOneman
{
    internal class BeaconObserver : IDisposable
    {
        private readonly INative Native;
        private readonly int BeaconType;

        public bool IsEnabled { get; private set; }

        public event EventHandler EnabledChanged;

        public BeaconObserver(INative native, int beaconType, bool isEnabledByDefault)
        {
            Native = native;
            BeaconType = beaconType;
            IsEnabled = isEnabledByDefault;

            Native.BeaconPassed += OnBeaconPassed;
        }

        public void Dispose()
        {
            Native.BeaconPassed -= OnBeaconPassed;
        }

        private void OnBeaconPassed(BeaconPassedEventArgs e)
        {
            if (e.Type != BeaconType) return;

            bool wasEnabled = IsEnabled;
            switch (e.Optional)
            {
                case 0:
                    IsEnabled = false;
                    break;

                default:
                    IsEnabled = true;
                    break;
            }

            if (IsEnabled != wasEnabled) EnabledChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
