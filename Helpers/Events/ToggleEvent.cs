using System;
using UnityEngine;

namespace SAIN.Helpers.Events
{
    public abstract class ToggleEventBase
    {
        public bool Value { get; protected set; }
        public ToggleEventBase(bool defaultValue) => SetValue(defaultValue);
        protected virtual void SetValue(bool value) => Value = value;
    }

    public abstract class ToggleEventTimeTrackBase : ToggleEventBase
    {
        public ToggleEventTimeTrackBase(bool defaultValue) : base(defaultValue) { }

        public float TimeLastTrue { get; private set; }
        public float TimeLastFalse { get; private set; }
        public float TimeSinceTrue => Time.time - TimeLastTrue;
        public float TimeSinceFalse => Time.time - TimeLastFalse;

        protected override void SetValue(bool value)
        {
            if (value)
                TimeLastTrue = Time.time;
            else
                TimeLastFalse = Time.time;

            base.SetValue(value);
        }
    }

    public class ToggleEventForObjectTimeTracked<T> : ToggleEventTimeTrackBase
    {
        public event Action<bool, T> OnToggle;

        public void CheckToggle(bool value)
        {
            if (Value != value)
            {
                base.SetValue(value);
                OnToggle?.Invoke(value, Object);
            }
        }

        private readonly T Object;

        public ToggleEventForObjectTimeTracked(T _object, bool defaultValue = false) : base(defaultValue)
        {
            Object = _object;
        }
    }

    public class ToggleEventForObject<T> : ToggleEventBase
    {
        public event Action<bool, T> OnToggle;

        public bool CheckToggle(bool value)
        {
            if (Value != value)
            {
                base.SetValue(value);
                OnToggle?.Invoke(value, Object);
                return true;
            }
            return false;
        }

        private readonly T Object;

        public ToggleEventForObject(T _object, bool defaultValue = false) : base(defaultValue)
        {
            Object = _object;
        }
    }

    public class ToggleEvent : ToggleEventBase
    {
        public event Action<bool> OnToggle;

        public void CheckToggle(bool value)
        {
            if (Value != value)
            {
                base.SetValue(value);
                OnToggle?.Invoke(value);
            }
        }

        public ToggleEvent(bool defaultValue = false) : base(defaultValue) { }
    }

    public class ToggleEventTimeTracked : ToggleEventTimeTrackBase
    {
        public event Action<bool> OnToggle;

        public void CheckToggle(bool value)
        {
            if (Value != value)
            {
                base.SetValue(value);
                OnToggle?.Invoke(value);
            }
        }

        public ToggleEventTimeTracked(bool defaultValue = false) : base(defaultValue) { }
    }

    public class ToggleEvent<A> : ToggleEventBase
    {
        public event Action<bool, A> OnToggle;

        public void Toggle(bool value, A a)
        {
            if (Value != value)
            {
                base.SetValue(value);
                OnToggle?.Invoke(value, a);
            }
        }

        public ToggleEvent(bool defaultValue = false) : base (defaultValue) { }
    }

    public class ToggleEvent<A, B> : ToggleEventBase
    {
        public event Action<bool, A, B> OnToggle;

        public void Toggle(bool value, A a, B b)
        {
            if (Value != value)
            {
                base.SetValue(value);
                OnToggle?.Invoke(value, a, b);
            }
        }

        public ToggleEvent(bool defaultValue = false) : base(defaultValue) { }
    }

    public class ToggleEvent<A, B, C> : ToggleEventBase
    {
        public event Action<bool, A, B, C> OnToggle;

        public void Toggle(bool value, A a, B b, C c)
        {
            if (Value != value)
            {
                base.SetValue(value);
                OnToggle?.Invoke(value, a, b, c);
            }
        }

        public ToggleEvent(bool defaultValue = false) : base(defaultValue) { }
    }
}
