using EFT;
using HarmonyLib;
using UnityEngine;
using WeatherInterface = GInterface26;

namespace SAIN.Components
{
    public class LocationClass : GameWorldBase, IGameWorldClass
    {
        private const string WEATHER_INTERFACE = "ginterface26_0";
        public bool WinterActive => Season == ESeason.Winter;
        public ESeason Season { get; private set; }
        public ELocation Location { get; private set; }

        public LocationClass(GameWorldComponent component) : base(component)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            findLocation();
            findWeather();
        }

        public void Dispose()
        {
        }

        private void findWeather()
        {
            if (_weatherFound) {
                return;
            }
            if (_nextCheckWeatherTime > Time.time) {
                return;
            }
            _nextCheckWeatherTime = Time.time + 0.5f;

            var weather = (AccessTools.Field(typeof(GameWorld), WEATHER_INTERFACE).GetValue(GameWorld) as WeatherInterface);
            if (weather == null) {
                Season = ESeason.Summer;
            }
            else {
                Logger.LogDebug($"Got Weather {weather.Status}");
                Season = weather.Season;
                _weatherFound = true;
            }
        }

        private void findLocation()
        {
            if (!_foundLocation) {
                Location = parseLocation();
            }
        }

        private ELocation parseLocation()
        {
            ELocation Location = ELocation.None;
            string locationString = GameWorld.GameWorld?.LocationId;
            if (locationString.IsNullOrEmpty()) {
                return Location;
            }

            switch (locationString.ToLower()) {
                case "bigmap":
                    Location = ELocation.Customs;
                    break;

                case "factory4_day":
                    Location = ELocation.Factory;
                    break;

                case "factory4_night":
                    Location = ELocation.FactoryNight;
                    break;

                case "interchange":
                    Location = ELocation.Interchange;
                    break;

                case "laboratory":
                    Location = ELocation.Labs;
                    break;

                case "lighthouse":
                    Location = ELocation.Lighthouse;
                    break;

                case "rezervbase":
                    Location = ELocation.Reserve;
                    break;

                case "sandbox":
                    Location = ELocation.GroundZero;
                    break;

                case "shoreline":
                    Location = ELocation.Shoreline;
                    break;

                case "tarkovstreets":
                    Location = ELocation.Streets;
                    break;

                case "terminal":
                    Location = ELocation.Terminal;
                    break;

                case "town":
                    Location = ELocation.Town;
                    break;

                default:
                    Location = ELocation.None;
                    break;
            }

            _foundLocation = true;
            return Location;
        }

        private bool _weatherFound;
        private float _nextCheckWeatherTime;
        private bool _foundLocation;
    }
}