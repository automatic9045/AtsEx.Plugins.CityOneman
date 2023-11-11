using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BveTypes.ClassWrappers;

using AtsEx.Extensions;
using AtsEx.Extensions.ConductorPatch;

namespace Automatic9045.AtsEx.CityOneman
{
    internal class ManualConductor : ConductorBase
    {
        private static readonly DoorSide[] DoorSides = (DoorSide[])Enum.GetValues(typeof(DoorSide));
        private static readonly Random Random = new Random();

        private readonly StationListEx StationListEx;

        private readonly Dictionary<int, DoorSwitch> DoorSwitches = new Dictionary<int, DoorSwitch>()
        {
            { (int)DoorSide.Left, new DoorSwitch() },
            { (int)DoorSide.Right, new DoorSwitch() },
        };

        private bool HasStopPositionChecked = false;
        private TimeSpan MinDepartureSoundPlayTime = TimeSpan.Zero;

        private bool IsMoving => 5 / 3.6 <= Math.Abs(Original.LocationManager.SpeedMeterPerSecond);

        public bool IsLeftReopening
        {
            get => DoorSwitches[(int)DoorSide.Left].IsReopening;
            set
            {
                DoorSwitch doorSwitch = DoorSwitches[(int)DoorSide.Left];
                if (doorSwitch.IsReopening && !value && !doorSwitch.IsSwitchOn) FinishReopen(DoorSide.Left);
                doorSwitch.IsReopening = value;
            }
        }

        public bool IsRightReopening
        {
            get => DoorSwitches[(int)DoorSide.Right].IsReopening;
            set
            {
                DoorSwitch doorSwitch = DoorSwitches[(int)DoorSide.Right];
                if (doorSwitch.IsReopening && !value && !doorSwitch.IsSwitchOn) FinishReopen(DoorSide.Right);
                doorSwitch.IsReopening = value;
            }
        }

        private void FinishReopen(DoorSide doorSide)
        {
            (_, Station nextStation) = StationListEx.GetStation(1);
            SideDoorSet sideDoors = Original.Doors.GetSide(doorSide);

            foreach (CarDoor door in sideDoors.CarDoors)
            {
                if (door.IsOpen) door.Close((int)(nextStation.StuckInDoorMilliseconds * Random.NextDouble() * Random.NextDouble()));
            }
        }

        protected override event EventHandler FixStopPositionRequested;
        protected override event EventHandler StopPositionChecked;
        protected override event EventHandler DoorOpening;
        protected override event EventHandler DepartureSoundPlaying;
        protected override event EventHandler DoorClosing;
        protected override event EventHandler DoorClosed;

        public ManualConductor(Conductor original) : base(original)
        {
            StationListEx = new StationListEx(Original.Stations, () => Original.LocationManager.Location);
        }

        public void Sync()
        {
            OpenOrClose(DoorSide.Left);
            OpenOrClose(DoorSide.Right);


            void OpenOrClose(DoorSide side)
            {
                SideDoorSet doors = Original.Doors.GetSide(side);
                if (doors.IsOpen) OpenDoors(side); else CloseDoors(side);
            }
        }

        public (int nextStationIndex, Station nextStation) Desync()
        {
            IsLeftReopening = false;
            IsRightReopening = false;

            CloseDoors(DoorSide.Left);
            CloseDoors(DoorSide.Right);

            (int nextStationIndex, Station nextStation) = StationListEx.GetStation(1);
            
            SideDoorSet leftDoors = Original.Doors.GetSide(DoorSide.Left);
            SideDoorSet rightDoors = Original.Doors.GetSide(DoorSide.Right);

            if (nextStation is null || !(Math.Abs(Original.LocationManager.SpeedMeterPerSecond) < 0.01f && StationListEx.IsAtValidPosition(nextStationIndex)))
            {
                leftDoors.CloseDoors(0);
                rightDoors.CloseDoors(0);
            }
            else
            {
                switch (nextStation.DoorSide)
                {
                    case -1:
                        leftDoors.OpenDoors();
                        rightDoors.CloseDoors(0);
                        break;

                    case 1:
                        leftDoors.CloseDoors(0);
                        rightDoors.OpenDoors();
                        break;

                    default:
                        leftDoors.CloseDoors(0);
                        rightDoors.CloseDoors(0);
                        break;
                }
            }

            return (nextStationIndex, nextStation);
        }

        protected override MethodOverrideMode OnJumped(int stationIndex, bool isDoorClosed)
        {
            HasStopPositionChecked = false;

            Original.Stations.GoTo(stationIndex - 1);
            Original.Doors.SetState(DoorState.Close, DoorState.Close);

            Station currentStation = Original.Stations.Count <= stationIndex ? null : Original.Stations[stationIndex] as Station;
            int doorSide = currentStation is null || currentStation.Pass || isDoorClosed ? 0 : currentStation.DoorSide;
            if (doorSide == 0) Original.Stations.GoTo(stationIndex);

            return MethodOverrideMode.SkipOriginal;
        }

