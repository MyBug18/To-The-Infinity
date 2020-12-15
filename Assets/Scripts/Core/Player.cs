using System;
using System.Collections.Generic;
using System.Linq;
using Core.GameData;
using MoonSharp.Interpreter;

namespace Core
{
    public enum RelationType
    {
        Neutral, // Default value
        Friendly,
        Hostile,
    }

    public interface IPlayer : ISpecialActionHolder
    {
        public Game Game { get; }

        public string PlayerName { get; }

        public void SetRelation(string target, RelationType type);

        public RelationType GetRelationInner(string target);
    }

    [MoonSharpUserData]
    public sealed class Player : IPlayer
    {
        public Game Game { get; }

        public string PlayerName { get; }

        public IPlayer OwnPlayer => this;

        public string TypeName => nameof(Player);

        public int Id { get; set; }

        public LuaDictWrapper Storage { get; }


        #region Relation

        private readonly Dictionary<string, RelationType> _relations = new Dictionary<string, RelationType>();

        public Player(string playerName) => PlayerName = playerName;

        public void StartNewTurn(int month)
        {
        }

        [MoonSharpHidden]
        public InfinityObjectData Save()
        {
            var result = new Dictionary<string, object>
            {
                ["PlayerName"] = PlayerName,
                ["Storage"] = Storage.Data,
                ["Relation"] = _relations,
                ["OwnPlayer"] = OwnPlayer.Id,
                ["SpecialActions"] = SpecialActions.Keys.ToArray(),
            };

            return new InfinityObjectData(Id, TypeName, result);
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

        private readonly Dictionary<string, SpecialAction> _specialActions = new Dictionary<string, SpecialAction>();

        public IReadOnlyDictionary<string, SpecialAction> SpecialActions => _specialActions;

        public void AddSpecialAction(string name)
        {
            if (_specialActions.ContainsKey(name)) return;

            _specialActions[name] = GameDataStorage.Instance.GetGameData<SpecialActionData>()
                .GetSpecialActionDirectly(this, name);
        }

        public bool CheckSpecialActionCost(IReadOnlyDictionary<string, int> cost) =>
            throw new NotImplementedException();

        public void ConsumeSpecialActionCost(IReadOnlyDictionary<string, int> cost)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    [MoonSharpUserData]
    public sealed class NoPlayer : IPlayer
    {
        private static NoPlayer _instance;

        public static NoPlayer Instance => _instance ??= new NoPlayer();

        public Game Game { get; }

        public string PlayerName => nameof(NoPlayer);

        [MoonSharpHidden]
        public void SetRelation(string target, RelationType type)
        {
            // NoPlayer has no relation
        }

        [MoonSharpHidden]
        public RelationType GetRelationInner(string target) => RelationType.Neutral;

        public void StartNewTurn(int month)
        {
        }

        [MoonSharpHidden]
        public InfinityObjectData Save()
        {
            var result = new Dictionary<string, object>
            {
                ["PlayerName"] = PlayerName,
            };

            return new InfinityObjectData(Id, TypeName, result);
        }

        public IPlayer OwnPlayer => this;

        public string TypeName => nameof(NoPlayer);

        public int Id { get; set; } = 0;

        public LuaDictWrapper Storage => null;

        public IReadOnlyDictionary<string, SpecialAction> SpecialActions => new Dictionary<string, SpecialAction>();

        public void AddSpecialAction(string name)
        {

        }

        public bool CheckSpecialActionCost(IReadOnlyDictionary<string, int> cost) => false;

        public void ConsumeSpecialActionCost(IReadOnlyDictionary<string, int> cost)
        {
            // NoPlayer can't cast special action
        }
    }
}
