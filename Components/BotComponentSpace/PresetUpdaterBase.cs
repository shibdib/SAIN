using SAIN.Plugin;
using SAIN.Preset;
using System;

namespace SAIN.SAINComponent
{
    public abstract class PresetUpdaterBase
    {
        protected virtual void SubscribeToPresetChanges(Action<SAINPresetClass> func)
        {
            if (func == null)
            {
                return;
            }

            _func = func;
            func.Invoke(SAINPresetClass.Instance);
            PresetHandler.OnPresetUpdated += func;
        }

        private Action<SAINPresetClass> _func;

        protected virtual void UnSubscribeToPresetChanges()
        {
            if (_func == null)
            {
                return;
            }
            Logger.LogDebug($"UnSubed");
            PresetHandler.OnPresetUpdated -= _func;
        }
    }
}