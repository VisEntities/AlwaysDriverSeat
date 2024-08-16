/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Always Driver Seat", "VisEntities", "1.1.1")]
    [Description("Forces players into the driver's seat when they mount certain vehicles.")]
    public class AlwaysDriverSeat : RustPlugin
    {
        #region Fields

        private static AlwaysDriverSeat _plugin;
        private static Configuration _config;
        private Dictionary<ulong, BaseVehicle> _playersAndTheirCurrentVehicles = new Dictionary<ulong, BaseVehicle>();

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
            PermissionUtil.RegisterPermissions();
        }

        private void Unload()
        {
            _config = null;
            _plugin = null;
        }

        private void OnEntityMounted(BaseMountable mountable, BasePlayer player)
        {
            if (player == null || mountable == null)
                return;

            if (!PermissionUtil.HasPermission(player, PermissionUtil.USE))
                return;

            BaseVehicle vehicle = mountable.GetParentEntity() as BaseVehicle;
            if (vehicle == null)
                return;

            if (_playersAndTheirCurrentVehicles.TryGetValue(player.userID, out BaseVehicle lastVehicle) && lastVehicle == vehicle)
                return;

            _playersAndTheirCurrentVehicles[player.userID] = vehicle;

            if (vehicle.IsDriver(player))
                return;

            if (!_config.VehicleShortPrefabNames.Contains(vehicle.ShortPrefabName))
                return;

            var (driverSeat, driverSeatIndex) = GetDriverSeat(vehicle);
            if (driverSeat == null || driverSeatIndex == -1)
                return;

            driverSeat.mountable.MountPlayer(player);
        }

        private void OnEntityDismounted(BaseMountable mountable, BasePlayer player)
        {
            if (player == null || mountable == null)
                return;

            timer.Once(0.5f, () =>
            {
                if (player == null || mountable == null)
                    return;

                if (_playersAndTheirCurrentVehicles.ContainsKey(player.userID))
                {
                    BaseMountable mountedEntity = player.GetMounted();
                    BaseVehicle currentVehicle = null;

                    if (mountedEntity != null)
                        currentVehicle = mountable.GetParentEntity() as BaseVehicle;

                    if (currentVehicle == null || currentVehicle != _playersAndTheirCurrentVehicles[player.userID])
                        _playersAndTheirCurrentVehicles.Remove(player.userID);
                }
            });
        }

        #endregion Oxide Hooks

        #region Driver Seat Retrieval

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

        #endregion Driver Seat Retrieval

        #region Permissions

        private static class PermissionUtil
        {
            public const string USE = "alwaysdriverseat.use";
            private static readonly List<string> _permissions = new List<string>
            {
                USE,
            };

            public static void RegisterPermissions()
            {
                foreach (var permission in _permissions)
                {
                    _plugin.permission.RegisterPermission(permission, _plugin);
                }
            }

            public static bool HasPermission(BasePlayer player, string permissionName)
            {
                return _plugin.permission.UserHasPermission(player.UserIDString, permissionName);
            }
        }

        #endregion Permissions
    }
}