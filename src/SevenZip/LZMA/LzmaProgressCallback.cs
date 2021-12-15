namespace SevenZip
{
    using System;

    using SevenZip.Sdk;

    /// <summary>
    /// Callback to implement the ICodeProgress interface
    /// </summary>
    internal sealed class LzmaProgressCallback : ICodeProgress
    {
        private readonly long _inSize;
        private float _oldPercentDone;

        /// <summary>
        /// Initializes a new instance of the LzmaProgressCallback class
        /// </summary>
        /// <param name="inSize">The input size</param>
        /// <param name="working">Progress event handler</param>
        public LzmaProgressCallback(long inSize, EventHandler<ProgressEventArgs> working)
        {
            _inSize = inSize;
            Working += working;
        }

        #region ICodeProgress Members

        /// <summary>
        /// Sets the progress
        /// </summary>
        /// <param name="inSize">The processed input size</param>
        /// <param name="outSize">The processed output size</param>
        public void SetProgress(long inSize, long outSize)
        {
            if (Working != null)
            {
                float newPercentDone = (inSize + 0.0f) / _inSize;
                float delta = newPercentDone - _oldPercentDone;
                if (delta * 100 < 1.0)
                {
                    delta = 0;
                }
                else
                {
                    _oldPercentDone = newPercentDone;
                }
                Working(this, new ProgressEventArgs(
                                  PercentDoneEventArgs.ProducePercentDone(newPercentDone),
                                  delta > 0 ? PercentDoneEventArgs.ProducePercentDone(delta) : (byte)0));
            }
        }

        #endregion

        public event EventHandler<ProgressEventArgs> Working;
    }
}
