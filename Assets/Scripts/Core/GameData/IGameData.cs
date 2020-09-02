﻿using MoonSharp.Interpreter;

namespace Core.GameData
{
    public interface IGameData
    {
        [MoonSharpHidden]
        void AddNewData(ILuaHolder luaHolder);

        [MoonSharpHidden]
        void RemoveInvalidData();

        [MoonSharpHidden]
        void OnGameInitialized(Game game);
    }
}