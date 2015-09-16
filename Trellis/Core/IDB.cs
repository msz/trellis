using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trellis.Core
{
    public interface IDB
    {
        IDBCollection GetCollection(string name);
    }
}
