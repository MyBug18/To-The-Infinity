using System;
using System.Collections.Generic;

namespace Core
{
    public sealed class ModifierCore : IEquatable<ModifierCore>
    {
        public ModifierCore(string name, string targetType, bool isTileLimited, bool isPlayerExclusive,
            string additionalDesc, IReadOnlyDictionary<string, ModifierScope> scope)
        {
            Name = name;
            TargetType = targetType;
            IsTileLimited = isTileLimited;
            IsPlayerExclusive = isPlayerExclusive;
            AdditionalDesc = additionalDesc;
            Scope = scope;
        }

        public string Name { get; }

        public string TargetType { get; }

        public bool IsTileLimited { get; }

        public bool IsPlayerExclusive { get; }

        public string AdditionalDesc { get; }

        public IReadOnlyDictionary<string, ModifierScope> Scope { get; }

        public bool Equals(ModifierCore m) => m != null && Name == m.Name;

        public override bool Equals(object obj) => obj is ModifierCore m && Equals(m);

        public override int GetHashCode() => Name.GetHashCode();

        public void OnAdded(IModifierEffectHolder target, int adderObjectId)
        {
            if (!Scope.TryGetValue(target.TypeName, out var scope)) return;

            if (!scope.TriggerEvent.TryGetValue("OnAdded", out var onAdded)) return;

            onAdded.TryInvoke($"Scope.{target.TypeName}.OnAdded", Name, target, Game.Instance.GetObject(adderObjectId));
        }

        public void OnRemoved(IModifierEffectHolder target, int adderObjectId)
        {
            if (!Scope.TryGetValue(target.TypeName, out var scope)) return;

            if (!scope.TriggerEvent.TryGetValue("OnRemoved", out var onRemoved)) return;

            onRemoved.TryInvoke($"Scope.{target.TypeName}.OnRemoved", Name, target,
                Game.Instance.GetObject(adderObjectId));
        }

        public IReadOnlyList<ModifierEffect> GetEffects(IModifierEffectHolder target, int adderObjectId) =>
            Scope.TryGetValue(target.TypeName, out var scope)
                ? scope.GetEffects(target, adderObjectId)
                : new List<ModifierEffect>();
    }
}
