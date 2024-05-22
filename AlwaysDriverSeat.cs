using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Always Driver Seat", "VisEntities", "1.0.0")]
    [Description("Automatically moves players to the driver seat when they mount certain vehicles.")]
    public class AlwaysDriverSeat : RustPlugin
    {
        #region Fields

        private static AlwaysDriverSeat _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Vehicle Short Prefab Names")]
            public List<string> VehicleShortPrefabNames { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                VehicleShortPrefabNames = new List<string>
                {
                    "rowboat",
                    "rhib",
                    "minicopter.entity",
                    "scraptransporthelicopter",
                    "attackhelicopter.entity",
                }
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
        }

        private void Unload()
        {
            _config = null;
            _plugin = null;
        }

        private void OnEntityMounted(BaseMountable mountable, BasePlayer player)
        {
            if (player == null)
                return;

            BaseVehicle vehicle = mountable.GetParentEntity() as BaseVehicle;
            if (vehicle == null)
                return;

            if (vehicle.IsDriver(player))
                return;

            if (!_config.VehicleShortPrefabNames.Contains(vehicle.ShortPrefabName))
                return;

            var (driverSeat, driverSeatIndex) = GetDriverSeat(vehicle);
            if (driverSeat == null || driverSeatIndex == -1)
                return;

            driverSeat.mountable.MountPlayer(player);
        }

        #endregion Oxide Hooks

        #region Helper Functions

        private (BaseVehicle.MountPointInfo, int) GetDriverSeat(BaseVehicle vehicle)
        {
            if (vehicle == null)
                return (null, -1);

            int index = 0;
            foreach (var mountPoint in vehicle.allMountPoints)
            {
                if (mountPoint.isDriver && mountPoint.mountable != null)
                {
                    return (mountPoint, index);
                }
                index++;
            }
            return (null, -1);
        }

        #endregion Helper Functions
    }
}