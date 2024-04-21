using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BveTypes.ClassWrappers;
using FastMember;
using ObjectiveHarmonyPatch;
using TypeWrapping;

using AtsEx.PluginHost;
using AtsEx.PluginHost.Native;

using AtsEx.Extensions.ConductorPatch;

namespace Automatic9045.AtsEx.CityOneman
{
    internal class ConductorHost : IDisposable
    {
        private readonly BeaconObserver BeaconObserver;
        private readonly IBveHacker BveHacker;
        private readonly IConductorPatchFactory ConductorPatchFactory;

        private readonly HarmonyPatch OnJumpedPatch;

        private bool IsFirstFrame = true;
        private bool IsCalledByMyself = false;

        private StationListEx StationListEx;

        public ManualConductor Conductor { get; private set; } = null;
        private ConductorPatch Patch = null;

        private readonly Lazy<EmptyConductor> EmptyConductor;
        private ConductorPatch EmptyConductorPatch = null;

        private bool IsReadyToChangeMode = true;
        public bool IsEnabled { get; private set; }
        public ConductorMode Mode
        {
            get
            {
                return IsEnabled
                    ? BeaconObserver.IsEnabled ? ConductorMode.Oneman : ConductorMode.TwomanAtNextStation
                    : BeaconObserver.IsEnabled ? ConductorMode.OnemanAtNextStation : ConductorMode.Twoman;
            }
        }

        public ConductorHost(BeaconObserver beaconObserver, IBveHacker bveHacker, IConductorPatchFactory conductorPatchFactory)
        {
            BeaconObserver = beaconObserver;
            BveHacker = bveHacker;
            ConductorPatchFactory = conductorPatchFactory;

            ClassMemberSet conductorMembers = BveHacker.BveTypes.GetClassInfoOf<Conductor>();
            FastMethod initializeMethod = conductorMembers.GetSourceMethodOf(nameof(BveTypes.ClassWrappers.Conductor.OnJumped));
            OnJumpedPatch = HarmonyPatch.Patch(nameof(CityOneman), initializeMethod.Source, PatchType.Prefix);
            OnJumpedPatch.Invoked += (sender, e) =>
            {
                if (IsCalledByMyself) return PatchInvokationResult.DoNothing(e);

                if (IsReadyToChangeMode && BveHacker.IsScenarioCreated)
                {
                    int index = (int)e.Args[0];
                    Station station = BveHacker.Scenario.Route.Stations[index] as Station;
                    if (!station.Pass && StationListEx.IsNearestStation(index))
                    {
                        ChangeMode();
                    }
                }

                return PatchInvokationResult.DoNothing(e);
            };

            BeaconObserver.EnabledChanged += OnBeaconObserverEnabledChanged;
            BveHacker.ScenarioCreated += OnScenarioCreated;

            EmptyConductor = new Lazy<EmptyConductor>(() =>
            {
                Conductor original = BveHacker.Scenario.Vehicle.Conductor;
                return new EmptyConductor(original);
            });

            ConductorPatchFactory.BeginPatch(
                originalConductor =>
                {
                    Conductor = new ManualConductor(originalConductor);
                    return Conductor;
                },
                DeclarationPriority.Sequentially,
                patch => Patch = patch);
        }

        public void Dispose()
        {
            OnJumpedPatch.Dispose();

            if (!(Patch is null)) ConductorPatchFactory.Unpatch(Patch);
            if (!(EmptyConductorPatch is null)) ConductorPatchFactory.Unpatch(EmptyConductorPatch);
        }

        private void OnBeaconObserverEnabledChanged(object sender, EventArgs e)
        {
            IsReadyToChangeMode = true;
        }

        private void OnScenarioCreated(ScenarioCreatedEventArgs e)
        {
            if (Conductor is null)
            {
                Conductor originalConductor = e.Scenario.Vehicle.Conductor;

                StationListEx = new StationListEx(originalConductor.Stations, () => originalConductor.LocationManager.Location);
                Conductor = new ManualConductor(originalConductor);
                Patch = ConductorPatchFactory.Patch(Conductor);
            }
        }

        public void Tick()
        {
            if (IsFirstFrame)
            {
                IsFirstFrame = false;
                ChangeMode();
                return;
            }

            (int index, Station station) = StationListEx.GetStation(1);
            if (IsReadyToChangeMode && !station.Pass && StationListEx.IsNearestStation(index) && BveHacker.Scenario.Vehicle.Doors.GetSide((DoorSide)station.DoorSideEnum).IsOpen)
            {
                ChangeMode();
            }
        }

        private void ChangeMode()
        {
            if (BeaconObserver.IsEnabled)
            {
                if (!(EmptyConductorPatch is null))
                {
                    ConductorPatchFactory.Unpatch(EmptyConductorPatch);
                    EmptyConductorPatch = null;
                }

                Conductor.Sync();
            }
            else
            {
                (int nextStationIndex, Station nextStation) = Conductor.Desync();
                EmptyConductorPatch = ConductorPatchFactory.Patch(EmptyConductor.Value, DeclarationPriority.Sequentially);

                Vehicle vehicle = BveHacker.Scenario.Vehicle;
                if (nextStationIndex != -1)
                {
                    IsCalledByMyself = true;
                    if (vehicle.Doors.AreAllClosingOrClosed)
                    {
                        vehicle.Conductor.OnJumped(nextStationIndex - 1, true);
                    }
                    else
                    {
                        vehicle.Conductor.OnJumped(nextStationIndex, false);
                    }
                    IsCalledByMyself = false;
                }
            }

            IsEnabled = BeaconObserver.IsEnabled;
            IsReadyToChangeMode = false;
        }
    }
}
