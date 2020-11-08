using System;
using System.Reactive.Linq;

namespace Athernet.PhysicalLayer
{
    public static class FrameWindowExtension
    {
        public static IObservable<IObservable<T>> MyWindow<T>(
            this IObservable<T> source, 
            int count)
        {
            var shared = source.Publish().RefCount();
            var windowEdge = shared
                .Select((i, idx) => idx % count)
                .Where(mod => mod == 0)
                .Publish()
                .RefCount();
            return shared.Window(windowEdge, _ => windowEdge);
        }
    }
}