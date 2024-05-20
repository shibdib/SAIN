using EFT;
using SAIN.SAINComponent.BaseClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SAIN.Components
{
    public class SAINPlayerComponent : MonoBehaviour
    {
        private void Awake()
        {

        }

        private void Start()
        {

        }

        private void Update()
        {

        }

        public void ManualUpdate()
        {

        }

        public Vector3 LookPoint { get; private set; }

        public SAINPersonClass Person { get ; private set; }

        public Player Player { get; private set; }

        public SAINFlashLightComponent FlashlightComponent { get; private set; }
    }

    public class SAINAIData
    {

    }
}
