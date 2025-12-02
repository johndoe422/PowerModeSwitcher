using System;
using System.Xml.Linq;
using System.Xml;
using System.IO;
using System.Linq;

namespace PowerModes
{
    public static class ConfigManager
    {
        private static readonly string configPath = Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
            System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + ".exe.config"
        );

        /// <summary>
        /// Gets whether power mode auto-switching is enabled
        /// </summary>
        public static bool IsAutoSwitchEnabled
        {
            get { return GetAppSetting("AutoSwitchEnabled", false); }
            set { SetAppSetting("AutoSwitchEnabled", value.ToString()); }
        }

        /// <summary>
        /// Gets the power plan GUID to switch to when computer is idle
        /// </summary>
        public static string IdlePowerPlan
        {
            get { return GetAppSetting("IdlePowerPlan", ""); }
            set { SetAppSetting("IdlePowerPlan", value); }
        }

        /// <summary>
        /// Gets the power plan GUID to switch to when computer is in active use
        /// </summary>
        public static string ActiveUsePowerPlan
        {
            get { return GetAppSetting("ActiveUsePowerPlan", ""); }
            set { SetAppSetting("ActiveUsePowerPlan", value); }
        }

        /// <summary>
        /// Gets whether to switch power plan when system is locked
        /// </summary>
        public static bool SwitchWhenLocked
        {
            get { return GetAppSetting("SwitchWhenLocked", false); }
            set { SetAppSetting("SwitchWhenLocked", value.ToString()); }
        }

        /// <summary>
        /// Gets the idle timeout in minutes before switching to idle power plan
        /// </summary>
        public static int IdleTimeoutMinutes
        {
            get { return GetAppSetting("IdleTimeoutMinutes", 5); }
            set { SetAppSetting("IdleTimeoutMinutes", value.ToString()); }
        }

