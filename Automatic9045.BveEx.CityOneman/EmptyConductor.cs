using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BveTypes.ClassWrappers;

using BveEx.Extensions.ConductorPatch;

namespace Automatic9045.BveEx.CityOneman
{
    internal class EmptyConductor : ConductorBase
    {
        protected override event EventHandler FixStopPositionRequested;
        protected override event EventHandler StopPositionChecked;
        protected override event EventHandler DoorOpening;
        protected override event EventHandler DepartureSoundPlaying;
        protected override event EventHandler DoorClosing;
        protected override event EventHandler DoorClosed;

        public EmptyConductor(Conductor original) : base(original)
        {
        }
    }
}
