namespace Core
{
    public interface IInfinityObject
    {
        IPlayer OwnPlayer { get; }

        /// <summary>
        ///     The type name of this object
        /// </summary>
        string TypeName { get; }

        /// <summary>
        ///     The GUID of this object
        /// </summary>
        string Guid { get; }

        /// <summary>
        ///     Every InfinityObject has it's own small dictionary which can store string key and primitive type value
        /// </summary>
        LuaDictWrapper Storage { get; }

        void StartNewTurn(int month);

        InfinityObjectData Save();
    }
}
