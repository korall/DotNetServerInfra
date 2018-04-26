using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using Utility.Data;
using Utility.Log;
using System.Text;
using Utility;

namespace AppSystem.CSVConfig
{
    public class CSVConfigInfo
    {
        Dictionary<string, CSVFileTable> mCSVCfgTables = new Dictionary<string, CSVFileTable>();
        string mDirectoryPathName;

        public Dictionary<string, CSVFileTable> CSVCfgTables => mCSVCfgTables;

        public string DirectoryPathName { get { return mDirectoryPathName; } set { mDirectoryPathName = value; } }

        public CSVFileTable GetCsvCfgTable(string cfgName)
        {
            cfgName = cfgName.ToLower();
            if (mCSVCfgTables.ContainsKey(cfgName))
                return mCSVCfgTables[cfgName];

            return null;
        }

        public CSVFileTable LoadCSVIntoConfig(string cfgName, string csvFile)
        {
            cfgName = cfgName.ToLower();
            CSVFileTable csvTable = new CSVFileTable(cfgName);
            csvTable.LoadFromCSVFile(csvFile);
            if (!mCSVCfgTables.TryAdd(cfgName, csvTable))
            {
                mCSVCfgTables.Remove(cfgName);
                mCSVCfgTables.Add(cfgName, csvTable);
            }

            return csvTable;
        }

    }

    public class CSVCfgManager : Singleton<CSVCfgManager>
    {
        Dictionary<string, CSVConfigInfo> mConfigs = new Dictionary<string, CSVConfigInfo>();

        public CSVConfigInfo GetConfigInfo(string cfgTagName)
        {
            var key = cfgTagName;
            if (mConfigs.ContainsKey(key))
                return mConfigs[key];

            return null;
        }

        void _LoadCSVConfigsInDir(string preCfgTagName, string cfgDiretory, CSVConfigInfo csvConfig)
        {
            var cfgInfo = csvConfig;
            var files = Directory.GetFiles(cfgDiretory);
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var fileExt = Path.GetExtension(file);
                if (string.IsNullOrEmpty(fileExt) || fileExt.ToLower() != ".csv")
                    continue;

                var configName = Path.GetRelativePath(cfgDiretory, file);
                if (configName.StartsWith('.'))
                    continue;

                if (!string.IsNullOrEmpty(preCfgTagName))
                {
                    configName = Path.Combine(preCfgTagName, configName);
                    configName = configName.Replace('\\', '/');
                }

                configName = configName.Substring(0, configName.LastIndexOf(fileExt));
                Log.WriteLog(eLogLevel.LOG_INFO, "CSVCfgManager", $" loading csv config [{configName}], file \"{file}\" ...");
                try
                {
                    var csvTable = cfgInfo.LoadCSVIntoConfig(configName, file);
                    Log.WriteLog(eLogLevel.LOG_INFO, "CSVCfgManager", $" loading csv config [{configName}] done, {csvTable.RowCount} rows data loaded.");

                }
                catch (Exception ex)
                {
                    Log.WriteLog(eLogLevel.LOG_ERROR, "CSVCfgManager", $" loading csv file \"{file}\" failed: {ex.Message}");
                    throw;
                }
            }
        }

        void _LoadCSVConfig(string preCfgTagName, string cfgDiretory, CSVConfigInfo csvConfig)
        {
            _LoadCSVConfigsInDir(preCfgTagName, cfgDiretory, csvConfig);

            var dirs = Directory.GetDirectories(cfgDiretory);
            for (int i = 0; i < dirs.Length; i++)
            {
                var currentDirName = dirs[i];
                currentDirName = Path.GetRelativePath(cfgDiretory, currentDirName);
                var preTagName = string.IsNullOrEmpty(preCfgTagName) ? currentDirName : Path.Combine(preCfgTagName, currentDirName);
                preTagName = preTagName.Replace('\\', '/');
                _LoadCSVConfig(preTagName, dirs[i], csvConfig);
            }
        }

        public void LoadCSVConfig(string cfgTagName, string cfgDiretory)
        {
            Log.WriteLog(eLogLevel.LOG_INFO, "CSVCfgManager", $"\n\t======= Loading csv config:[{cfgTagName}] from directory \"{cfgDiretory}\" =========");

            if (!mConfigs.ContainsKey(cfgTagName))
                mConfigs.Add(cfgTagName, new CSVConfigInfo() { DirectoryPathName = cfgDiretory });

            var cfgInfo = mConfigs[cfgTagName];
            _LoadCSVConfig(null, cfgDiretory, cfgInfo);
        }

