using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;

namespace Utility.Data
{
    public enum VTypeCode
    {
        Empty = 0,
        Object = 1,
        DBNull = 2,
        Boolean = 3,
        Char = 4,
        SByte = 5,
        Byte = 6,
        Int16 = 7,
        UInt16 = 8,
        Int32 = 9,
        UInt32 = 10,
        Int64 = 11,
        UInt64 = 12,
        Single = 13,
        Double = 14,
        Decimal = 15,
        DateTime = 16,
        String = 18,

        VTable = 100,
        ByteArray = 101,
    }

    public class VTableSchema
    {
        int mMainIndexCol;
        List<KeyValuePair<string, VTypeCode>> mMetaData;
        Dictionary<string, int> mFieldsToColIndex;

        internal List<KeyValuePair<string, VTypeCode>> MetaData { get { return mMetaData; } }

        public string SchemaName
        {
            get; set;
        }

        public int ColumnCount
        {
            get
            {
                return mMetaData.Count;
            }
        }

        public int AppendField(string fielName, VTypeCode fieldType, int colIndex)
        {
            if (mMetaData.Count > colIndex)
            {
                mMetaData[colIndex] = new KeyValuePair<string, VTypeCode>(fielName, fieldType);

            }
            else
            {
                for (int i = mMetaData.Count; i <= colIndex; i++)
                    mMetaData.Add(new KeyValuePair<string, VTypeCode>(fielName, fieldType));
            }

            mFieldsToColIndex.Add(fielName, colIndex);
            return colIndex;
        }

        public int AppendField(string fielName, VTypeCode fieldType) =>
            AppendField(fielName, fieldType, mMetaData.Count);

        public VTableSchema(string schemeName)
        {
            SchemaName = schemeName;
            mMetaData = new List<KeyValuePair<string, VTypeCode>>();
            mFieldsToColIndex = new Dictionary<string, int>();
            mMainIndexCol = -1;
        }

        internal void SetMainIndex(int colIndex)
        {
            mMainIndexCol = colIndex;
            if (mMainIndexCol > mMetaData.Count)
                mMainIndexCol = -1;

            if (mMainIndexCol > 0)
            {
                if (mMetaData[mMainIndexCol].Value != VTypeCode.String && mMetaData[mMainIndexCol].Value != VTypeCode.Int32 && mMetaData[mMainIndexCol].Value != VTypeCode.Int16)
                    mMainIndexCol = -1;
            }
        }

        public VTableSchema(string schemaName, List<KeyValuePair<string, VTypeCode>> fields, int mainIndexCol = -1)
        {
            SchemaName = schemaName;
            mMetaData = new List<KeyValuePair<string, VTypeCode>>();
            mFieldsToColIndex = new Dictionary<string, int>();

            mMetaData.Capacity = fields.Count;
            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                if (mFieldsToColIndex.ContainsKey(field.Key))
                    throw new Exception($"Duplicated field name: \"{field.Key}\"; schema: \"{schemaName}\"");

                mMetaData.Add(field);
                mFieldsToColIndex.Add(field.Key, i);
            }

            mMainIndexCol = -1;
            SetMainIndex(mainIndexCol);
        }

        public int MainIndexCol
        {
            get
            {
                return mMainIndexCol;
            }

            internal set
            {
                mMainIndexCol = value;
            }
        }

        public VTableSchema Clone()
        {
            VTableSchema result = new VTableSchema(SchemaName);

            result.mMetaData.Capacity = mMetaData.Capacity;
            for (int i = 0; i < mMetaData.Count; i++)
            {
                var field = mMetaData[i];
                result.mMetaData.Add(field);
                result.mFieldsToColIndex.Add(field.Key, i);
            }

            if (mMainIndexCol >= 0)
                result.SetMainIndex(mMainIndexCol);

            return result;
        }

        public int FieldToColIndex(string field)
        {
            int result = -1;
            if (mFieldsToColIndex.TryGetValue(field, out result))
                return result;
            else
                return -1;
        }

        public VTypeCode VTableTypeCodeOfCol(int colIndex)
        {
            if (colIndex >= 0 && colIndex < mMetaData.Count)
                return mMetaData[colIndex].Value;

            return VTypeCode.Empty;
        }

        public string FieldNameOfCol(int colIndex)
        {
            if (colIndex >= 0 && colIndex < mMetaData.Count)
                return mMetaData[colIndex].Key;

            return "";
        }

