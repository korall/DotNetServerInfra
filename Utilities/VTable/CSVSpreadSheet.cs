using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Utility.Log;

namespace Utility.Data
{
    public class EncodeDetect
    {
        public enum Encode
        {
            None = 0x0,             // Unknown or binary
            ANSI = 0x1,             // 0-255
            ASCII = 0x2,                // 0-127
            UTF8_BOM = 0x10,            // UTF8 with BOM
            UTF8_NOBOM = 0x11,          // UTF8 without BOM
            UTF16_LE_BOM = 0x100,       // UTF16 LE with BOM
            UTF16_LE_NOBOM = 0x101,     // UTF16 LE without BOM
            UTF16_BE_BOM = 0x102,       // UTF16-BE with BOM
            UTF16_BE_NOBOM = 0x103,     // UTF16-BE without BOM
        };

        int mBOMSkip;
        Encode mNowDetectedEncode;
        bool mIsAmbiguity;

        public EncodeDetect()
        {
            mNowDetectedEncode = Encode.None;
            mIsAmbiguity = false;
            mBOMSkip = 0;
        }

        public int BOMSkip
        {
            get { return mBOMSkip; }
        }
        public Encode EncodeDectected
        {
            get { return mNowDetectedEncode; }
        }

        public bool IsAmbiguity
        {
            get { return mIsAmbiguity; }
        }

        public void UpdateEncode(byte[] pBuffer, bool isInBegin = true)
        {
            if (isInBegin)
            {
                mIsAmbiguity = true;
                mNowDetectedEncode = Encode.None;
                mBOMSkip = 0;

                Encode encode = CheckBOM(pBuffer, ref mBOMSkip);
                if (encode != Encode.None)
                {
                    mIsAmbiguity = false;
                    mNowDetectedEncode = encode;
                    return;
                }
            }

            if (mIsAmbiguity)
            {
                Encode encode = CheckUTF8(pBuffer);
                if (encode != Encode.None)
                {
                    mNowDetectedEncode = encode;
                    mIsAmbiguity = (mNowDetectedEncode != Encode.ANSI);
                    return;
                }

                mNowDetectedEncode = encode;
                mIsAmbiguity = false;
            }
        }

        public static Encode CheckBOM(byte[] pBuffer, ref int bomSize)
        {
            if (pBuffer.Length >= 2 && pBuffer[0] == 0xFF && pBuffer[1] == 0xFE)
            {
                bomSize = 2;
                return Encode.UTF16_LE_BOM;
            }
            else if (pBuffer.Length >= 2 && pBuffer[0] == 0xFE && pBuffer[1] == 0xFF)
            {
                bomSize = 2;
                return Encode.UTF16_BE_BOM;
            }
            else if (pBuffer.Length >= 3 && pBuffer[0] == 0xEF && pBuffer[1] == 0xBB && pBuffer[2] == 0xBF)
            {
                bomSize = 3;
                return Encode.UTF8_BOM;
            }
            else
            {
                return Encode.None;
            }
        }

        public static Encode CheckUTF8(byte[] pBuffer)
        {
            // UTF8 Valid sequences
            // 0xxxxxxx  ASCII
            // 110xxxxx 10xxxxxx  2-byte
            // 1110xxxx 10xxxxxx 10xxxxxx  3-byte
            // 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx  4-byte
            //
            // Width in UTF8
            // Decimal		Width
            // 0-127		1 byte
            // 194-223		2 bytes
            // 224-239		3 bytes
            // 240-244		4 bytes
            //
            // Subsequent chars are in the range 128-191

            bool only_saw_ascii_range = true;
            int pos = 0;
            int more_chars;

            while (pos < pBuffer.Length)
            {
                var ch = pBuffer[pos++];

                if (ch == 0)
                {
                    return Encode.None;
                }
                else if (ch <= 0x7F)
                {
                    // 1 byte
                    more_chars = 0;
                }
                else if (ch >= 0xC2 && ch <= 0xDF)
                {
                    // 2 Byte
                    more_chars = 1;
                }
                else if (ch >= 0xE0 && ch <= 0xEF)
                {
                    // 3 Byte
                    more_chars = 2;
                }
                else if (ch >= 0xF0 && ch <= 0xF4)
                {
                    // 4 Byte
                    more_chars = 3;
                }
                else
                {
                    return Encode.ANSI;                        // Not utf8
                }

                // Check secondary chars are in range if we are expecting any
                while (more_chars > 0 && pos < pBuffer.Length)
                {
                    only_saw_ascii_range = false;       // Seen non-ascii chars now

                    ch = pBuffer[pos++];
                    if (ch < 128 || ch > 191) // Not utf8
                        return Encode.ANSI;

                    --more_chars;
                }
            }

            // If we get to here then only valid UTF-8 sequences have been processed

            // If we only saw chars in the range 0-127 then we can't assume UTF8 (the caller will need to decide)
            if (only_saw_ascii_range)
                return Encode.ASCII;
            else
                return Encode.UTF8_NOBOM;
        }
    }

