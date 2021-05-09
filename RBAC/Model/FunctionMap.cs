using System;
using System.Collections.Generic;
using NetCasbin.Util;
using NetCasbin.Util.Function;

namespace RBAC.Model
{
    public class FunctionMap
    {
        public IDictionary<string, Delegate> FunctionDict { get; private set; }

        public void AddFunction(string name, Delegate function)
        {
            FunctionDict.Add(name, function);
        }

        public static FunctionMap LoadFunctionMap()
        {
            var map = new FunctionMap
            {
                FunctionDict = new Dictionary<string, Delegate>()
            };

            map.AddFunction("keyMatch",  new KeyMatchFunc());
            map.AddFunction("keyMatch2", new KeyMatch2Func());
            map.AddFunction("keyMatch3", new KeyMatch3Func());
            map.AddFunction("keyMatch4", new KeyMatch4Func());
            map.AddFunction("regexMatch", new RegexMatchFunc());
            map.AddFunction("ipMatch", new IPMatchFunc());
            return map;
        }
    }
}