        public static VTypeCode DeserializeVTableTypeCode(string str)
        {
            switch (str.ToUpper())
            {
                case "BOOL":
                    return VTypeCode.Boolean;
                case "BYTE":
                    return VTypeCode.SByte;
                case "SHORT":
                    return VTypeCode.Int16;
                case "INT":
                    return VTypeCode.Int32;
                case "LONG":
                    return VTypeCode.Int64;
                case "STRING":
                    return VTypeCode.String;
                case "FLOAT":
                    return VTypeCode.Single;
                case "DOUBLE":
                    return VTypeCode.Double;
                case "VTABLE":
                    return VTypeCode.VTable;
                default:
                    return VTypeCode.Empty;
            }
        }

        public static string SerializeVTableTypeCode(VTypeCode VTableTypeCode)
        {
            switch (VTableTypeCode)
            {
                case VTypeCode.Boolean:
                    return "BOOL";
                case VTypeCode.SByte:
                    return "BYTE";
                case VTypeCode.Int16:
                    return "SHORT";
                case VTypeCode.Int32:
                    return "INT";
                case VTypeCode.Int64:
                    return "LONG";
                case VTypeCode.String:
                    return "STRING";
                case VTypeCode.Single:
                    return "FLOAT";
                case VTypeCode.Double:
                    return "DOUBLE";
                case VTypeCode.VTable:
                    return "VTABLE";
                default:
                    return VTableTypeCode.ToString();
            }
        }
    }

    public sealed class VTableCellData
    {
        int mCelIndex;
        VTableRow mRow;

        //bool mIsDirty;
        object mObject;
        VTypeCode mTypeCode;

        public VTableCellData()
        { }

        public VTableCellData(int celIndex, VTableRow row)
        {
            mCelIndex = celIndex;
            mRow = row;

            mObject = mRow._GetDataObject(mCelIndex);
            mTypeCode = mRow.Table.Schema.VTableTypeCodeOfCol(mCelIndex);
        }

        public object DataObj
        {
            get
            {
                if (mRow != null)
                    mObject = mRow._GetDataObject(mCelIndex);
                return mObject;
            }
            set
            {
                if (mRow != null)
                {
                    mRow._SetDataObject(mCelIndex, value);
                    mObject = mRow._GetDataObject(mCelIndex);
                }
                else
                {
                    mObject = value;
                }
            }
        }

        public VTypeCode DataType
        {
            get { return mTypeCode; }
            set { if (mRow != null) return; mTypeCode = value; }
        }

        public VTableRow TableRow
        {
            get { return mRow; }
        }

        public int ColIndex
        {
            get { return mCelIndex; }
        }

        public string ColFieldName
        {
            get
            {
                if (mRow != null)
                    return mRow.Table.Schema.FieldNameOfCol(mCelIndex);
                else
                    return null;
            }
        }

        public bool IsNull()
        {
            return DataObj == null;
        }

        public bool IsObjectType()
        {
            return DataType == VTypeCode.Object || DataType >= VTypeCode.VTable;
        }

        public override string ToString()
        {
            return DataObj.ToString();
        }

        public void _Set(object value)
        {
            if (DataType != (VTypeCode)(int)Type.GetTypeCode(value.GetType()))
            {
                if (DataType == VTypeCode.VTable && value.GetType() != typeof(VTable))
                    return;
                if (DataType == VTypeCode.ByteArray && value.GetType() != typeof(byte[]))
                    return;
            }

            DataObj = value;
        }

        public static implicit operator byte[](VTableCellData value)
        {
            return value.IsNull() ? null : (byte[])value.DataObj;
        }

        public static implicit operator DateTime(VTableCellData value)
        {
            return (DateTime)value.DataObj;
        }

        public static implicit operator float(VTableCellData value)
        {
            return value.IsNull() ? 0.0f : (float)value.DataObj;
        }

        public static implicit operator double(VTableCellData value)
        {
            return value.IsNull() ? 0.0 : (double)value.DataObj;
        }

        public static implicit operator string(VTableCellData value)
        {
            return (string)value.DataObj;
        }

        public static implicit operator long(VTableCellData value)
        {
            return value.IsNull() ? 0 : (long)value.DataObj;
        }

        public static implicit operator int(VTableCellData value)
        {
            return value.IsNull() ? 0 : (int)value.DataObj;
        }

        public static implicit operator short(VTableCellData value)
        {
            return value.IsNull() ? (short)0 : (short)value.DataObj;
        }

        public static implicit operator byte(VTableCellData value)
        {
            return value.IsNull() ? (byte)0 : (byte)value.DataObj;
        }

        public static implicit operator bool(VTableCellData value)
        {
            return value.IsNull() ? false : (bool)value.DataObj;
        }

        public static implicit operator VTableCellData(byte[] v)
        {
            return new VTableCellData { DataObj = v, DataType = VTypeCode.ByteArray };
        }

        public static implicit operator VTableCellData(double v)
        {
            return new VTableCellData { DataObj = v, DataType = VTypeCode.Double };
        }

        public static implicit operator VTableCellData(float v)
        {
            return new VTableCellData { DataObj = v, DataType = VTypeCode.Single };
        }

        public static implicit operator VTableCellData(string v)
        {
            return new VTableCellData { DataObj = v, DataType = VTypeCode.String };
        }

        public static implicit operator VTableCellData(long v)
        {
            return new VTableCellData { DataObj = v, DataType = VTypeCode.Int64 };
        }

        public static implicit operator VTableCellData(int v)
        {
            return new VTableCellData { DataObj = v, DataType = VTypeCode.Int32 };
        }

        public static implicit operator VTableCellData(short v)
        {
            return new VTableCellData { DataObj = v, DataType = VTypeCode.Int16 };
        }

        public static implicit operator VTableCellData(byte v)
        {
            return new VTableCellData { DataObj = v, DataType = VTypeCode.SByte };
        }

        public static implicit operator VTableCellData(bool v)
        {
            return new VTableCellData { DataObj = v, DataType = VTypeCode.Boolean };
        }

        public static implicit operator VTableCellData(VTable v)
        {
            return new VTableCellData { DataObj = v, DataType = VTypeCode.VTable };
        }

        public static implicit operator VTableCellData(DateTime v)
        {
            return new VTableCellData { DataObj = v, DataType = VTypeCode.DateTime };
        }
    }

    internal enum VTableRowProperty
    {
        EMPTY = 0x1000,
        DUMMY_ROW = 0x0001,

        DATA_ROW = 0x0010,
        INDEXIES = 0x0011,
    }

    [Flags]
    public enum VTableRowModifyingFlag
    {
        NONE_MODIFY = 0x000,
        DELETED = 0x010,
        INSERTED = 0x020,
        UPDATED = 0x040,
        UPDATED_ALL = 0x080
    }

    public class VTableRow
    {
        VTable mTable;

        internal int mRowIndex;
        internal List<object> mDataObjs;
        internal VTableRowProperty mRowProperty;
        internal VTableRowModifyingFlag mModifyingFlag;
#if !RUNNING_BELOW_4
        internal SortedSet<int> mModifyingState;
#endif

        internal bool mIsWellTypedKey;
        internal bool mIsWellTypedDatas;

        public VTableRow(int rowIndex, VTable table)
        {
            mRowIndex = rowIndex;
            mTable = table;
            mIsWellTypedKey = false;
            mIsWellTypedDatas = true;

            mDataObjs = new List<object>();
        }

        public void DetacheFromTable()
        {
            mTable = null;
        }

        public VTable Table
        {
            get
            {
                return mTable;
            }
        }

        public int RowIndex
        {
            get
            {
                return mRowIndex;
            }
            set
            {
                mRowIndex = value;
            }
        }

        public void SetToTable(VTable table)
        {
            mTable = table;
        }

        public List<string> DeserializingErrorLog = new List<string>();

        public bool IsDirty => mModifyingFlag != VTableRowModifyingFlag.NONE_MODIFY;

        public bool IsIndexiesRow => (mRowProperty & VTableRowProperty.DATA_ROW) > 0 && (mRowProperty & VTableRowProperty.INDEXIES) > 0;

        public bool IsDummyRow => (mRowProperty & VTableRowProperty.DATA_ROW) == 0;

        public bool IsEmptyRow => (mRowProperty & VTableRowProperty.EMPTY) > 0;

        public VTableRowModifyingFlag ModifyingFlag => mModifyingFlag;

#if !RUNNING_BELOW_4
        public SortedSet<int> SwapOutDirtyState(ref VTableRowModifyingFlag flag)
        {
            lock (this)
            {
                var result = mModifyingState;
                mModifyingState = null;
                flag = mModifyingFlag;
                mModifyingFlag = VTableRowModifyingFlag.NONE_MODIFY;
                return result;
            }
        }
#endif

        public void MarkAsDirty(string field)
        {
            if (!mTable.MarkDirtyFlag)
                return;
            int colIndex = mTable.Schema.FieldToColIndex(field);
            if (colIndex < 0)
                return;

            lock (this)
            {
                mModifyingFlag |= VTableRowModifyingFlag.UPDATED;
#if !RUNNING_BELOW_4
                if (mModifyingState == null)
                    mModifyingState = new SortedSet<int>();

                mModifyingState.Add(colIndex);
#endif
            }

            mTable.OnRowModified(this);
        }

        internal object _GetDataObject(int index)
        {
            if (IsEmptyRow) // empty
                return null;

            if (index < 0 || mDataObjs.Count <= index)
                return null;

            lock (this)
                return mDataObjs[index];
        }

        internal void _SetDataObject(int index, object dataObj)
        {
            if (index < 0 || index >= mTable.Schema.ColumnCount)
                return;

            // 不支持动态更改主键值
            if (mRowProperty == VTableRowProperty.INDEXIES)
                if (mTable.Schema.MainIndexCol >= 0 && index == mTable.Schema.MainIndexCol)
                    return;
            lock (this)
            {
                while (mDataObjs.Count <= index)
                    mDataObjs.Add(null);

                mDataObjs[index] = dataObj;

                if (mRowProperty == VTableRowProperty.EMPTY)
                    mRowProperty = VTableRowProperty.DUMMY_ROW;

                // mark as dirty
                if (mTable.MarkDirtyFlag)
                {
                    mModifyingFlag |= VTableRowModifyingFlag.UPDATED;
#if !RUNNING_BELOW_4
                    if (mModifyingState == null)
                        mModifyingState = new SortedSet<int>();

                    mModifyingState.Add(index);
#endif
                }
            }

            mTable.OnRowModified(this);
        }

        object _GetData(string field)
        {
            int colIndex = mTable.Schema.FieldToColIndex(field);
            return _GetDataObject(colIndex);
        }

        void _SetData(string field, object dataObj)
        {
            int colIndex = mTable.Schema.FieldToColIndex(field);

            if (mTable.Schema.MainIndexCol >= 0 && colIndex == mTable.Schema.MainIndexCol && dataObj == null)
                return;

            _SetDataObject(colIndex, dataObj);
        }

        void _SetData(string field, ref VTableCellData value)
        {
            int colIndex = mTable.Schema.FieldToColIndex(field);
            if (colIndex < 0)
                return;

            if (mTable.Schema.MainIndexCol >= 0 && colIndex == mTable.Schema.MainIndexCol && value.DataObj == null)
                return;

            var expectedType = mTable.Schema.VTableTypeCodeOfCol(colIndex);
            var valueObj = value.DataObj;
            if (expectedType != value.DataType && expectedType != VTypeCode.String && expectedType != VTypeCode.VTable)
            {
                try
                {
                    var converter = TypeDescriptor.GetConverter((TypeCode)expectedType);
                    valueObj = converter.ConvertFrom(value.DataObj);
                }
                catch
                {
                    return;
                }
            }

            _SetDataObject(colIndex, valueObj);
        }

        public VTableCellData GetData(string field)
        {
            int colIndex = mTable.Schema.FieldToColIndex(field);
            if (colIndex < 0)
                return new VTableCellData { DataObj = null, DataType = VTypeCode.Empty };

            return new VTableCellData(colIndex, this);
        }

        public void SetNull(string field)
        {
            int colIndex = mTable.Schema.FieldToColIndex(field);
            if (colIndex < 0)
                return;

            _SetDataObject(colIndex, null);
        }

        public bool IsDataNull(int colIndex)
        {
            if (colIndex < 0 || colIndex >= mTable.Schema.ColumnCount)
                return true;

            if (colIndex >= mDataObjs.Count)
                return true;

            return mDataObjs[colIndex] == null;
        }

        public bool IsDataNull(string field)
        {
            int colIndex = mTable.Schema.FieldToColIndex(field);
            return IsDataNull(colIndex);
        }

        public VTableCellData this[string field]
        {
            get
            {
                return GetData(field);
            }

            set
            {
                _SetData(field, ref value);
            }
        }

        public VTableCellData this[int colIndex]
        {
            get
            {
                if (colIndex < 0 || colIndex >= mTable.Schema.ColumnCount)
                    return null;
                return new VTableCellData(colIndex, this);
            }

            set
            {
                if (colIndex < 0 || colIndex >= mTable.Schema.ColumnCount)
                    return;

                var expectedType = mTable.Schema.VTableTypeCodeOfCol(colIndex);
                if (expectedType != value.DataType && expectedType != VTypeCode.String)
                    return;

                _SetDataObject(colIndex, value.DataObj);
            }
        }

        bool _BooleanFromString(string valueStr)
        {
            int iValue = 0;
            if (int.TryParse(valueStr, out iValue))
                return iValue > 0;

            return Convert.ToBoolean(valueStr);
        }

        public void OnJoinToTableIndex()
        {
            mRowProperty |= VTableRowProperty.INDEXIES;
        }

        public void DeserializeFromStrings(List<string> contents)
        {
            mDataObjs.Clear();
            for (int i = 0; i < mTable.Schema.ColumnCount; i++)
            {
                if (i < contents.Count)
                {
                    var content = contents[i];
                    if (string.IsNullOrEmpty(content))
                    {
                        mDataObjs.Add(null);
                    }
                    else
                    {
                        var dataType = mTable.Schema.VTableTypeCodeOfCol(i);
                        try
                        {
                            if (dataType == VTypeCode.String)
                            {
                                mDataObjs.Add(content);
                            }
                            else
                            {
                                content = content.Trim();
                                if (string.IsNullOrEmpty(content))
                                    mDataObjs.Add(null);
                                else if (dataType == VTypeCode.Double)
                                    mDataObjs.Add(Convert.ToDouble(content));
                                else if (dataType == VTypeCode.Single)
                                    mDataObjs.Add(Convert.ToSingle(content));
                                else if (dataType == VTypeCode.Int64)
                                    mDataObjs.Add(Convert.ToInt64(content));
                                else if (dataType == VTypeCode.Int32)
                                    mDataObjs.Add(Convert.ToInt32(content));
                                else if (dataType == VTypeCode.Int16)
                                    mDataObjs.Add(Convert.ToInt16(content));
                                else if (dataType == VTypeCode.SByte)
                                    mDataObjs.Add(Convert.ToSByte(content));
                                else if (dataType == VTypeCode.Boolean)
                                    mDataObjs.Add(_BooleanFromString(content));
                                else
                                    mDataObjs.Add(null);
                            }

                            if (i == mTable.Schema.MainIndexCol)
                                mIsWellTypedKey = true;
                        }
                        catch (FormatException)
                        {
                            string fieldName = mTable.Schema.FieldNameOfCol(i);
                            DeserializingErrorLog.Add($"Invalid value at column {i} [\"{fieldName}\"]; type: {dataType}, raw string: \"{content}\"");
                            mIsWellTypedDatas = false;
                            mDataObjs.Add(content);
                        }
                    }
                }
                else
                {
                    mDataObjs.Add(null);
                }
            }
        }

        public void SerializeToString(List<string> contents)
        {
            for (int i = 0; i < mDataObjs.Count; i++)
            {
                if (mDataObjs[i] == null)
                    contents.Add("");
                else if (mTable.Schema.VTableTypeCodeOfCol(i) == VTypeCode.Boolean)
                    contents.Add(((bool)mDataObjs[i]) ? "1" : "0");
                else
                    contents.Add(mDataObjs[i].ToString());
            }
        }
    }

    public class VTable
    {
        VTableSchema mSchema;
        internal List<VTableRow> mDataRows = new List<VTableRow>();
        internal HashSet<VTableRow> mDirtyRows = new HashSet<VTableRow>();
        int mDataRowCount;

        bool mIsStringIndex;
        Dictionary<string, int> mMainRowIndex = new Dictionary<string, int>();
        Dictionary<long, int> mMainRowIntIndex = new Dictionary<long, int>();

        //VTable mParentTable;

        public VTableSchema Schema
        {
            get
            {
                return mSchema;
            }
        }

        public string BaseTableName { get; set; }
        public string BaseSchemaName { get; set; }

        public bool MarkDirtyFlag { get; set; }
        public string TableName { get { return mSchema.SchemaName; } }
        public int RowCount { get { return DataRows.Count(); } }

        /// <summary>
        /// return IEnumerable interface of data rows list
        /// </summary>
        public IEnumerable<VTableRow> DataRows
        {
            get
            {
                //return mDataRows.AsEnumerable();
                for (int i = 0; i < mDataRows.Count; i++)
                    if (!mDataRows[i].IsDummyRow)
                        yield return mDataRows[i];
            }
        }

        public IEnumerable<VTableRow> DirtyRows
        {
            get
            {
                foreach (var row in mDirtyRows)
                    if (!row.IsDummyRow)
                        yield return row;
            }
        }

        public IEnumerable<VTableRow> AllRows
        {
            get
            {
                return mDataRows.AsEnumerable();
            }
        }

        public void ClearDirtyFlag()
        {
            foreach (var row in mDirtyRows)
                row.mModifyingFlag = VTableRowModifyingFlag.NONE_MODIFY;

            mDirtyRows.Clear();
        }

        public IEnumerable<KeyValuePair<string, VTypeCode>> Columns
        {
            get
            {
                for (int i = 0; i < mSchema.MetaData.Count; i++)
                    yield return new KeyValuePair<string, VTypeCode>(mSchema.MetaData[i].Key, mSchema.MetaData[i].Value);
            }
        }

        internal void OnRowModified(VTableRow row)
        {
            if (row.Table != this)
                return;

            if (!MarkDirtyFlag || !row.IsDirty)
                return;

            mDirtyRows.Add(row);
        }

        int _KeyToRowIndex(string key)
        {
            if (!mIsStringIndex)
            {
                long keyValue;
                if (!long.TryParse(key, out keyValue))
                    return -1;
                return _KeyToRowIndex(keyValue);
            }

            int result = -1;
            if (!mMainRowIndex.TryGetValue(key, out result))
                result = -1;
            return result;
        }

        int _KeyToRowIndex(long key)
        {
            if (mIsStringIndex)
                return _KeyToRowIndex(key.ToString());

            int result = -1;
            if (!mMainRowIntIndex.TryGetValue(key, out result))
                result = -1;
            return result;
        }

        int _CellDataKeyToRowIndex(VTableCellData key)
        {
            if (key.DataType == VTypeCode.String)
                return _KeyToRowIndex((string)key);
            else if (key.DataType == VTypeCode.Int64)
                return _KeyToRowIndex((long)key);
            else if (key.DataType == VTypeCode.Int32)
                return _KeyToRowIndex((long)(int)key);
            else if (key.DataType == VTypeCode.Int16)
                return _KeyToRowIndex((long)(short)key);
            else
                return -1;
        }

        VTableRow _GetRowAtIndex(int rowIndex)
        {
            if (rowIndex < mDataRows.Count && mDataRows[rowIndex] != null)
                return mDataRows[rowIndex];

            for (int i = mDataRows.Count; i <= rowIndex; i++)
                mDataRows.Add(new VTableRow(i, this));

            return mDataRows[rowIndex];
        }

        bool _SetMainIndexCol(int col)
        {
            mSchema.SetMainIndex(col);
            if (mSchema.MainIndexCol < 0)
                return false;

            var fieldType = mSchema.VTableTypeCodeOfCol(col);

            bool isStringIndex = false;
            if (fieldType == VTypeCode.String)
                isStringIndex = true;

            Dictionary<long, int> intIndex = null;
            Dictionary<string, int> strIndex = null;

            if (isStringIndex)
                strIndex = new Dictionary<string, int>();
            else
                intIndex = new Dictionary<long, int>();

            if (isStringIndex)
            {
                for (int i = 0; i < mDataRows.Count; i++)
                {
                    if (mDataRows[i].IsDummyRow)
                        continue;
                    var keyValue = (string)mDataRows[i][col];
                    if (strIndex.ContainsKey(keyValue))
                        return false;

                    strIndex.Add(keyValue, i);
                }
            }
            else
            {
                if (fieldType == VTypeCode.Int64)
                {
                    for (int i = 0; i < mDataRows.Count; i++)
                    {
                        if (mDataRows[i].IsDummyRow)
                            continue;
                        var keyValue = (long)mDataRows[i][col];
                        if (intIndex.ContainsKey(keyValue))
                            return false;

                        intIndex.Add(keyValue, i);
                    }
                }
                else if (fieldType == VTypeCode.Int32)
                {
                    for (int i = 0; i < mDataRows.Count; i++)
                    {
                        if (mDataRows[i].IsDummyRow)
                            continue;
                        var keyValue = (long)(int)mDataRows[i][col];
                        if (intIndex.ContainsKey(keyValue))
                            return false;

                        intIndex.Add(keyValue, i);
                    }
                }
                else if (fieldType == VTypeCode.Int16)
                {
                    for (int i = 0; i < mDataRows.Count; i++)
                    {
                        if (mDataRows[i].IsDummyRow)
                            continue;
                        var keyValue = (long)(short)mDataRows[i][col];
                        if (intIndex.ContainsKey(keyValue))
                            return false;

                        intIndex.Add(keyValue, i);
                    }
                }
            }

            mMainRowIndex = strIndex;
            mMainRowIntIndex = intIndex;
            mIsStringIndex = isStringIndex;
            return true;
        }

        public VTable(string tableName)
        {
            mSchema = new VTableSchema(tableName);
            MarkDirtyFlag = true;
        }

        public VTable(VTableSchema schema)
        {
            mSchema = schema;
            if (mSchema.MainIndexCol >= 0)
                if (!_SetMainIndexCol(mSchema.MainIndexCol))
                    mSchema.MainIndexCol = -1;
            MarkDirtyFlag = true;
        }


        public VTable(string tableName, List<KeyValuePair<string, VTypeCode>> fields, int mainIndexCol = -1)
        {
            mSchema = new VTableSchema(tableName, fields, mainIndexCol);
            if (mSchema.MainIndexCol >= 0)
                if (!_SetMainIndexCol(mSchema.MainIndexCol))
                    mSchema.MainIndexCol = -1;
            MarkDirtyFlag = true;
        }

        public bool SetMainIndex(string fieldName)
        {
            int colIndex = mSchema.FieldToColIndex(fieldName);
            return SetMainIndex(colIndex);
        }

        public bool SetMainIndex(int col)
        {
            if (col < 0 || col >= mSchema.ColumnCount)
                return false;

            if (col == mSchema.MainIndexCol)
                return true;

            return _SetMainIndexCol(col);
        }

        public VTableRow BuildRow()
        {
            var result = new VTableRow(-1, this);

            return result;
        }

        protected int _AppendRow(VTableRow rowToAdd)
        {
            if (rowToAdd.Table != this)
                return -1;

            int index = mDataRows.Count;
            rowToAdd.mRowIndex = index;
            mDataRows.Add(rowToAdd);

            if (MarkDirtyFlag)
            {
                lock (rowToAdd)
                    rowToAdd.mModifyingFlag |= VTableRowModifyingFlag.INSERTED;
            }

            if (!rowToAdd.IsDummyRow)
                mDataRowCount++;

            return index;
        }

        protected void _ReplaceRow(VTableRow rowNew, int atIndex)
        {
            var oldRow = mDataRows[atIndex];
            mDataRows[atIndex] = rowNew;
            if (!rowNew.IsDummyRow && oldRow.IsDummyRow)
                mDataRowCount++;

            lock (rowNew)
            {
                rowNew.mModifyingFlag |= VTableRowModifyingFlag.UPDATED_ALL;
                rowNew.mRowIndex = atIndex;
            }

            oldRow.DetacheFromTable();
        }

        protected void _RemoveRow(VTableRow rowToRemove)
        {
            if (rowToRemove.Table != this)
                return;

            // 处理主索引
            bool isRemovedByKey = false;
            if (Schema.MainIndexCol >= 0 && rowToRemove.IsIndexiesRow)
            {
                var key = rowToRemove[Schema.MainIndexCol];
                var rowIndex = _KeyToRowIndex(key.ToString());

                if (rowIndex >= 0)
                {
                    mDataRows.RemoveAt(rowIndex);
                    isRemovedByKey = true;
                    mDataRowCount--;
                }
            }

            if (!isRemovedByKey)
            {
                if (mDataRows.Remove(rowToRemove))
                    if (!rowToRemove.IsDummyRow)
                        mDataRowCount--;
            }
        }

        public int AppendAsDummyRow(VTableRow rowToAdd)
        {
            if (!rowToAdd.IsDummyRow)
                return -1;
            return _AppendRow(rowToAdd);
        }

        public int ApendAsDataRow(VTableRow rowToAdd)
        {
            rowToAdd.mRowProperty |= VTableRowProperty.DATA_ROW;
            return _AppendRow(rowToAdd);
        }

        public int AddRowByKey(string key, VTableRow rowToAdd, bool isForceReplceOld = false)
        {
            if (rowToAdd.Table != this)
                return -1;

            if (mSchema.MainIndexCol < 0)
                throw new Exception("Main index NOT set at table " + mSchema.SchemaName);

            int index = mDataRows.Count;
            if (mIsStringIndex)
            {
                if (mMainRowIndex.ContainsKey(key))
                {
                    if (!isForceReplceOld)
                        throw new Exception($"Duplicated key value : {key}; table:<{mSchema.SchemaName}>");

                    index = mMainRowIndex[key];
                    _ReplaceRow(rowToAdd, index);
                }
                else
                {
                    index = _AppendRow(rowToAdd);
                    mMainRowIndex.Add(key, index);
                }
            }
            else
            {
                long keyValue;
                if (!long.TryParse(key, out keyValue))
                    return -1;

                if (mMainRowIntIndex.ContainsKey(keyValue))
                {
                    if (!isForceReplceOld)
                        throw new Exception($"Duplicated key value : {key}; table:<{mSchema.SchemaName}>");

                    index = mMainRowIndex[key];
                    _ReplaceRow(rowToAdd, index);
                }
                else
                {
                    index = _AppendRow(rowToAdd);
                    mMainRowIntIndex.Add(keyValue, index);
                }
            }

            rowToAdd.OnJoinToTableIndex();
            return index;
        }

        public int AddRowByKey(VTableCellData key, VTableRow rowToAdd, bool isForceReplceOld = false)
        {
            return AddRowByKey(key.ToString(), rowToAdd);
        }

        public int AttachRow(VTableRow rowToAdd, bool isForceReplceOld = false)
        {
            if (rowToAdd.Table != this)
                return -1;

            if (Schema.MainIndexCol >= 0)
            {
                var key = rowToAdd[Schema.MainIndexCol];
                if (key.IsNull())
                    return AppendAsDummyRow(rowToAdd);

                if (!rowToAdd.mIsWellTypedKey || !rowToAdd.mIsWellTypedDatas)
                {
                    Log.Log.WriteLog(Log.eLogLevel.LOG_WARN, "VTable", $"Table[{Schema.SchemaName}] row is NOT well formated: {key}");
                    return AppendAsDummyRow(rowToAdd);
                }

                return AddRowByKey(key.ToString(), rowToAdd, isForceReplceOld);
            }

            return ApendAsDataRow(rowToAdd);
        }

        public VTableRow GetRowByKey(int key)
        {
            int rowIndex = _KeyToRowIndex(key);
            if (rowIndex < 0)
                return null;

            return _GetRowAtIndex(rowIndex);
        }

        public IEnumerable<VTableRow> GetRowByKey(Func<VTableRow, bool> predicate)
        {
            for (int i = 0; i < mDataRows.Count; i++)
            {
                if (predicate(mDataRows[i]))
                    yield return mDataRows[i];
            }
        }

        public VTableRow GetRowByKey(string key)
        {
            int rowIndex = _KeyToRowIndex(key);
            if (rowIndex < 0)
                return null;

            return _GetRowAtIndex(rowIndex);
        }

        public VTableRow GetRowByKey(VTableCellData key)
        {
            int rowIndex = _CellDataKeyToRowIndex(key);
            if (rowIndex < 0)
                return null;

            return _GetRowAtIndex(rowIndex);
        }

        public VTableRow GetLastDataRow()
        {
            for (int i = mDataRows.Count - 1; i >= 0; i--)
                if (!mDataRows[i].IsDummyRow)
                    return mDataRows[i];

            return null;
        }

        public VTableRow GetFirstDataRow()
        {
            for (int i = 0; i < mDataRows.Count; i++)
                if (!mDataRows[i].IsDummyRow)
                    return mDataRows[i];
            return null;
        }

        public VTableRow this[int rowKey]
        {
            get
            {
                return GetRowByKey(rowKey);
            }
        }

        public VTableRow this[string rowKey]
        {
            get
            {
                return GetRowByKey(rowKey);
            }
        }

        public void SerializeTypeToString(List<string> contents)
        {
            for (int i = 0; i < Schema.MetaData.Count; i++)
            {
                var typeStr = VTableSchema.SerializeVTableTypeCode(Schema.MetaData[i].Value);
                contents.Add(typeStr);
            }
        }

        public void SerializeFieldsToString(List<string> contents)
        {
            for (int i = 0; i < Schema.MetaData.Count; i++)
                contents.Add(Schema.MetaData[i].Key);
        }
    }
}