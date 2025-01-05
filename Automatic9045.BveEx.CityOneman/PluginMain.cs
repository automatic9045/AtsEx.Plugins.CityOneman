using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using BveTypes.ClassWrappers;

using BveEx.PluginHost;
using BveEx.PluginHost.Plugins;

using BveEx.Extensions.ConductorPatch;
using BveEx.Extensions.Native;
using BveEx.Extensions.SoundFactory;

namespace Automatic9045.BveEx.CityOneman
{
    [Plugin(PluginType.VehiclePlugin)]
    public class PluginMain : AssemblyPluginBase
    {
        private readonly Data.Config Config;
        private readonly INative Native;
        private readonly BeaconObserver BeaconObserver;
        private readonly ConductorHost ConductorHost;

        private readonly Sound DoorSwitchOnSound = null;
        private readonly Sound DoorSwitchOffSound = null;

        private readonly AssistantText AssistantText = null;

        private ConductorMode ModePanelValue
        {
            set { if (0 < Config.Vehicle.AtsPanelValues.Mode.Index) Native.AtsPanelArray[Config.Vehicle.AtsPanelValues.Mode.Index] = (int)value; }
        }

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
            Native = Extensions.GetExtension<INative>();

            BveHacker.ScenarioCreated += OnScenarioCreated;
            BveHacker.MainFormSource.KeyDown += OnKeyDown;
            BveHacker.MainFormSource.KeyUp += OnKeyUp;

            BeaconObserver = new BeaconObserver(Native, Config.Map.Beacons.ChangeEnabledBeacon.TypeNumber, Config.Vehicle.IsEnabledByDefault);
            IConductorPatchFactory conductorPatchFactory = Extensions.GetExtension<IConductorPatchFactory>();
            ConductorHost = new ConductorHost(BeaconObserver, BveHacker, conductorPatchFactory);

            ISoundFactory soundFactory = Extensions.GetExtension<ISoundFactory>();
            DoorSwitchOnSound = TryLoadFrom(Config.Vehicle.Sounds.DoorSwitchOn.Path);
            DoorSwitchOffSound = TryLoadFrom(Config.Vehicle.Sounds.DoorSwitchOff.Path);

            if (Config.ShowDebugLabel)
            {
                AssistantText = new AssistantText(new Mackoy.Bvets.AssistantSettings()
                {
                    Scale = 40,
                });

                BveHacker.Assistants.Items.Add(AssistantText);
            }


            Sound TryLoadFrom(string path)
            {
                if (string.IsNullOrWhiteSpace(path)) return null;

                Sound sound = soundFactory.LoadFrom(Path.Combine(Data.Config.BaseDirectory, path), 1, Sound.SoundPosition.Cab);
                return sound;
            }
        }

        public override void Dispose()
        {
            BveHacker.ScenarioCreated -= OnScenarioCreated;
            BveHacker.MainFormSource.KeyDown -= OnKeyDown;

            BeaconObserver.Dispose();

            DoorSwitchOnSound?.Dispose();
            DoorSwitchOffSound?.Dispose();

            if (!(AssistantText is null)) BveHacker.Assistants.Items.Remove(AssistantText);
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
                    DoorSwitchOnSound?.Play(1, 1, 0);
                    IsLeftOpenButtonPushed = true;
                }
                else if (e.KeyCode == keys.LeftClose.KeyCode && !IsLeftCloseButtonPushed)
                {
                    ConductorHost.Conductor.CloseDoors(DoorSide.Left);
                    DoorSwitchOnSound?.Play(1, 1, 0);
                    IsLeftCloseButtonPushed = true;
                }
                else if (e.KeyCode == keys.LeftReopen.KeyCode && !IsLeftReopenButtonPushed)
                {
                    ConductorHost.Conductor.IsLeftReopening = true;
                    DoorSwitchOnSound?.Play(1, 1, 0);
                    IsLeftReopenButtonPushed = true;
                }
                else if (e.KeyCode == keys.RightOpen.KeyCode && !IsRightOpenButtonPushed)
                {
                    ConductorHost.Conductor.OpenDoors(DoorSide.Right);
                    DoorSwitchOnSound?.Play(1, 1, 0);
                    IsRightOpenButtonPushed = true;
                }
                else if (e.KeyCode == keys.RightClose.KeyCode && !IsRightCloseButtonPushed)
                {
                    ConductorHost.Conductor.CloseDoors(DoorSide.Right);
                    DoorSwitchOnSound?.Play(1, 1, 0);
                    IsRightCloseButtonPushed = true;
                }
                else if (e.KeyCode == keys.RightReopen.KeyCode && !IsRightReopenButtonPushed)
                {
                    ConductorHost.Conductor.IsRightReopening = true;
                    DoorSwitchOnSound?.Play(1, 1, 0);
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
                    DoorSwitchOffSound?.Play(1, 1, 0);
                    IsLeftOpenButtonPushed = false;
                }
                else if (e.KeyCode == keys.LeftClose.KeyCode && IsLeftCloseButtonPushed)
                {
                    DoorSwitchOffSound?.Play(1, 1, 0);
                    IsLeftCloseButtonPushed = false;
                }
                else if (e.KeyCode == keys.LeftReopen.KeyCode && IsLeftReopenButtonPushed)
                {
                    ConductorHost.Conductor.IsLeftReopening = false;
                    DoorSwitchOffSound?.Play(1, 1, 0);
                    IsLeftReopenButtonPushed = false;
                }
                else if (e.KeyCode == keys.RightOpen.KeyCode && IsRightOpenButtonPushed)
                {
                    DoorSwitchOffSound?.Play(1, 1, 0);
                    IsRightOpenButtonPushed = false;
                }
                else if (e.KeyCode == keys.RightClose.KeyCode && IsRightCloseButtonPushed)
                {
                    DoorSwitchOffSound?.Play(1, 1, 0);
                    IsRightCloseButtonPushed = false;
                }
                else if (e.KeyCode == keys.RightReopen.KeyCode && IsRightReopenButtonPushed)
                {
                    ConductorHost.Conductor.IsRightReopening = false;
                    DoorSwitchOffSound?.Play(1, 1, 0);
                    IsRightReopenButtonPushed = false;
                }
            }
        }

        public override void Tick(TimeSpan elapsed)
        {
            ConductorHost.Tick();
            if (ConductorHost.IsEnabled)
            {
                ConductorValve.Tick(BveHacker.Scenario.VehicleLocation.Speed);
            }

            ModePanelValue = ConductorHost.Mode;

            if (!(AssistantText is null))
            {
                var text = GetText();
                AssistantText.Text = text.Text;
                AssistantText.Color = text.Color;
                AssistantText.BackgroundColor = text.BackgroundColor;
            }


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
