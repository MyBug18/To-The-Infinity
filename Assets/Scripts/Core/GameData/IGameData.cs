using MoonSharp.Interpreter;

namespace Core.GameData
{
    public interface IGameData
    {
        [MoonSharpHidden]
        bool HasDefaultValue { get; }

        [MoonSharpHidden]
        void AddNewData(ILuaHolder luaHolder);

        [MoonSharpHidden]
        void OnGameInitialized(Game game);
    }
}
