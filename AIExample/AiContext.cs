using System;
using System.Collections.Generic;

namespace engine.core.ai
{
    /// <summary>
    /// Class represent for storing context state values
    /// </summary>
    public class AiContext
    {
        public AiContext(AiBase ai)
        {
            this.ai = ai;
        }

        public List<String> conditions = new List<String>();
		public List<String> signals = new List<String>();
        public Dictionary<String, Object> global = new Dictionary<String, Object>();
        public Dictionary<String, Object> local = new Dictionary<String, Object>();
		public AiBase ai;
    }
}
