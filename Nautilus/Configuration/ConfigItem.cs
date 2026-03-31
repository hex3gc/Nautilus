using System;
using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.Options;
using RiskOfOptions.OptionConfigs;
using System.Collections.Generic;

namespace Nautilus.Configuration
{
    /// <summary>
    ///     Initializes a config entry and makes its value available
    /// </summary>
    public abstract class ConfigItem
    {
        private static List<ConfigItem> _configList;
        public static List<ConfigItem> ConfigList
        {
            get
            {
                if (_configList == null)
                {
                    _configList = new List<ConfigItem>();
                }
                return _configList;
            }
            set
            {
                _configList = value;
            }
        }
        public abstract void InitConfigItem();
        public static void Init()
        {
            foreach (ConfigItem cfg in ConfigList)
            {
                cfg.InitConfigItem();
            }
        }
    }

    public class ConfigItem<T> : ConfigItem
    {
        public T Value
        {
            get
            {
                if (Main.Config_Enabled.Value == true)
                {
                    return configEntry.Value;
                }
                else
                {
                    return defaultValue;
                }
            }
        }
        public Type Type
        {
            get
            {
                return typeof(T);
            }
        }
        private ConfigEntry<T> configEntry;
        private readonly string header;
        private readonly string name;
        private readonly string desc;
        private readonly T defaultValue;
        private readonly float minValue;
        private readonly float maxValue;
        private readonly float increment;
        
        public ConfigItem(string _header, string _name, string _desc, T _defaultValue, float _minValue = float.MinValue, float _maxValue = float.MinValue, float _increment = float.MinValue)
        {
            header = _header;
            name = _name;
            desc = _desc;
            defaultValue = _defaultValue;
            minValue = _minValue;
            maxValue = _maxValue;
            increment = _increment;

            ConfigList.Add(this);
        }

        public override void InitConfigItem()
        {
            configEntry = Main.Instance.Config.Bind(new ConfigDefinition(header, name), defaultValue, new ConfigDescription(desc));

            if (this.Type == typeof(bool))
            {
                ModSettingsManager.AddOption
                (
                    new CheckBoxOption
                    (
                        configEntry as ConfigEntry<bool>,
                        true
                    )
                );
            }
            else if (this.Type == typeof(string))
            {
                ModSettingsManager.AddOption
                (
                    new StringInputFieldOption
                    (
                        configEntry as ConfigEntry<string>,
                        true
                    )
                );
            }
            else if (this.Type == typeof(int))
            {
                int defaultValueInt = Convert.ToInt32(defaultValue);
                float minValueInt = minValue == float.MinValue ? 0.0f : minValue;
                float maxValueInt = maxValue == float.MinValue ? defaultValueInt * 10f : maxValue;

                ModSettingsManager.AddOption
                (
                    new IntSliderOption
                    (
                        configEntry as ConfigEntry<int>, 
                        new IntSliderConfig(){min = (int)minValueInt, max = (int)maxValueInt, restartRequired = true}
                    )
                );
            }
            else if (this.Type == typeof(float))
            {
                float defaultValueFloat = Convert.ToSingle(defaultValue);
                float minValueFloat = minValue == float.MinValue ? 0.0f : minValue;
                float maxValueFloat = maxValue == float.MinValue ? defaultValueFloat * 10f : maxValue;
                float incrementFloat = increment == float.MinValue ? defaultValueFloat / 10f : increment;

                ModSettingsManager.AddOption
                (
                    new StepSliderOption
                    (
                        configEntry as ConfigEntry<float>,
                        new StepSliderConfig(){min = minValueFloat, max = maxValueFloat, increment = incrementFloat, restartRequired = true}
                    )
                );
            }
            
        }
    }
}