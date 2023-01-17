using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Exaroton
{
    // TODO: Create minecraft command parser/builder. Possibly a separate repository
    public interface IMinecraftCommand
    {
        public string Build();
    }
}