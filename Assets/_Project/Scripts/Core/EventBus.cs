// -----------------------------------------------------------------------------
//  Framed Drift  -  Core
//  GDD 0.2  secao 20.1
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace FramedDrift.Core
{
    /// <summary>
    /// Pub/sub tipado. Implementado (e nao deixado em branco) porque e encanamento
    /// sem decisao de design: praticamente todo sistema abaixo depende dele para nao
    /// virar uma teia de referencias diretas.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> Handlers = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> handler)
        {
            Delegate existing;
            Handlers.TryGetValue(typeof(T), out existing);
            Handlers[typeof(T)] = Delegate.Combine(existing, handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            Delegate existing;
            if (!Handlers.TryGetValue(typeof(T), out existing)) return;
            Delegate remaining = Delegate.Remove(existing, handler);
            if (remaining == null) Handlers.Remove(typeof(T));
            else Handlers[typeof(T)] = remaining;
        }

        public static void Publish<T>(T evt)
        {
            Delegate existing;
            if (!Handlers.TryGetValue(typeof(T), out existing)) return;
            Action<T> typed = existing as Action<T>;
            if (typed != null) typed(evt);
        }

        /// <summary>Necessario porque os estaticos sobrevivem ao Play Mode no editor.</summary>
        public static void Clear()
        {
            Handlers.Clear();
        }
    }
}
