using System;
using System.Threading;

namespace Athernet.MacLayer
{
    public class BackOffHandler
    {
        public int Collisions { get; set; }
        public int Scale = 1;
        
        private Random _random = new Random();
        private int RandMax => (1 << Collisions) - 1;

        public int Wait()
        {
            Collisions++;
            var waitTime = _random.Next(0, RandMax) * Scale;
            Thread.Sleep(waitTime);
            return waitTime;
        }

        public void Reset() => Collisions = 0;
    }
}