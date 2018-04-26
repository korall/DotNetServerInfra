using System;
using System.Text;
using System.Collections.Generic;

namespace Utility.Data
{
    public class VTableToJsonEx
    {
        VTable mVTable;
        VTableRow mVTableRow;

        Dictionary<string, bool> mSpecifiedFields = new Dictionary<string, bool>();
        public VTableToJsonEx(VTable vTable)
        {
            mVTable = vTable;
        }

        public VTableToJsonEx(VTableRow vTableRow)
        {
            mVTableRow = vTableRow;
        }

        public void ExcludeField(string fieldName)
        {
            if (mSpecifiedFields.ContainsKey(fieldName))
                mSpecifiedFields[fieldName] = true;
            else
                mSpecifiedFields.Add(fieldName, true);
        }

        public void OnlyFields(params string[] fields)
        {
            mSpecifiedFields.Clear();
            for (int i = 0; i < fields.Length; i++)
                mSpecifiedFields.Add(fields[i], false);
        }

        string RowtoJson(VTableRow row)
        {
            StringBuilder str = new StringBuilder();
            str.Append("{");

            int cellCount = 0;
            bool isOnlyMode = mSpecifiedFields.Count > 0 && mSpecifiedFields.ContainsValue(false);

            for (int i = 0; i < row.Table.Schema.ColumnCount; i++)
            {
                string colName = row.Table.Schema.FieldNameOfCol(i);
                if (isOnlyMode && (!mSpecifiedFields.ContainsKey(colName) || mSpecifiedFields[colName]))
                    continue;

                if (mSpecifiedFields.Count > 0 && mSpecifiedFields.ContainsKey(colName) && mSpecifiedFields[colName])
                    continue;

                var cell = row[i];
                if (cellCount == 0)
                {
                    if (cell.DataType == VTypeCode.String && !cell.IsNull())
                        str.AppendFormat("\"{0}\":\"{1}\"", colName, cell.ToJson());
                    else
                        str.AppendFormat("\"{0}\":{1}", colName, cell.ToJson());
                }
                else
                {
                    if (cell.DataType == VTypeCode.String && !cell.IsNull())
                        str.AppendFormat(",\"{0}\":\"{1}\"", colName, cell.ToJson());
                    else
                        str.AppendFormat(",\"{0}\":{1}", colName, cell.ToJson());
                }

                cellCount++;
            }

            str.Append("}");
            return str.ToString();
        }

        public string ToJson()
        {
            if (mVTableRow != null)
                return RowtoJson(mVTableRow);

            if (mVTable == null)
                return "{}";

            StringBuilder str = new StringBuilder();
            int rowCount = 0;
            for (int i = 0; i < mVTable.mDataRows.Count; i++)
            {
                if (mVTable.mDataRows[i].IsDummyRow)
                    continue;

                var row = mVTable.mDataRows[i];
                if (row.IsEmptyRow)
                    continue;

                if (rowCount > 0)
                    str.Append(",");

                str.Append(RowtoJson(row));
                rowCount++;
            }

            if (rowCount == 0)
                return $"{{\"{mVTable.TableName}\":null}}";

            if (rowCount == 1)
                return $"{{\"{mVTable.TableName}\":{str}}}";
            else
                return $"{{\"{mVTable.TableName}\":[{str}]}}";
        }

        object RowtoJsonableObj(VTableRow row)
        {
            Dictionary<string, object> rowDic = new Dictionary<string, object>();
            bool isOnlyMode = mSpecifiedFields.Count > 0 && mSpecifiedFields.ContainsValue(false);

            for (int i = 0; i < row.Table.Schema.ColumnCount; i++)
            {
                string colName = row.Table.Schema.FieldNameOfCol(i);
                if (isOnlyMode && (!mSpecifiedFields.ContainsKey(colName) || mSpecifiedFields[colName]))
                    continue;

                if (mSpecifiedFields.Count > 0 && mSpecifiedFields.ContainsKey(colName) && mSpecifiedFields[colName])
                    continue;

                var cell = row[i];
                var key = row.Table.Schema.FieldNameOfCol(i);
                if (cell.IsNull() || cell.DataType != VTypeCode.VTable)
                {
                    if (!rowDic.ContainsKey(key))
                        rowDic.Add(key, cell.DataObj);
                }
                else
                {
                    var vTable = (VTable)cell.DataObj;
                    if (!rowDic.ContainsKey(key))
                        rowDic.Add(key, vTable.ToJsonableObj());
                }
            }