        protected override MethodOverrideMode OnDoorStateChanged()
        {
            if (Original.Doors.AreAllClosingOrClosed && HasStopPositionChecked)
            {
                HasStopPositionChecked = false;
                Original.Stations.GoTo(Original.Stations.CurrentIndex + 1);
                DoorClosed(this, EventArgs.Empty);
            }

            return MethodOverrideMode.SkipOriginal;
        }

        protected override MethodOverrideMode OnTick()
        {
            (int nextStationIndex, Station nextStation) = StationListEx.GetStation(1);
            if (!(nextStation is null))
            {
                if (nextStation.Pass || nextStation.DoorSide == 0)
                {
                    double location = Original.LocationManager.Location;
                    if ((Math.Abs(Original.LocationManager.SpeedMeterPerSecond) < 0.01f && location >= nextStation.MinStopPosition) || location >= nextStation.MaxStopPosition)
                    {
                        Original.Stations.GoTo(Original.Stations.CurrentIndex + 1);
                    }
                }
                else
                {
                    TimeSpan now = Original.TimeManager.Time;
                    if (MinDepartureSoundPlayTime != TimeSpan.Zero
                        && HasStopPositionChecked
                        && MinDepartureSoundPlayTime <= now
                        && nextStation.DepertureTime - nextStation.StoppageTime <= now
                        && 0 < Original.SectionManager.ForwardSectionSpeedLimit)
                    {
                        MinDepartureSoundPlayTime = TimeSpan.Zero;

                        StationListEx.GetStation(1).Station?.DepertureSound?.Play(1, 1, 0);
                        DepartureSoundPlaying(this, EventArgs.Empty);
                    }
                }
            }

            foreach (DoorSide doorSide in DoorSides)
            {
                DoorSwitch doorSwitch = DoorSwitches[(int)doorSide];
                SideDoorSet sideDoors = Original.Doors.GetSide(doorSide);

                if (doorSwitch.IsSwitchOn && !IsMoving)
                {
                    if (!doorSwitch.IsOpened)
                    {
                        if (!(nextStation is null) && StationListEx.IsNearestStation(nextStationIndex) && nextStation.DoorSide == ToDoorSideNumber(doorSide))
                        {
                            OpenDoorsAt(nextStation, !HasStopPositionChecked);
                        }
                        else
                        {
                            (int stationIndex, Station station) = StationListEx.GetStation(0);

                            if (!(station is null) && StationListEx.IsNearestStation(stationIndex) && station.DoorSide == ToDoorSideNumber(doorSide))
                            {
                                Original.Stations.GoTo(stationIndex - 1);
                                OpenDoorsAt(station, false);
                            }
                            else
                            {
                                sideDoors.OpenDoors();
                            }
                        }

                        doorSwitch.IsOpened = true;
                    }
                }
                else
                {
                    if (doorSwitch.IsOpened)
                    {
                        if (!(nextStation is null) && StationListEx.IsNearestStation(nextStationIndex) && nextStation.DoorSide == ToDoorSideNumber(doorSide))
                        {
                            sideDoors.CloseDoors(nextStation.StuckInDoorMilliseconds);
                            DoorClosing(this, EventArgs.Empty);
                        }
                        else
                        {
                            sideDoors.CloseDoors(nextStation is null ? 0 : nextStation.StuckInDoorMilliseconds);
                        }

                        doorSwitch.IsOpened = false;
                    }
                }

                if (!doorSwitch.IsSwitchOn && doorSwitch.IsReopening && !IsMoving)
                {
                    foreach (CarDoor door in sideDoors.CarDoors)
                    {
                        if (door.IsOpen) door.Open();
                    }
                }


                void OpenDoorsAt(Station station, bool isFirstTime)
                {
                    HasStopPositionChecked = true;

                    if (isFirstTime)
                    {
                        StopPositionChecked(this, EventArgs.Empty);

                        MinDepartureSoundPlayTime = Original.TimeManager.Time + TimeSpan.FromSeconds(5);

                        station.ArrivalSound?.Play(1, 1, 0);
                    }

                    sideDoors.OpenDoors();
                    DoorOpening(this, EventArgs.Empty);
                }
            }

            return MethodOverrideMode.SkipOriginal;
        }

        public void RequestFixStopPosition()
        {
            FixStopPositionRequested(this, EventArgs.Empty);
        }

        public void OpenDoors(DoorSide doorSide)
        {
            DoorSwitch doorSwitch = DoorSwitches[(int)doorSide];
            doorSwitch.IsSwitchOn = true;
        }

        public void CloseDoors(DoorSide doorSide)
        {
            DoorSwitch doorSwitch = DoorSwitches[(int)doorSide];
            doorSwitch.IsSwitchOn = false;
        }

        private int ToDoorSideNumber(DoorSide doorSide) => (int)doorSide * 2 - 1;
    }
}
