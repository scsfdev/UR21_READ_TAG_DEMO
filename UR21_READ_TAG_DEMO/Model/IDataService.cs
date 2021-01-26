using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UR21_READ_TAG_DEMO.Model
{
    public interface IDataService
    {
        void GetData(Action<DataItem, Exception> callback);
    }
}
