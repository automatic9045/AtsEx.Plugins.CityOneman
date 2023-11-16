using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using BveTypes.ClassWrappers;

using AtsEx.PluginHost;
using AtsEx.PluginHost.Panels.Native;
using AtsEx.PluginHost.Sound.Native;
using AtsEx.PluginHost.Plugins;

using AtsEx.Extensions.ConductorPatch;

namespace Automatic9045.AtsEx.CityOneman
{
    [PluginType(PluginType.VehiclePlugin)]
    public class PluginMain : AssemblyPluginBase
    {
        private readonly Data.Config Config;
        private readonly ConductorHost ConductorHost = null;

        private readonly IAtsPanelValue<ConductorMode> ModePanelValue = null;
        private readonly IAtsSound DoorSwitchOnSound = null;
        private readonly IAtsSound DoorSwitchOffSound = null;

        private readonly AssistantText AssistantText;

        private ConductorValve ConductorValve = null;

        private bool IsLeftOpenButtonPushed = false;
        private bool IsLeftCloseButtonPushed = false;
        private bool IsLeftReopenButtonPushed = false;
        private bool IsRightOpenButtonPushed = false;
        private bool IsRightCloseButtonPushed = false;
        private bool IsRightReopenButtonPushed = false;

        public PluginMain(PluginBuilder builder) : base(builder)
        {
            Config = Data.Config.Deserialize("CityOneman.Config.xml", false);

            BveHacker.ScenarioCreated += OnScenarioCreated;
            BveHacker.MainFormSource.KeyDown += OnKeyDown;
            BveHacker.MainFormSource.KeyUp += OnKeyUp;

            BeaconObserver beaconObserver = new BeaconObserver(Native, Config.Route.Beacons.ChangeEnabledBeacon.TypeNumber, Config.Vehicle.IsEnabledByDefault);
            IConductorPatchFactory conductorPatchFactory = Extensions.GetExtension<IConductorPatchFactory>();
            ConductorHost = new ConductorHost(beaconObserver, BveHacker, conductorPatchFactory);

            if (Config.Vehicle.AtsPanelValues.Mode.Index > 0) ModePanelValue = Native.AtsPanelValues.Register<ConductorMode>(Config.Vehicle.AtsPanelValues.Mode.Index, x => (int)x);
            if (Config.Vehicle.AtsSounds.DoorSwitchOn.Index > 0) DoorSwitchOnSound = Native.AtsSounds.Register(Config.Vehicle.AtsSounds.DoorSwitchOn.Index);
            if (Config.Vehicle.AtsSounds.DoorSwitchOff.Index > 0) DoorSwitchOffSound = Native.AtsSounds.Register(Config.Vehicle.AtsSounds.DoorSwitchOff.Index);

            if (Config.ShowDebugLabel)
            {
                AssistantText = new AssistantText(new Mackoy.Bvets.AssistantSettings()
                {
                    Scale = 40,
                });

                BveHacker.MainForm.AssistantDrawer.Items.Add(AssistantText);
            }
        }

        public override void Dispose()
        {
            BveHacker.ScenarioCreated -= OnScenarioCreated;
            BveHacker.MainFormSource.KeyDown -= OnKeyDown;
            if (!(AssistantText is null)) BveHacker.MainForm.AssistantDrawer.Items.Remove(AssistantText);
        }