            return rowDic;
        }

        public object ToJsonableObj()
        {
            if (mVTableRow != null)
                return RowtoJsonableObj(mVTableRow);

            var tableList = new List<object>();
            if (mVTable == null)
                return tableList;

            for (int i = 0; i < mVTable.mDataRows.Count; i++)
            {
                if (mVTable.mDataRows[i].IsDummyRow)
                    continue;

                var row = mVTable.mDataRows[i];
                if (row.IsEmptyRow)
                    continue;

                tableList.Add(row.ToJsonableObj());
            }

            return tableList;
        }
    }

    public static class VTableToJsonExt
    {
        /////////////////////////////////////
        /// Json := Value
        /// Value := String | Number | Object | Array | true | false | null
        /// String := '"' EscapedStr '"'
        /// Number := DecimalNum
        /// KVair  := String ':' Value
        /// Object := '{' KVair {',' KVair} '}'
        /// Array  := '[' Value {',' Value } ']'
        ///////////////////////////////////////
        static string JsonEscape(string origin)
        {
            if (string.IsNullOrEmpty(origin))
                return origin;
            return origin.Replace("\\", "\\\\").Replace("/", "\\/").Replace("\"", "\\\"").Replace("\t", "\\\t").Replace("\r", "\\\r").Replace("\n", "\\\n");
        }
        public static string ToJson(this VTableCellData data)
        {
            if (data.IsNull())
                return "null";

            if (data.DataType == VTypeCode.VTable)
                return ToJson((VTable)data.DataObj);
            else if (data.DataType == VTypeCode.String)
                return JsonEscape((string)data.DataObj);

            return data.DataObj.ToString();
        }

        public static string ToJson(this VTableRow row)
        {
            StringBuilder str = new StringBuilder();
            str.Append("{");

            for (int i = 0; i < row.Table.Schema.ColumnCount; i++)
            {
                var cell = row[i];
                if (i == 0)
                {
                    if (cell.DataType == VTypeCode.String && !cell.IsNull())
                        str.AppendFormat("\"{0}\":\"{1}\"", row.Table.Schema.FieldNameOfCol(i), cell.ToJson());
                    else
                        str.AppendFormat("\"{0}\":{1}", row.Table.Schema.FieldNameOfCol(i), cell.ToJson());
                }
                else
                {
                    if (cell.DataType == VTypeCode.String && !cell.IsNull())
                        str.AppendFormat(",\"{0}\":\"{1}\"", row.Table.Schema.FieldNameOfCol(i), cell.ToJson());
                    else
                        str.AppendFormat(",\"{0}\":{1}", row.Table.Schema.FieldNameOfCol(i), cell.ToJson());
                }
            }

            str.Append("}");
            return str.ToString();
        }

        public static string ToJson(this VTableRow row, Dictionary<string, bool> exportSetting)
        {
            if (exportSetting.Count <= 0)
                return "{}";

            bool isIncludeMode = false;
            foreach (var setting in exportSetting.Values)
            {
                isIncludeMode = setting;
                break;
            }

            StringBuilder str = new StringBuilder();
            str.Append("{");

            for (int i = 0; i < row.Table.Schema.ColumnCount; i++)
            {
                if (row.Table.Schema.MainIndexCol != i)
                {
                    var filedName = row.Table.Schema.FieldNameOfCol(i);
                    if (isIncludeMode && !exportSetting.ContainsKey(filedName))
                        continue;
                    else if (!isIncludeMode && exportSetting.ContainsKey(filedName))
                        continue;
                }

                var cell = row[i];
                if (i == 0)
                {
                    if (cell.DataType == VTypeCode.String && !cell.IsNull())
                        str.AppendFormat("\"{0}\":\"{1}\"", row.Table.Schema.FieldNameOfCol(i), cell.ToJson());
                    else
                        str.AppendFormat("\"{0}\":{1}", row.Table.Schema.FieldNameOfCol(i), cell.ToJson());
                }
                else
                {
                    if (cell.DataType == VTypeCode.String && !cell.IsNull())
                        str.AppendFormat(",\"{0}\":\"{1}\"", row.Table.Schema.FieldNameOfCol(i), cell.ToJson());
                    else
                        str.AppendFormat(",\"{0}\":{1}", row.Table.Schema.FieldNameOfCol(i), cell.ToJson());
                }
            }

            str.Append("}");
            return str.ToString();
        }

