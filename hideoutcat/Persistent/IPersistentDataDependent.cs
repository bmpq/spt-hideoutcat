using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tarkin.hideoutcat.Persistent
{
    internal interface IPersistentDataDependent
    {
        void OnPersistentDataLoad(CatPersistentDataController data);
    }
}