        /// <summary>
        /// Gets an application setting from app.config
        /// </summary>
        private static string GetAppSetting(string key, string defaultValue)
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    Logger.Warning($"Config file not found at {configPath}");
                    return defaultValue;
                }

                var config = XDocument.Load(configPath);
                var root = config.Root;
                var appSettingsElement = root.Element("appSettings");

                if (appSettingsElement == null)
                    return defaultValue;

                var setting = appSettingsElement.Descendants("add")
                    .FirstOrDefault(e => e.Attribute("key")?.Value == key);

                if (setting != null && setting.Attribute("value") != null)
                    return setting.Attribute("value").Value;

                return defaultValue;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error reading config setting '{key}'", ex);
                return defaultValue;
            }
        }

        /// <summary>
        /// Gets a boolean application setting from app.config
        /// </summary>
        private static bool GetAppSetting(string key, bool defaultValue)
        {
            try
            {
                string value = GetAppSetting(key, "");
                if (string.IsNullOrEmpty(value))
                    return defaultValue;

                if (bool.TryParse(value, out bool result))
                    return result;

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Gets an integer application setting from app.config
        /// </summary>
        private static int GetAppSetting(string key, int defaultValue)
        {
            try
            {
                string value = GetAppSetting(key, "");
                if (string.IsNullOrEmpty(value))
                    return defaultValue;

                if (int.TryParse(value, out int result))
                    return result;

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Sets an application setting in app.config
        /// </summary>
        private static void SetAppSetting(string key, string value)
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    Logger.Warning($"Config file not found at {configPath}");
                    return;
                }

                var config = XDocument.Load(configPath);
                var root = config.Root;

                // Find or create appSettings section
                var appSettingsElement = root.Element("appSettings");
                if (appSettingsElement == null)
                {
                    appSettingsElement = new XElement("appSettings");
                    root.Add(appSettingsElement);
                }

                // Find or create the setting
                var settingElement = appSettingsElement.Descendants("add")
                    .FirstOrDefault(e => e.Attribute("key")?.Value == key);

                if (settingElement != null)
                {
                    settingElement.Attribute("value").Value = value;
                }
                else
                {
                    appSettingsElement.Add(new XElement("add",
                        new XAttribute("key", key),
                        new XAttribute("value", value)));
                }

                // Save with proper formatting
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "    ",
                    NewLineChars = "\r\n",
                    Encoding = System.Text.Encoding.UTF8
                };

                using (var writer = XmlWriter.Create(configPath, settings))
                {
                    config.Save(writer);
                }

                Logger.Info($"Config setting '{key}' updated to '{value}'");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error setting config value for key '{key}'", ex);
            }
        }

        /// <summary>
        /// Removes a setting from the config
        /// </summary>
        public static void RemoveSetting(string key)
        {
            try
            {
                if (!File.Exists(configPath))
                    return;

                var config = XDocument.Load(configPath);
                var root = config.Root;
                var appSettingsElement = root.Element("appSettings");

                if (appSettingsElement != null)
                {
                    var settingElement = appSettingsElement.Descendants("add")
                        .FirstOrDefault(e => e.Attribute("key")?.Value == key);

                    if (settingElement != null)
                    {
                        settingElement.Remove();
                        var settings = new XmlWriterSettings
                        {
                            Indent = true,
                            IndentChars = "    ",
                            NewLineChars = "\r\n",
                            Encoding = System.Text.Encoding.UTF8
                        };

                        using (var writer = XmlWriter.Create(configPath, settings))
                        {
                            config.Save(writer);
                        }
                        Logger.Info($"Config setting '{key}' removed");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error removing config setting '{key}'", ex);
            }
        }

        /// <summary>
        /// Validates and corrects power plan configuration if plans don't exist
        /// </summary>
        public static void ValidateAndCorrectPowerPlanConfig(System.Collections.Generic.List<MainForm.PowerPlan> availablePlans)
        {
            Logger.Info("Validating power plan configuration...");

            bool configChanged = false;
            string idlePlan = IdlePowerPlan;
            string activePlan = ActiveUsePowerPlan;

            // Check if idle plan exists
            if (!string.IsNullOrEmpty(idlePlan))
            {
                if (!PlanExists(availablePlans, idlePlan))
                {
                    Logger.Warning($"Idle power plan '{idlePlan}' not found. Removing from config.");
                    idlePlan = "";
                    configChanged = true;
                }
            }

            // Check if active use plan exists
            if (!string.IsNullOrEmpty(activePlan))
            {
                if (!PlanExists(availablePlans, activePlan))
                {
                    Logger.Warning($"Active use power plan '{activePlan}' not found. Removing from config.");
                    activePlan = "";
                    configChanged = true;
                }
            }

            // Set defaults for missing idle plan
            if (string.IsNullOrEmpty(idlePlan))
            {
                idlePlan = FindPlanByName(availablePlans, "Balanced");
                if (string.IsNullOrEmpty(idlePlan) && availablePlans.Count > 0)
                {
                    idlePlan = availablePlans[0].Guid.ToString();
                }

                if (!string.IsNullOrEmpty(idlePlan))
                {
                    Logger.Info($"Setting idle power plan to: {idlePlan}");
                    IdlePowerPlan = idlePlan;
                    configChanged = true;
                }
            }

            // Set defaults for missing active use plan
            if (string.IsNullOrEmpty(activePlan))
            {
                activePlan = FindPlanByName(availablePlans, "High Performance");
                if (string.IsNullOrEmpty(activePlan))
                {
                    activePlan = FindPlanByName(availablePlans, "Balanced");
                }
                if (string.IsNullOrEmpty(activePlan) && availablePlans.Count > 0)
                {
                    activePlan = availablePlans[0].Guid.ToString();
                }

                if (!string.IsNullOrEmpty(activePlan))
                {
                    Logger.Info($"Setting active use power plan to: {activePlan}");
                    ActiveUsePowerPlan = activePlan;
                    configChanged = true;
                }
            }

            if (configChanged)
            {
                Logger.Info("Power plan configuration has been corrected");
            }
            else
            {
                Logger.Info("Power plan configuration is valid");
            }
        }

        /// <summary>
        /// Checks if a power plan exists in the available plans list
        /// </summary>
        private static bool PlanExists(System.Collections.Generic.List<MainForm.PowerPlan> plans, string planGuid)
        {
            if (!Guid.TryParse(planGuid, out Guid guid))
                return false;

            return plans.Any(p => p.Guid == guid);
        }

        /// <summary>
        /// Finds a power plan by name
        /// </summary>
        private static string FindPlanByName(System.Collections.Generic.List<MainForm.PowerPlan> plans, string planName)
        {
            var plan = plans.FirstOrDefault(p => p.Name.Equals(planName, StringComparison.OrdinalIgnoreCase));
            return plan?.Guid.ToString() ?? "";
        }
    }
}
