using Core.GameData;
using MoonSharp.Interpreter;

namespace Core
{
    [MoonSharpUserData]
    public sealed class DamageInfo
    {
        public DamageInfo(IInfinityObject inflicter, int amount, string damageType, bool isMelee)
        {
            Inflicter = inflicter;
            Amount = amount;
            DamageType = damageType;
            IsMelee = isMelee;
        }

        public IInfinityObject Inflicter { get; }

        public int Amount { get; private set; }

        public string DamageType { get; private set; }

        public bool IsMelee { get; }

        public void ChangeWithPercent(double value) => Amount = (int)(Amount * (1 + value / 100));

        public void ChangeDamageType(string damageType)
        {
            if (!GameDataStorage.Instance.GetGameData<DamageTypeData>().ExistsDamageType(damageType))
            {
                Logger.Log(LogType.Warning, "",
                    $"{damageType} is not defined in DamageTypeData, so it will be ignored.");
                return;
            }

            DamageType = damageType;
        }
    }
}
