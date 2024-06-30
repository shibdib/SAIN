using EFT.Weather;
using UnityEngine;

namespace SAIN.Components.BotController
{
    public class SAINWeatherClass : SAINControl
    {
        public static SAINWeatherClass Instance { get; private set; }

        public SAINWeatherClass(SAINBotController botController) : base(botController)
        {
            Instance = this;
        }

        public readonly float UpdateWeatherVisibilitySec = 20f;

        public void Update()
        {
            if (_getModifierTime < Time.time)
            {
                _getModifierTime = Time.time + UpdateWeatherVisibilitySec;
                VisionDistanceModifier = CalcWeatherVisibility();
                GainSightModifier = 2f - VisionDistanceModifier;
            }
        }

        public float VisionDistanceModifier { get; private set; }

        public float GainSightModifier { get; private set; }

        private float _getModifierTime = 0f;

        public float RainSoundModifier
        {
            get
            {
                if (WeatherController.Instance?.WeatherCurve == null)
                    return 1f;

                if (_rainCheckTime < Time.time)
                {
                    _rainCheckTime = Time.time + 5f;
                    // Grabs the current rain Rounding
                    float Rain = WeatherController.Instance.WeatherCurve.Rain;
                    _rainSoundMod = 1f;
                    float max = 1f;
                    float rainMin = 0.65f;

                    Rain = InverseScaling(Rain, rainMin, max);

                    // Combines ModifiersClass and returns
                    _rainSoundMod *= Rain;
                }
                return _rainSoundMod;
            }
        }

        private float _rainCheckTime = 0f;
        private float _rainSoundMod;

        private static float CalcWeatherVisibility()
        {
            if (WeatherController.Instance?.WeatherCurve == null)
            {
                return 1f;
            }

            IWeatherCurve weatherCurve = WeatherController.Instance.WeatherCurve;

            float fogmod = FogModifier(weatherCurve.Fog);
            float rainmod = RainModifier(weatherCurve.Rain);
            float cloudsmod = CloudsModifier(weatherCurve.Cloudiness);

            // Combines ModifiersClass
            float weathermodifier = 1f * fogmod * rainmod * cloudsmod;
            weathermodifier = Mathf.Clamp(weathermodifier, 0.2f, 1f);

            if (GameWorldComponent.Instance.WinterActive)
            {
                weathermodifier = Mathf.Clamp(weathermodifier, 0.35f, 1f);
                Logger.LogWarning("Snow Active");
            }

            return weathermodifier;
        }

        private static float FogModifier(float Fog)
        {
            // Points where fog values actually matter. Anything over 0.018 has little to no effect
            float fogMax = 0.018f;
            float fogValue = Mathf.Clamp(Fog, 0f, fogMax);

            // scales from 0 to 1 instead of 0 to 0.018
            fogValue /= fogMax;

            // Fog Tiers
            float fogScaleMin;
            // Very Light Fog
            /*
            if (fogValue <= 0.1f)
            {
                fogScaleMin = 0.75f;
            }
            else
            {
                // Light Fog
                if (fogValue < 0.35f)
                {
                    fogScaleMin = 0.5f;
                }
                else
                {
                    // Normal Fog
                    if (fogValue < 0.5f)
                    {
                        fogScaleMin = 0.4f;
                    }
                    else
                    {
                        // Heavy Fog
                        if (fogValue < 0.75f)
                        {
                            fogScaleMin = 0.35f;
                        }
                        // I can't see shit
                        else
                        {
                            fogScaleMin = 0.25f;
                        }
                    }
                }
            }
            */

            fogScaleMin = 0.2f;

            float fogModifier = InverseScaling(fogValue, fogScaleMin, 1f);

            return fogModifier;
        }

        private static float RainModifier(float Rain)
        {
            // Rain Tiers
            float rainScaleMin;
            // Sprinkling
            if (Rain <= 0.1f)
            {
                rainScaleMin = 0.95f;
            }
            else
            {
                // Light Rain
                if (Rain < 0.35f)
                {
                    rainScaleMin = 0.65f;
                }
                else
                {
                    // Normal Rain
                    if (Rain < 0.5f)
                    {
                        rainScaleMin = 0.5f;
                    }
                    else
                    {
                        // Heavy Rain
                        if (Rain < 0.75f)
                        {
                            rainScaleMin = 0.45f;
                        }
                        // Downpour
                        else
                        {
                            rainScaleMin = 0.4f;
                        }
                    }
                }
            }

            // Scales rain modifier depending on strength of rain found above
            float rainModifier = InverseScaling(Rain, rainScaleMin, 1f);

            return rainModifier;
        }

        private static float CloudsModifier(float Clouds)
        {
            // Clouds Rounding usually scales between -1 and 1, this sets it to scale between 0 and 1
            float cloudsScaled = (Clouds + 1f) / 2f;

            // Cloudiness Tiers
            float minScaledClouds;
            // Scattered Clouds
            if (cloudsScaled <= 0.33f)
            {
                minScaledClouds = 1f;
            }
            else
            {
                // Cloudy
                if (cloudsScaled <= 0.66)
                {
                    minScaledClouds = 0.75f;
                }
                // Heavy Overcast
                else
                {
                    minScaledClouds = 0.5f;
                }
            }

            float cloudsModifier = InverseScaling(Clouds, minScaledClouds, 1f);

            return cloudsModifier;
        }

        private static float InverseScaling(float value, float min, float max)
        {
            // Inverse
            float InverseValue = 1f - value;

            // Scaling
            float ScaledValue = (InverseValue * (max - min)) + min;

            value = ScaledValue;

            return value;
        }
    }
}