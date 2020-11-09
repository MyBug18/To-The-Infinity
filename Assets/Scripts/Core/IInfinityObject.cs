namespace Core
{
    /// <summary>
    ///     Every InfinityObject has it's own small dictionary which can store string key and primitive type value
    /// </summary>
    public interface IInfinityObject
    {
        Player OwnPlayer { get; }

        /// <summary>
        ///     The type name of this object
        /// </summary>
        string TypeName { get; }

        /// <summary>
        ///     The GUID of this object
        /// </summary>
        string Guid { get; }


        LuaDictWrapper Storage { get; }
    }
}