        private void OnScenarioCreated(ScenarioCreatedEventArgs e)
        {
            ConductorValve = new ConductorValve(e.Scenario.Vehicle.Instruments.Cab.Handles);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (ConductorHost.IsEnabled)
            {
                Data.KeySet keys = Config.Vehicle.Keys;

                if (e.KeyCode == keys.LeftOpen.KeyCode && !IsLeftOpenButtonPushed)
                {
                    ConductorHost.Conductor.OpenDoors(DoorSide.Left);
                    DoorSwitchOnSound?.Play();
                    IsLeftOpenButtonPushed = true;
                }
                else if (e.KeyCode == keys.LeftClose.KeyCode && !IsLeftCloseButtonPushed)
                {
                    ConductorHost.Conductor.CloseDoors(DoorSide.Left);
                    DoorSwitchOnSound?.Play();
                    IsLeftCloseButtonPushed = true;
                }
                else if (e.KeyCode == keys.LeftReopen.KeyCode && !IsLeftReopenButtonPushed)
                {
                    ConductorHost.Conductor.IsLeftReopening = true;
                    DoorSwitchOnSound?.Play();
                    IsLeftReopenButtonPushed = true;
                }
                else if (e.KeyCode == keys.RightOpen.KeyCode && !IsRightOpenButtonPushed)
                {
                    ConductorHost.Conductor.OpenDoors(DoorSide.Right);
                    DoorSwitchOnSound?.Play();
                    IsRightOpenButtonPushed = true;
                }
                else if (e.KeyCode == keys.RightClose.KeyCode && !IsRightCloseButtonPushed)
                {
                    ConductorHost.Conductor.CloseDoors(DoorSide.Right);
                    DoorSwitchOnSound?.Play();
                    IsRightCloseButtonPushed = true;
                }
                else if (e.KeyCode == keys.RightReopen.KeyCode && !IsRightReopenButtonPushed)
                {
                    ConductorHost.Conductor.IsRightReopening = true;
                    DoorSwitchOnSound?.Play();
                    IsRightReopenButtonPushed = true;
                }
                else if (e.KeyCode == keys.RequestFixStopPosition.KeyCode)
                {
                    ConductorHost.Conductor.RequestFixStopPosition();
                }
            }

            /*
            if (e.KeyCode == Config.Vehicle.Keys.ConductorValve.Code)
            {
                ConductorValve.Pull();
            }*/
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (ConductorHost.IsEnabled)
            {
                Data.KeySet keys = Config.Vehicle.Keys;

                if (e.KeyCode == keys.LeftOpen.KeyCode && IsLeftOpenButtonPushed)
                {
                    DoorSwitchOffSound?.Play();
                    IsLeftOpenButtonPushed = false;
                }
                else if (e.KeyCode == keys.LeftClose.KeyCode && IsLeftCloseButtonPushed)
                {
                    DoorSwitchOffSound?.Play();
                    IsLeftCloseButtonPushed = false;
                }
                else if (e.KeyCode == keys.LeftReopen.KeyCode && IsLeftReopenButtonPushed)
                {
                    ConductorHost.Conductor.IsLeftReopening = false;
                    DoorSwitchOffSound?.Play();
                    IsLeftReopenButtonPushed = false;
                }
                else if (e.KeyCode == keys.RightOpen.KeyCode && IsRightOpenButtonPushed)
                {
                    DoorSwitchOffSound?.Play();
                    IsRightOpenButtonPushed = false;
                }
                else if (e.KeyCode == keys.RightClose.KeyCode && IsRightCloseButtonPushed)
                {
                    DoorSwitchOffSound?.Play();
                    IsRightCloseButtonPushed = false;
                }
                else if (e.KeyCode == keys.RightReopen.KeyCode && IsRightReopenButtonPushed)
                {
                    ConductorHost.Conductor.IsRightReopening = false;
                    DoorSwitchOffSound?.Play();
                    IsRightReopenButtonPushed = false;
                }
            }
        }

        public override TickResult Tick(TimeSpan elapsed)
        {
            ConductorHost.Tick();
            if (ConductorHost.IsEnabled)
            {
                ConductorValve.Tick(BveHacker.Scenario.LocationManager.SpeedMeterPerSecond);
            }

            if (!(ModePanelValue is null)) ModePanelValue.Value = ConductorHost.Mode;

            if (!(AssistantText is null))
            {
                var text = GetText();
                AssistantText.Text = text.Text;
                AssistantText.Color = text.Color;
                AssistantText.BackgroundColor = text.BackgroundColor;
            }

            return new VehiclePluginTickResult();


            (string Text, System.Drawing.Color Color, System.Drawing.Color BackgroundColor) GetText()
            {
                switch (ConductorHost.Mode)
                {
                    case ConductorMode.Twoman:
                        return ("ツーマン", System.Drawing.Color.White, System.Drawing.Color.Blue);
                    case ConductorMode.TwomanAtNextStation:
                        return ("次駅からツーマン", System.Drawing.Color.Red, System.Drawing.Color.LightBlue);
                    case ConductorMode.Oneman:
                        return ("ワンマン", System.Drawing.Color.White, System.Drawing.Color.Green);
                    case ConductorMode.OnemanAtNextStation:
                        return ("次駅からワンマン", System.Drawing.Color.Red, System.Drawing.Color.LightGreen);
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}
