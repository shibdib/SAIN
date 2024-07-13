using SAIN.Helpers;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyShootTargets : EnemyBase, IBotEnemyClass
    {
        public EnemyPartDataClass SelectedPart { get; private set; }
        public EnemyPartDataClass LastPart { get; private set; }
        public float TimeSinceChangedPart => Time.time - _lastChangePartTime;
        public bool CanShootHead { get; private set; }

        public EnemyShootTargets(Enemy enemy) : base(enemy)
        {
        }

        public void Init()
        {
            Enemy.Events.OnEnemyKnownChanged.OnToggle += OnEnemyKnownChanged;
            Enemy.Bot.Shoot.OnShootEnemy += checkChangePart;
            Enemy.Bot.Shoot.OnEndShoot += executePartChange;
            addEnemyParts();
            SubscribeToDispose(Dispose);
        }

        public void Update()
        {
        }

        public void Dispose()
        {
            Enemy.Events.OnEnemyKnownChanged.OnToggle -= OnEnemyKnownChanged;
            Enemy.Bot.Shoot.OnShootEnemy -= checkChangePart;
            Enemy.Bot.Shoot.OnEndShoot -= executePartChange;
        }

        public void OnEnemyKnownChanged(bool known, Enemy enemy)
        {
        }

        private void checkChangePart(Enemy enemy)
        {
            if (enemy != null && enemy.IsSame(Enemy) && _timeCanChange < Time.time) {
                _willChangePart = true;
                _timeCanChange = Time.time + MAX_CHANGE_FREQ;
            }
        }

        private float _timeCanChange;
        private const float MAX_CHANGE_FREQ = 0.5f;

        private void executePartChange()
        {
            if (_willChangePart) {
                _willChangePart = false;
                _changePart = true;
            }
        }

        private void checkChangePart()
        {
            if (SelectedPart != null &&
                !_changePart &&
                TimeSinceChangedPart < CHANGE_PART_FREQ) {
                return;
            }
            _changePart = false;
            changeSelectedPart();
        }

        private EnemyPartDataClass changeSelectedPart()
        {
            var enemyParts = Enemy.Vision.VisionChecker.EnemyParts.Parts;

            for (int i = 0; i < CHANGE_PART_ITERATION_ATTEMPTS; i++) {
                EBodyPart randomPart = _selector.GetRandomOption();
                if (enemyParts.TryGetValue(randomPart, out EnemyPartDataClass enemyPartData) &&
                    enemyPartData?.CanShoot == true &&
                    enemyPartData.LastSuccessShootPoint != null) {
                    if (SelectedPart != null &&
                        SelectedPart.BodyPart != randomPart) {
                        LastPart = SelectedPart;
                    }

                    if (!Enemy.IsAI)
                        Logger.LogDebug($"Selected [{randomPart}] body part to shoot after [{i}] iterations through random selector.");

                    SelectedPart = enemyPartData;
                    return enemyPartData;
                }
            }
            return null;
        }

        private const float CHANGE_PART_FREQ = 1.5f;
        private const int CHANGE_PART_ITERATION_ATTEMPTS = 10;

        private bool _willChangePart = false;
        private bool _changePart = false;

        public Vector3? GetPointToShoot()
        {
            checkChangePart();

            var partToShoot = SelectedPart ?? changeSelectedPart();

            if (partToShoot == null) {
                foreach (var part in Enemy.Vision.VisionChecker.EnemyParts.Parts.Values) {
                    if (part?.CanShoot == true && part.LastSuccessShootPoint != null) {
                        partToShoot = part;
                        break;
                    }
                }
            }

            return partToShoot?.LastSuccessShootPoint;
        }

        private void addEnemyParts()
        {
            CanShootHead = _headWeight > 0;
            if (CanShootHead) {
                _selector.AddOption(EBodyPart.Head, _headWeight);
            }
            _selector.AddOption(EBodyPart.Chest, _chestWeight);
            _selector.AddOption(EBodyPart.Stomach, _stomachWeight);
            _selector.AddOption(EBodyPart.LeftArm, _leftArmWeight);
            _selector.AddOption(EBodyPart.RightArm, _rightArmWeight);
            _selector.AddOption(EBodyPart.LeftLeg, _leftLegWeight);
            _selector.AddOption(EBodyPart.RightLeg, _rightLegWeight);

            _selector.Test();
        }

        private int _headWeight = 0;
        private int _chestWeight = 10;
        private int _stomachWeight = 6;
        private int _leftArmWeight = 3;
        private int _rightArmWeight = 3;
        private int _leftLegWeight = 4;
        private int _rightLegWeight = 4;

        private float _lastChangePartTime;

        private readonly WeightedRandomSelector<EBodyPart> _selector = new WeightedRandomSelector<EBodyPart>();
    }
}