        public static string ToJson(this VTable table, Dictionary<string, bool> exportSetting = null)
        {
            StringBuilder str = new StringBuilder();
            int rowCount = 0;
            for (int i = 0; i < table.mDataRows.Count; i++)
            {
                if (table.mDataRows[i].IsDummyRow)
                    continue;

                var row = table.mDataRows[i];
                if (row.IsEmptyRow)
                    continue;

                if (rowCount > 0)
                    str.Append(",");

                if (exportSetting == null)
                    str.Append(row.ToJson());
                else
                    str.Append(row.ToJson(exportSetting));
                rowCount++;
            }

            if (rowCount == 0)
                return $"{{\"{table.TableName}\":null}}";

            if (rowCount == 1)
                return $"{{\"{table.TableName}\":{str}}}";
            else
                return $"{{\"{table.TableName}\":[{str}]}}";
        }
    }

    public static class VTableToJsonableObjectExt
    {
        public static object ToJsonableObj(this VTableCellData cell, Dictionary<string, object> ctxDic = null)
        {
            if (ctxDic == null)
                ctxDic = new Dictionary<string, object>();

            string key = cell.ColFieldName;
            if (!string.IsNullOrEmpty(key) && !ctxDic.ContainsKey(key))
                ctxDic.Add(key, cell.DataObj);

            return ctxDic;
        }


        public static object ToJsonableObj(this VTableRow row, Dictionary<string, object> ctxDic = null)
        {
            if (ctxDic == null)
                ctxDic = new Dictionary<string, object>();

            for (int i = 0; i < row.Table.Schema.ColumnCount; i++)
            {
                var cell = row[i];
                var key = row.Table.Schema.FieldNameOfCol(i);
                if (cell.IsNull() || cell.DataType != VTypeCode.VTable)
                {
                    if (!ctxDic.ContainsKey(key))
                        ctxDic.Add(key, cell.DataObj);
                }
                else
                {
                    var vTable = (VTable)cell.DataObj;
                    if (!ctxDic.ContainsKey(key))
                        ctxDic.Add(key, vTable.ToJsonableObj());
                }
            }

            return ctxDic;
        }

        public static object ToJsonableObj(this VTable vTabe, List<object> ctxList = null)
        {
            if (ctxList == null)
                ctxList = new List<object>();

            for (int i = 0; i < vTabe.mDataRows.Count; i++)
            {
                if (vTabe.mDataRows[i].IsDummyRow)
                    continue;

                var row = vTabe.mDataRows[i];
                if (row.IsEmptyRow)
                    continue;

                ctxList.Add(row.ToJsonableObj());
            }

            return ctxList;
        }
    }

    public static class VTypeCodeFromString
    {
        public static VTypeCode TryParseVTypeCode(this string str)
        {
            Type type = Type.GetType(str);
            if (type != null)
            {
                if (type == typeof (byte[]))
                    return VTypeCode.ByteArray;
                else if (type == typeof (VTable))
                    return VTypeCode.VTable;

                var tc = Type.GetTypeCode(type);
                return (VTypeCode)(int)tc;
            }

            var result = VTypeCode.Empty;
            if (str.ToLower() == "vtable")
                result = VTypeCode.VTable;
            return result;
        }
    }
}