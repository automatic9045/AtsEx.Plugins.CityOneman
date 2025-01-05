using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BveTypes.ClassWrappers;

namespace Automatic9045.BveEx.CityOneman
{
    internal class ConductorValve
    {
        private readonly HandleSet Handles;

        private bool IsRunning = false;

        public ConductorValve(HandleSet handles)
        {
            Handles = handles;
        }

        public void Pull()
        {
            IsRunning = true;
        }

        public void Tick(double speed)
        {
            if (Handles.BrakeNotch == Handles.NotchInfo.EmergencyBrakeNotch && speed <= 0.01)
            {
                IsRunning = false;
            }

            if (IsRunning)
            {
                Handles.BrakeNotch = Handles.NotchInfo.EmergencyBrakeNotch;
            }
        }
    }
}