        #region Export as json
        Dictionary<string, Dictionary<string, bool>> _LoadExportJsonCfg(string fromPath)
        {
            var exportCfgName = Path.Combine(fromPath, ".exportjson.csv");
            if (!File.Exists(exportCfgName))
                return null;

            CSVFileTable csvTable = new CSVFileTable();
            csvTable.LoadFromCSVFile(exportCfgName);

            if (csvTable.RowCount <= 0)
                return null;

            bool isIncludeMode = false;
            Dictionary<string, Dictionary<string, bool>> result = new Dictionary<string, Dictionary<string, bool>>();
            foreach(var row in csvTable.DataRows)
            {
                var cfgFile = row["CONFIG_FILE"];
                if (result.ContainsKey(cfgFile))
                {
                    Log.WriteLog(eLogLevel.LOG_ERROR, "CSVCfgManager", $"Export setting: duplicated cfg file {cfgFile} at paht {fromPath}");
                    continue;
                }

                Dictionary<string, bool> setting = null;
                var includes = (string)row["INCLUDED_FILEDS"];
                if (!string.IsNullOrEmpty(includes))
                {
                    isIncludeMode = true;
                    setting = new Dictionary<string, bool>();
                    var fileds = includes.Split(';');
                    for (int i = 0; i < fileds.Length; i++)
                        if (!string.IsNullOrEmpty(fileds[i]) && !setting.ContainsKey(fileds[i]))
                            setting.Add(fileds[i], isIncludeMode);
                    if (setting.Count > 0)
                        result.Add(cfgFile, setting);
                    else
                        result.Add(cfgFile, null);
                    continue;
                }

                var excludes = (string)row["INCLUDED_FILEDS"];
                if (!string.IsNullOrEmpty(includes))
                {
                    isIncludeMode = false;
                    setting = new Dictionary<string, bool>();
                    var fileds = includes.Split(';');
                    for (int i = 0; i < fileds.Length; i++)
                        if (!string.IsNullOrEmpty(fileds[i]) && !setting.ContainsKey(fileds[i]))
                            setting.Add(fileds[i], isIncludeMode);
                    if (setting.Count > 0)
                        result.Add(cfgFile, setting);
                    else
                        result.Add(cfgFile, null);
                    continue;
                }

                result.Add(cfgFile, null);
            }
            return result;
        }
        public void ExportAsJson(string cfgName, string exportPath)
        {
            var configInfo = GetConfigInfo(cfgName);
            if (configInfo == null)
                return;

            var prefix = "";
            var cfgDir = configInfo.DirectoryPathName;

            Dictionary<string, Dictionary<string, Dictionary<string, bool>>> exportSettings = new Dictionary<string, Dictionary<string, Dictionary<string, bool>>>();

            foreach (var kv in configInfo.CSVCfgTables)
            {
                var spI = kv.Key.LastIndexOf('/');
                prefix = spI > 0 ? kv.Key.Substring(0, spI) : "";

                var settingKey = cfgName;
                Dictionary<string, Dictionary<string, bool>> exportSetting = null;
                if (!string.IsNullOrEmpty(prefix))
                    settingKey = prefix;

                cfgDir = Path.Combine(configInfo.DirectoryPathName, prefix);
                if (!exportSettings.ContainsKey(settingKey))
                {
                    exportSetting = _LoadExportJsonCfg(cfgDir);
                    if (exportSetting == null)
                        continue;
                    exportSettings.Add(settingKey, exportSetting);
                }
                else
                {
                    exportSetting = exportSettings[settingKey];
                }

                var csvCfg = kv.Value;
                var csvCfgFileName = Path.GetFileNameWithoutExtension(csvCfg.CSVFilePath);
                if (!exportSetting.ContainsKey(csvCfgFileName))
                    continue;
                
                var exportFilePath = $"{Path.Combine(exportPath, kv.Key)}.json";
                var exportFileName = Path.GetFileName(exportFilePath);
                var dir = Path.GetDirectoryName(exportFilePath);

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var export = exportSetting[csvCfgFileName];
                var jsonStr = csvCfg.Table.ToJson(export);
                var x = jsonStr.Length;
                File.WriteAllText(exportFilePath, jsonStr);
            }
        }
        #endregion

        #region Write back to csv and backup old virsion

        void _EnsureDirectory(string path)
        {
            var tempDir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(tempDir))
                return;
            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);
        }

        public void WriteBackCSVConfig(CSVFileTable cfg, string directoryPath = null, string newCfgDir = null)
        {
            var originFilePath = cfg.CSVFilePath;

            if (string.IsNullOrEmpty(directoryPath))
                directoryPath = Path.GetDirectoryName(originFilePath);

            var tempFileName = $"{originFilePath}.temp.csv";
            if (File.Exists(tempFileName))
                File.Delete(tempFileName);

            _EnsureDirectory(tempFileName);

            cfg.SaveDummyRow = true;
            cfg.SaveTableToCSV(tempFileName, Encoding.Unicode, '\t');
            if (!File.Exists(tempFileName))
                return;

            if (string.IsNullOrEmpty(newCfgDir) && File.Exists(originFilePath))
            {
                var expend = originFilePath.MD5OfFile();
                var backup = $"{originFilePath}.{expend}.bak";

                if (!File.Exists(backup))
                    File.Move(originFilePath, backup);
            }

            if (!string.IsNullOrEmpty(newCfgDir))
            {
                originFilePath = Path.GetRelativePath(directoryPath, originFilePath);
                originFilePath = Path.Combine(newCfgDir, originFilePath);
            }

            _EnsureDirectory(originFilePath);
            if (File.Exists(originFilePath))
                File.Delete(originFilePath);
            File.Move(tempFileName, originFilePath);
            File.Delete(tempFileName);
        }

        public void WriteBackCSVConfig(string cfgTagName, string cfgName, string newCfgDir = null)
        {
            var cfgInfo = GetConfigInfo(cfgTagName);
            if (cfgInfo == null)
                return;

            var cfg = cfgInfo.GetCsvCfgTable(cfgName);
            if (cfg == null)
                return;

            WriteBackCSVConfig(cfg, cfgInfo.DirectoryPathName, newCfgDir);
        }

        #endregion
    }
}