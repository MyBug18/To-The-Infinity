namespace Core
{
    public interface IModifierEffectHolder : IInfinityObject
    {
        void ApplyModifierChangeToDownward(string targetPlayerName, IModifier m, bool isRemoving);
    }
}
