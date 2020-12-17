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
        ///     The unique Id of this object (Should only be set on initialization!)
        /// </summary>
        int Id { get; set; }

        /// <summary>
        ///     Every InfinityObject has it's own small dictionary which can store string key and primitive type value
        /// </summary>
        LuaDictWrapper Storage { get; }

        void StartNewTurn(int week);

        InfinityObjectData Save();
    }
}
