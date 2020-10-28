namespace Core
{
    /// <summary>
    /// Every InfinityObject has it's own small dictionary which can store string key and primitive type value
    /// </summary>
    public interface IInfinityObject
    {
        /// <summary>
        /// The type name of this object
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// The GUID of this object
        /// </summary>
        string Guid { get; }

        /// <summary>
        /// Gets object with string key
        /// </summary>
        object GetCustomValue(string key, object defaultValue);

        /// <summary>
        /// Sets custom value with given key
        /// </summary>
        void SetCustomValue(string key, object value);
    }
}
