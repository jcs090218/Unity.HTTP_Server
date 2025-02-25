using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace com.jcs090218.HTTP_Server
{
    public static class Util
    {
        public static object InvokeWithNamedParameters(this MethodBase self, object obj, IDictionary<string, object> namedParameters)
        {
            return self.Invoke(obj, MapParameters(self, namedParameters));
        }

        public static object[] MapParameters(MethodBase method, IDictionary<string, object> namedParameters)
        {
            ParameterInfo[] paramInfos = method.GetParameters().ToArray();
            object[] parameters = new object[paramInfos.Length];
            int index = 0;

            foreach (var item in paramInfos)
            {
                object parameterName;
                if (!namedParameters.TryGetValue(item.Name, out parameterName))
                {
                    parameters[index] = Type.Missing;

                    ++index;

                    continue;
                }

                parameters[index] = ObjectCastTypeByParameterInfo(item, parameterName);

                ++index;
            }

            return parameters;
        }
        static object ObjectCastTypeByParameterInfo(ParameterInfo parameterInfo, object value)
        {
            if (parameterInfo.ParameterType == typeof(int) ||
                parameterInfo.ParameterType == typeof(System.Int32) ||
                parameterInfo.ParameterType == typeof(System.Int16) ||
                parameterInfo.ParameterType == typeof(System.Int64))
            {
                return (int)Convert.ChangeType(value, typeof(int));
            }
            else
            {
                return value;
            }
        }
    }
}