    /// <summary>
    /// Seperator       ::= seperator
    /// Quote           ::= """
    /// NormalContent   ::= "[^seperator]"
    /// EscapeContentA  ::= "[^\"]"
    /// EscapeContent   ::= EscapeContentA | Quote Quote EscapeContent
    /// ColContent      ::= NormalContent | Quote EscapeContent Quote | Empty
    /// Row             ::= ColContent {Seperator ColContent}
    /// </summary>
    public class CSVRowParser
    {
        string mRowLine;
        char mSeperator;
        int mNowParsedIndex;
        bool mHaveEscapeQuote;

        bool mIsEscapeCol;
        bool mHaveError;
        string mError;

        string mCurrentColContent;

        const char mQuote = '\"';

        public CSVRowParser(char seperator)
        {
            mRowLine = "";
            mSeperator = seperator;
            mNowParsedIndex = 0;
            mHaveEscapeQuote = false;
            mIsEscapeCol = false;
            mHaveError = false;
            mError = "";
            mCurrentColContent = "";
        }

        public string RowLine
        {
            get
            {
                return mRowLine;
            }
            set
            {
                mRowLine = value;
                mNowParsedIndex = 0;
            }
        }
        public char Seperator
        {
            get
            {
                return mSeperator;
            }
            set
            {
                mSeperator = value;
            }
        }
        public bool HaveError
        {
            get
            {
                return mHaveError;
            }
        }
        public bool IsEscapeCol
        {
            get
            {
                return mIsEscapeCol;
            }
        }
        public string Error
        {
            get
            {
                return mError;
            }
        }

        void _ParseNormalContent()
        {
            while (mNowParsedIndex < mRowLine.Length && mSeperator != mRowLine[mNowParsedIndex])
                mNowParsedIndex++;
        }

        void _ParseEscapeContentA()
        {
            while (mNowParsedIndex < mRowLine.Length && mQuote != mRowLine[mNowParsedIndex])
                mNowParsedIndex++;
        }

        void _ParseEscapeContent()
        {
            _ParseEscapeContentA();

            if (mNowParsedIndex + 1 >= mRowLine.Length || mQuote != mRowLine[mNowParsedIndex + 1])
                return;

            mNowParsedIndex += 2;
            mHaveEscapeQuote = true;

            _ParseEscapeContent();
        }

        void _ParseColContent()
        {
            mIsEscapeCol = false;
            if (mNowParsedIndex >= mRowLine.Length)
                return;

            if (mQuote == mRowLine[mNowParsedIndex])
            {
                mIsEscapeCol = true;
                mNowParsedIndex++;

                int startIndex = mNowParsedIndex;
                mHaveEscapeQuote = false;
                _ParseEscapeContent();

                if (mNowParsedIndex > startIndex)
                {
                    mCurrentColContent = mRowLine.Substring(startIndex, mNowParsedIndex - startIndex);

                    if (mHaveEscapeQuote)
                        mCurrentColContent = mCurrentColContent.Replace("\"\"", "\"");
                }
                else
                {
                    mCurrentColContent = "";
                }

                mNowParsedIndex++;
            }
            else
            {
                int startIndex = mNowParsedIndex;
                _ParseNormalContent();
                if (mNowParsedIndex > startIndex)
                    mCurrentColContent = mRowLine.Substring(startIndex, mNowParsedIndex - startIndex);
                else
                    mCurrentColContent = "";
            }
        }

        public IEnumerable<string> ParseRowColumns()
        {
            while (mNowParsedIndex <= mRowLine.Length)
            {
                mCurrentColContent = "";
                _ParseColContent();
                yield return mCurrentColContent;

                mNowParsedIndex++;  //Seperator
            }
        }
    }

