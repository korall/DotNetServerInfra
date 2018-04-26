using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using Utility.Data;

namespace AppSystem.DB
{
    public interface IDBLoader
    {
        Task<VTable> LoadTableAsync();

        Task<int> LoadDataIncrementallyAsync(VTable vTable, string keyValue);

        Task<VTable> LoadClusteredTableAsync(string clusterKey, string clusterValue);

        Task<int> LoadClusteredDataIncrementallyAsync(VTable vTable, string clusterKey, string clusterValue);
    }

    public struct SqlInfo
    {
        public string QueryString;
        public List<DbParameter> QueryParams;

        public static string SqlParamsToString<T>(List<T> sqlParams)
        {
            if (sqlParams == null || sqlParams.Count == 0)
                return "";
            StringBuilder str = new StringBuilder();
            int vCount = 0;
            for (int i = 0; i < sqlParams.Count; i++)
            {
                DbParameter paramObj = sqlParams[i] as DbParameter;
                if (paramObj != null)
                {
                    if (vCount > 0)
                        str.Append($", {paramObj.ParameterName} = {paramObj.Value}");
                    else
                        str.Append($"{paramObj.ParameterName} = {paramObj.Value}");
                    vCount++;
                }
            }
            return str.ToString();
        }
    }
}