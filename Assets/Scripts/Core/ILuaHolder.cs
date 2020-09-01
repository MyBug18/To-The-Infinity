using MoonSharp.Interpreter;

public interface ILuaHolder
{
    /// <summary>
    /// The name of it.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Which type it is.
    /// </summary>
    [MoonSharpHidden]
    string TypeName { get; }

    /// <summary>
    /// After all data is loaded, validate it's members with dependencies.
    /// </summary>
    /// <returns>Whether there is not valid member</returns>
    [MoonSharpHidden]
    bool IsValid { get; }

    /// <summary>
    /// The file path of lua file.
    /// </summary>
    [MoonSharpHidden]
    string FilePath { get; }

    /// <summary>
    /// Initializes it's members with lua script.
    /// </summary>
    /// <param name="luaScript"></param>
    /// <returns>Whether it is well-loaded or not.</returns>
    [MoonSharpHidden]
    bool Load(Script luaScript);
}
