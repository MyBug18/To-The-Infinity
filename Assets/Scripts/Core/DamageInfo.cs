using Core.GameData;
using MoonSharp.Interpreter;

namespace Core
{
    [MoonSharpUserData]
    public sealed class DamageInfo
    {
        private int _amount;

        private string _damageType;

        public IInfinityObject Inflicter { get; }

        public int Amount => _amount;

        public string DamageType => _damageType;

        public bool IsMelee { get; }

        public DamageInfo(IInfinityObject inflicter, int amount, string damageType, bool isMelee)
        {
            Inflicter = inflicter;
            _amount = amount;
            _damageType = damageType;
            IsMelee = isMelee;
        }

        public void ChangeWithPercent(double value) => _amount = (int)(_amount * (1 + value / 100));

        public void ChangeDamageType(string damageType)
        {
            if (!GameDataStorage.Instance.GetGameData<DamageTypeData>().ExistsDamageType(damageType))
            {
                Logger.Log(LogType.Warning, "", $"{damageType} is not defined in DamageTypeData, so it will be ignored.");
                return;
            }

            _damageType = damageType;
        }
    }
}
