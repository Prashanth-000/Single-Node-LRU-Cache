using System;
using System.Collections.Generic;
using System.Text;

namespace Single_Node_Cache.Services
{
    internal class CleanupService
    {
        private readonly Action _cleanupAction;

        public CleanupService(Action cleanupAction)
        {
            _cleanupAction = cleanupAction;
        }

        public void Start()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(5000);
                    _cleanupAction();
                }
            });
        }
    }
}
