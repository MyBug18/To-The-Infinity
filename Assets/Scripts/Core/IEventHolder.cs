using MoonSharp.Interpreter;

namespace Core
{
    public interface IEventHolder
    {
        void SubscribeEvent(string eventType, Closure luaFunction);

        void UnsubscribeEvent(string eventType, Closure luaFunction);
    }
}