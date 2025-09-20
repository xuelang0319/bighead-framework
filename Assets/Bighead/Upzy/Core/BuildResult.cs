using System.Collections.Generic;
using Bighead.Core;

namespace Bighead.Upzy.Core
{
    public class BuildResult
    {
        public ChangeLevel changeLevel;
        public List<Core.BuildEntry> entries = new List<Core.BuildEntry>();
        public string aggregateHash;
    }
}