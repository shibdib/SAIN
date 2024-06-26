using HarmonyLib;
using SAIN.Editor;
using SAIN.Helpers;
using SAIN.Preset.BotSettings.SAINSettings;
using SAIN.Preset.GlobalSettings;
using System;
using System.Configuration;
using System.Linq;
using System.Reflection;

namespace SAIN.Attributes
{
    public sealed class AttributesInfoClass
    {
        public AttributesInfoClass(MemberInfo member)
        {
            MemberInfo = member;
            GetInfo(member);
        }

        public Type ValueType
        {
            get
            {
                switch (MemberInfo.MemberType)
                {
                    case MemberTypes.Field:
                        return (MemberInfo as FieldInfo).FieldType;

                    case MemberTypes.Property:
                        return (MemberInfo as PropertyInfo).PropertyType;

                    default:
                        return null;
                }
            }
        }

        public Type DeclaringType => MemberInfo.DeclaringType;

        public readonly MemberInfo MemberInfo;

        public object GetValue(object obj)
        {
            if (obj == null)
                return null;

            switch (MemberInfo.MemberType)
            {
                case MemberTypes.Field:
                    return (MemberInfo as FieldInfo).GetValue(obj);

                case MemberTypes.Property:
                    return (MemberInfo as PropertyInfo).GetValue(obj);

                default:
                    return null;
            }
        }

        public void SetValue(object obj, object value)
        {
            switch (MemberInfo.MemberType)
            {
                case MemberTypes.Field:
                    (MemberInfo as FieldInfo).SetValue(obj, value);
                    return;

                case MemberTypes.Property:
                    (MemberInfo as PropertyInfo).SetValue(obj, value);
                    return;

                default:
                    return;
            }
        }

        public object Clamp(object value)
        {
            return MathHelpers.ClampObject(value, Min, Max);
        }

        private void GetInfo(MemberInfo member)
        {
            Hidden = Get<HiddenAttribute>() != null;
            Advanced = Get<AdvancedAttribute>() != null;
            Debug = Get<DebugAttribute>() != null;
            CopyValue = Get<CopyValueAttribute>() != null;

            if (Hidden)
            {
                return;
            }

            var nameDescription = Get<NameAndDescriptionAttribute>();
            Name = nameDescription?.Name ?? Get<NameAttribute>()?.Value ?? member.Name;
            Description = nameDescription?.Description ?? Get<DescriptionAttribute>()?.Value ?? string.Empty;

            SetDefault(Get<DefaultAttribute>()?.Value);

            GUIValuesAttribute GUIValues = Get<GUIValuesAttribute>();
            if (GUIValues != null)
            {
                Min = GUIValues.Min;
                Max = GUIValues.Max;
                Rounding = GUIValues.Rounding;
            }

            DictionaryString = Get<DefaultDictionaryAttribute>()?.Value;
        }

        private T Get<T>() where T : Attribute
        {
            return MemberInfo.GetCustomAttribute<T>();
        }

        public object DefaultDictionary => Reflection.GetStaticValue(DeclaringType, DictionaryString);
        private string DictionaryString;


        public string Name { get; private set; }

        public string MemberName { get; private set; }

        public string Description { get; private set; }

        public object GetDefault(object settingsObject)
        {
            if (settingsObject is ISAINSettings settings)
            {
                var defaults = settings.GetDefaults();
                object value = GetValue(defaults);
                return value ?? _default;
            }
            return _default;
        }

        private void SetDefault(object value)
        {
            _default = value;
        }

        private object _default;

        public float Min { get; private set; } = 0f;

        public float Max { get; private set; } = 300f;

        public float Rounding { get; private set; } = 10f;

        public bool Hidden { get; private set; }

        public bool Advanced { get; private set; }
        public bool Debug { get; private set; }

        public bool CopyValue { get; private set; }

        public bool DoNotShowGUI => Hidden || (Advanced && !SAINEditor.AdvancedBotConfigs); // || (Debug && !SAINPlugin.DebugMode)

        public EListType EListType { get; private set; } = EListType.None;
        public Type ListType { get; private set; }
        public Type SecondaryListType { get; private set; }

        public bool MenuOpen;
    }
}