    public class CSVFileTable
    {
        CSVRowParser mParser = new CSVRowParser(',');
        VTable mVTable;
        string mCSVFilePath;

        public VTable Table
        {
            get
            {
                return mVTable;
            }
        }

        public bool SaveDummyRow { get ; set ; }

        public IEnumerable<VTableRow> DataRows
        {
            get
            {
                return mVTable.DataRows;
            }
        }

        public int RowCount
        {
            get
            {
                return mVTable.RowCount;
            }
        }
        public string TableName
        {
            get
            {
                return mVTable.TableName;
            }
        }

        public string CSVFilePath
        { 
            get
            {
                return mCSVFilePath;
            }
        }
        public CSVFileTable()
        {
            List<KeyValuePair<string, VTypeCode>> fieldsList = new List<KeyValuePair<string, VTypeCode>>();
            mVTable = new VTable(new VTableSchema("DynamicScheme", fieldsList));
        }

        public CSVFileTable(string tableName)
        {
            List<KeyValuePair<string, VTypeCode>> fieldsList = new List<KeyValuePair<string, VTypeCode>>();
            mVTable = new VTable(new VTableSchema(tableName, fieldsList));
        }

        // FIELD_TYPE FieldTypeFromVTableTypeCode(VTableTypeCode VTableTypeCode)
        // {
        //     switch(VTableTypeCode)
        //     {
        //     case VTableTypeCode.Boolean:
        //         return FIELD_TYPE.FIELD_BOOL;
        //     case VTableTypeCode.SByte:
        //         return FIELD_TYPE.FIELD_BYTE;
        //     case VTableTypeCode.Int16:
        //         return FIELD_TYPE.FIELD_SHORT;
        //     case VTableTypeCode.Int32:
        //         return FIELD_TYPE.FIELD_INT;
        //     case VTableTypeCode.Int64:
        //         return FIELD_TYPE.FIELD_UINT64;
        //     case VTableTypeCode.Single:
        //         return FIELD_TYPE.FIELD_FLOAT;
        //     case VTableTypeCode.Double:
        //         return FIELD_TYPE.FIELD_DOUBLE;
        //     default:
        //         return FIELD_TYPE.FIELD_STRING;
        //     }
        // }

        public VTableRow GetRowByKey(string key)
        {
            return mVTable.GetRowByKey(key);
        }

        public VTableRow GetRowByKey(int key)
        {
            return mVTable.GetRowByKey(key);
        }

        public VTableRow GetRowByKey(VTableCellData key)
        {
            return mVTable.GetRowByKey(key);
        }

        public VTableRow GetLastRow()
        {
            return mVTable.GetLastDataRow();
        }

        public VTableRow GetFirstRow()
        {
            return mVTable.GetFirstDataRow();
        }

        public bool SetMainIndexField(string fieldName)
        {
            if (mVTable == null)
                return false;

            return mVTable.SetMainIndex(fieldName);
        }

        public bool SetMainIndexField(int colIndex)
        {
            if (mVTable == null)
                return false;

            return mVTable.SetMainIndex(colIndex);
        }

        public void AppendField(string fieldName, VTypeCode fieldType, int colIndex)
        {
            mVTable.Schema.AppendField(fieldName, fieldType, colIndex);
        }

        public void AppendField(string fieldName, VTypeCode fieldType)
        {
            mVTable.Schema.AppendField(fieldName, fieldType, mVTable.Schema.ColumnCount);
        }

        public VTableRow NewEmptyRecord()
        {
            if (mVTable == null)
                return null;

            var result = mVTable.BuildRow();
            if (result != null)
                mVTable.AppendAsDummyRow(result);

            return result;
        }

        public VTableRow CreateRowByKey(string key)
        {
            if (mVTable == null)
                return null;

            if (mVTable.Schema.MainIndexCol < 0)
                return null;

            var result = mVTable.BuildRow();
            if (result != null)
            {
                result[mVTable.Schema.MainIndexCol] = key;
                int rowIndex = mVTable.AddRowByKey(key, result);
                if (rowIndex < 0)
                    return null;
            }

            return result;
        }

