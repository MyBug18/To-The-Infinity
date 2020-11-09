using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace Core
{
    public enum RelationType
    {
        Neutral, // Default value
        Friendly,
        Hostile,
    }

    [MoonSharpUserData]
    public sealed class Player : ISpecialActionHolder
    {
        public Game Game { get; }

        public string PlayerName { get; }

        public Player OwnPlayer => this;

        public string TypeName => nameof(Player);

        public string Guid => PlayerName;

        public LuaDictWrapper Storage { get; }


        #region Relation

        private readonly Dictionary<string, RelationType> _relations = new Dictionary<string, RelationType>();

        public Player(string playerName)
        {
            PlayerName = playerName;
        }

        [MoonSharpHidden]
        public void SetRelation(string target, RelationType type)
        {
            if (target == PlayerName) return;

            if (type == RelationType.Neutral)
            {
                _relations.Remove(target);
                return;
            }

            _relations[target] = type;
        }

        [MoonSharpHidden]
        public RelationType GetRelationInner(string target)
        {
            if (target == PlayerName) return RelationType.Friendly;

            return _relations.TryGetValue(target, out var result) ? result : RelationType.Neutral;
        }

        public void SetRelation(string target, string type)
        {
            switch (type.ToLower())
            {
                case "neutral":
                    _relations.Remove(target);
                    break;
                case "friendly":
                    _relations[target] = RelationType.Friendly;
                    break;
                case "hostile":
                    _relations[target] = RelationType.Hostile;
                    break;
                default:
                    Logger.Log(LogType.Warning, $"{nameof(Player)}.{nameof(SetRelation)}",
                        $"The second parameter of {nameof(SetRelation)} must be \"Neutral\" or \"Friendly\" or \"Hostile\"!");
                    break;
            }
        }

        public string GetRelation(string target) => GetRelationInner(target).ToString();

        #endregion

        #region SpecialAction

        public IReadOnlyList<SpecialAction> SpecialActions { get; }

        public bool CheckSpecialActionCost(IReadOnlyDictionary<string, int> cost) => throw new System.NotImplementedException();

        public void ConsumeSpecialActionCost(IReadOnlyDictionary<string, int> cost)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}
