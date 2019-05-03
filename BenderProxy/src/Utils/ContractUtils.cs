using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BenderProxy.Utils
{
    public class ContractUtils
    {
        public static void Requires<T>(bool condition, string message) where T : Exception
        {
            if (!condition)
            {
                throw Activator.CreateInstance(typeof(T), message) as T;
            }
        }
    }
}