        public VTableRow CreateRowByKey(VTableCellData key)
        {
            if (mVTable == null)
                return null;

            if (mVTable.Schema.MainIndexCol < 0)
                return null;
            var result = mVTable.BuildRow();
            if (result != null)
            {
                result[mVTable.Schema.MainIndexCol] = key;
                int rowIndex = mVTable.AddRowByKey(key, result);
                if (rowIndex < 0)
                    return null;
            }

            return result;
        }

        bool _ParseCsvLine(string lineStr, List<string> destList, char seperator, int colCount)
        {
            mParser.Seperator = seperator;
            mParser.RowLine = lineStr;

            int colIndex = 0;
            if (destList.Count < colCount)
                for (int i = destList.Count; i < colCount; i++)
                    destList.Add("");

            foreach (var colContent in mParser.ParseRowColumns())
            {
                if (mParser.HaveError)
                    return false;

                if (colIndex >= colCount)
                    break;

                destList[colIndex] = colContent;
                colIndex++;
            }

            for (int i = colIndex; i < colCount; i++)
                destList[colIndex] = "";

            return true;
        }

        void _SaveTableToCSV(FileStream fs, Encoding encoding, char seprateNotation)
        {
            if (mVTable == null)
                return;

            using (StreamWriter writer = new StreamWriter(fs, encoding))
            {
                List<string> temp = new List<string>();
                mVTable.SerializeTypeToString(temp);

                for (int i = 0; i < temp.Count; i++)
                {
                    writer.Write(temp[i]);
                    if (i < temp.Count - 1)
                        writer.Write(seprateNotation);
                }

                writer.Write("\r\n");

                temp.Clear();
                mVTable.SerializeFieldsToString(temp);

                for (int i = 0; i < temp.Count; i++)
                {
                    writer.Write(temp[i]);
                    if (i < temp.Count - 1)
                        writer.Write(seprateNotation);
                }

                writer.Write("\r\n");

                var rows = mVTable.AllRows;
                if (!SaveDummyRow)
                    rows = mVTable.DataRows;

                foreach (var row in rows)
                {
                    temp.Clear();
                    row.SerializeToString(temp);

                    for (int i = 0; i < temp.Count; i++)
                    {
                        string value = temp[i];
                        bool needEscape = value.IndexOf(seprateNotation) >= 0;
                        if (needEscape)
                        {
                            writer.Write('"');
                            writer.Write(value.Replace("\"", "\"\""));
                            writer.Write('"');
                        }
                        else
                        {
                            writer.Write(value);
                        }

                        if (i < temp.Count - 1)
                            writer.Write(seprateNotation);
                    }
                    writer.Write("\r\n");
                }
                writer.Close();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePathName"> 绝对路径</param>
        /// <param name="encoding"></param>
        /// <param name="speratNotion"></param>
        public void SaveTableToCSV(string filePathName, Encoding encoding, char speratNotion = ',')
        {
            try
            {
                FileStream fs = new FileStream(filePathName, FileMode.OpenOrCreate);
                fs.SetLength(0);        //discard old data
                _SaveTableToCSV(fs, encoding, speratNotion);
            }
            catch (Exception e)
            {
                Debug.LogError($"Create table file: \"{filePathName}\"  failed, Error: {e}");
            }
        }

        string _UniformToUtf8String(string origin, Encoding encoding)
        {
            if (encoding != Encoding.UTF8)
            {
                var byData = encoding.GetBytes(origin);
                var utf8 = Encoding.GetEncoding("utf-8");
                byData = Encoding.Convert(encoding, utf8, byData);
                origin = utf8.GetString(byData);
            }
            return origin;
        }

        VTable _LoadVTableFromCSV(string tableName, FileStream fs, Encoding encoding, char seprateNotation)
        {
            VTableSchema scheme;
            VTable result = null;

            using (StreamReader reader = new StreamReader(fs, encoding))
            {
                string s = "";
                if ((s = reader.ReadLine()) == null)
                    return null;

                mParser.Seperator = seprateNotation;
                mParser.RowLine = s;

                List<string> typeList = new List<string>();
                foreach (var colContent in mParser.ParseRowColumns())
                {
                    if (mParser.HaveError)
                        return null;

                    typeList.Add(colContent);
                }

                if ((s = reader.ReadLine()) == null)
                    return null;

                mParser.RowLine = s;
                List<string> fieldList = new List<string>();
                foreach (var colContent in mParser.ParseRowColumns())
                {
                    if (mParser.HaveError)
                        return null;

                    fieldList.Add(colContent);
                }

                if (fieldList.Count != typeList.Count)
                    return null;

                List<KeyValuePair<string, VTypeCode>> fieldsList = new List<KeyValuePair<string, VTypeCode>>();

                for (int i = 0; i < typeList.Count; i++)
                    fieldsList.Add(new KeyValuePair<string, VTypeCode>(fieldList[i], VTableSchema.DeserializeVTableTypeCode(typeList[i])));

                scheme = new VTableSchema(tableName, fieldsList, 0);
                result = new VTable(scheme);

                var dataList = new List<string>(fieldsList.Count);
                VTableRow newRow = null;
                while ((s = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(s))
                    {
                        newRow = result.BuildRow();
                        result.AppendAsDummyRow(newRow);
                        continue;
                    }

                    s = _UniformToUtf8String(s, encoding);
                    if (!_ParseCsvLine(s, dataList, seprateNotation, scheme.ColumnCount))
                        return result;

                    newRow = result.BuildRow();
                    newRow.DeserializeFromStrings(dataList);
                    bool isDuplicated = false;
                    if (scheme.MainIndexCol >= 0)
                    {
                        string key = dataList[scheme.MainIndexCol];
                        if (key != "")
                        {
                            if (result.GetRowByKey(key) != null)
                            {
                                Debug.LogWarning($"csv table error: <{tableName}> Row [\"{key}\"]: duplicated!!!");
                                isDuplicated = true;
                            }
                        }
                    }

                    if (!isDuplicated)
                        result.AttachRow(newRow);
                    if (!newRow.IsDummyRow && scheme.MainIndexCol >= 0)
                    {
                        string key = dataList[scheme.MainIndexCol];
                        for (int i = 0; i < newRow.DeserializingErrorLog.Count; i++)
                            Debug.LogWarning($"csv table error: <{tableName}> Row [\"{key}\"]: {newRow.DeserializingErrorLog[i]}");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 注意：UTF-16 编码的CSV，默认是用制表符分隔字段的, Excel 支持显示(其它分隔符 Excel 不支持)；
        ///       UTF-8  编码的CSV, 在 Excel 中会显示乱码
        /// </summary>
        /// <param name="filePathName"></param>
        /// <param name="encoding"></param>
        /// <param name="speratNotion"></param>
        public void LoadFromCSVFile(string filePathName, Encoding encoding, char speratNotion = ',')
        {
            try
            {
                using (var fs = new FileStream(filePathName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    string tableName = Path.GetFileNameWithoutExtension(filePathName);
                    var tempTable = _LoadVTableFromCSV(tableName, fs, encoding, speratNotion);
                    if (tempTable != null)
                    {
                        mVTable = tempTable;
                        mCSVFilePath = filePathName;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"load csv spreadsheet table file: \"{filePathName}\" failed, Error: {e}");
            }
        }

        public void LoadFromCSVFile(string filePathName)
        {
            var encodins = Encoding.GetEncodings();
            
            Encoding fileEncod = Encoding.Default;// Encoding.GetEncoding("GBK"); //
            char seperateNotion = ',';

            using (var fs = new FileStream(filePathName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                EncodeDetect encodeDectector = new EncodeDetect();
                var fileSize = fs.Length;
                if (fileSize >= 4)
                {
                    var bytes = new byte[4];
                    fs.Read(bytes, 0, 4);
                    encodeDectector.UpdateEncode(bytes, true);
                    while (encodeDectector.IsAmbiguity && fs.Read(bytes, 0, 4) > 0)
                        encodeDectector.UpdateEncode(bytes, false);

                    if (!encodeDectector.IsAmbiguity)
                    {
                        if ((encodeDectector.EncodeDectected & EncodeDetect.Encode.UTF16_LE_BOM) > 0)
                            fileEncod = Encoding.Unicode;
                        else if ((encodeDectector.EncodeDectected & EncodeDetect.Encode.UTF8_BOM) > 0)
                            fileEncod = Encoding.UTF8;
                        else if ((encodeDectector.EncodeDectected & EncodeDetect.Encode.ASCII) > 0)
                            fileEncod = Encoding.ASCII;
                    }
                }
            }

            if (fileEncod == Encoding.Unicode)
                seperateNotion = '\t';

            LoadFromCSVFile(filePathName, fileEncod, seperateNotion);
        }
    }
}