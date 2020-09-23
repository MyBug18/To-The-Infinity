using MoonSharp.Interpreter;

namespace Core
{
    public interface IEventHolder : ITypeNameHolder
    {
        void SubscribeEvent(string eventType, Closure luaFunction);

        void UnsubscribeEvent(string eventType, Closure luaFunction);
    }
}
