using System;
using System.Diagnostics;

namespace Bighead.Core.Utility
{
    public class TimerLoop
    {
        /// <summary>
        /// 计时器
        /// </summary>
        private readonly Stopwatch _stopwatch = new Stopwatch();

        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="duration">计时时间</param>
        /// <param name="foo">执行函数，若返回值为true则强行打断</param>
        public void Execute(float duration, Func<bool> foo)
        {
            var millisecond = duration * 1000;
            _stopwatch.Restart();
            while (true)
            {
                var forceBreak = foo.Invoke();
                if (forceBreak) break;

                if (_stopwatch.ElapsedMilliseconds >= millisecond)
                {
                    break;
                }
            }
            
            _stopwatch.Stop();
        }
    }
}