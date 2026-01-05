using System;
using System.IO;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using IWshRuntimeLibrary; // 需要引用Windows Script Host Object Model（COM）
using System.Text.RegularExpressions;
using System.IO.Compression;

namespace windowOP
{
    class FileManager
    {

        public static string? GetIndex()
        {
            JObject resultObject = new JObject();

            JArray diskInfoArray = new JArray();

            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();

                foreach (DriveInfo drive in drives)
                {
                    if (drive.IsReady) // 确保驱动器是准备好的状态
                    {
                        string driveName = drive.VolumeLabel;  // 磁盘的卷标 (例如：“本地磁盘”)
                        string driveLetter = drive.Name; // 磁盘的驱动器字母 (例如："C:")
                        if (string.IsNullOrWhiteSpace(driveName)) driveName = "本地磁盘"; // 可以设置成其它默认值，例如 "无标签"

                        string displayDriveName = $"{driveName} ({driveLetter})";

                        JObject diskInfo = new JObject
                        {
                            ["Name"] = displayDriveName, // 去掉尾部的反斜杠
                            ["Type"] = "/disk",
                            ["Size"] = $"{drive.TotalFreeSpace / (1024 * 1024 * 1024)}GB可用/{drive.TotalSize / (1024 * 1024 * 1024)}GB",
                            ["Time"] = "", // 因为磁盘没有具体时间，可以留空
                            ["Lnk"] = drive.Name // 磁盘的根路径
                        };
                        diskInfoArray.Add(diskInfo);
                    }
                }

                // 获取特殊文件夹路径
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";




                // 将特殊文件夹信息添加到 JSON 对象中
        List<JObject> specialFolders = new List<JObject>
        {
            new JObject
            {
                ["Name"] = "桌面（Desktop）",
                ["Type"] = "lnk",
                ["Size"] = "", // 可以选填，通常文件夹没有大小
                ["Time"] = "", // 可以选填
                ["Lnk"] = desktopPath
            },
            new JObject
            {
                ["Name"] = "应用程序数据（AppData）",
                ["Type"] = "lnk",
                ["Size"] = "",
                ["Time"] = "",
                ["Lnk"] = appDataPath
            },
            new JObject
            {
                ["Name"] = "启动文件夹（Startup）",
                ["Type"] = "lnk",
                ["Size"] = "",
                ["Time"] = "",
                ["Lnk"] = startupPath
            },
            new JObject
            {
                ["Name"] = "文档文件夹（Document）",
                ["Type"] = "lnk",
                ["Size"] = "",
                ["Time"] = "",
                ["Lnk"] = documentsPath
            },
            new JObject
            {
                ["Name"] = "下载文件夹（Download）--可能不可靠",
                ["Type"] = "lnk",
                ["Size"] = "",
                ["Time"] = "",
                ["Lnk"] = downloadsPath
            },
            new JObject
            {
                ["Name"] = "程序文件夹（windowOP）",
                ["Type"] = "lnk",
                ["Size"] = "",
                ["Time"] = "",
                ["Lnk"] = Setting.programDir
            }

        };

                foreach (var folder in specialFolders)
                {
                    diskInfoArray.Add(folder);
                }

                resultObject["Path"] = new JArray();
                resultObject["Content"] = diskInfoArray;
                return resultObject.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取文件主目录出现错误: {ex}");
                // 可以选择抛出异常或处理错误
                return null;
            }
        }


        public static string? GetInfo(string path)
        {
            JObject resultObject = new JObject();
            JArray fileInfoArray = new JArray();

            if (!Directory.Exists(path)) return null;


            try
            {
                // 获取文件夹信息
                var directories = Directory.GetDirectories(path);
                foreach (var dir in directories)
                {
                    var dirInfo = new DirectoryInfo(dir);
                    JObject directoryInfo = new JObject
                    {
                        ["Name"] = dirInfo.Name,
                        ["Type"] = "dir",
                        ["Size"] = 0, // 文件夹大小可以设置为0
                        ["Time"] = dirInfo.LastWriteTime.ToString("yyyy-MM-dd-HH-mm-ss")
                    };

                    // 检查是否为快捷方式
                    string shortcutTarget = GetShortcutTarget(dir);
                    if (!string.IsNullOrEmpty(shortcutTarget))
                    {
                        directoryInfo["Lnk"] = shortcutTarget;
                    }

                    fileInfoArray.Add(directoryInfo);
                }

                // 获取文件信息
                var files = Directory.GetFiles(path);
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    JObject fileInfoObject = new JObject
                    {
                        ["Name"] = fileInfo.Name,
                        ["Type"] = fileInfo.Extension.TrimStart('.'),
                        ["Size"] = fileInfo.Length,
                        ["Time"] = fileInfo.LastWriteTime.ToString("yyyy-MM-dd-HH-mm-ss")
                    };

                    // 检查是否为快捷方式
                    string shortcutTarget = GetShortcutTarget(file);
                    if (!string.IsNullOrEmpty(shortcutTarget))
                    {
                        fileInfoObject["Lnk"] = shortcutTarget;
                    }

                    fileInfoArray.Add(fileInfoObject);
                }


                JArray pathArray = new JArray();

                // 拆分路径
                string[] pathParts = Path.GetFullPath(path)
                      .TrimEnd(Path.DirectorySeparatorChar)
                      .Split(Path.DirectorySeparatorChar);

                // 将路径部分添加到路径数组
                foreach (var part in pathParts)
                {
                    if (!string.IsNullOrEmpty(part))
                    {
                        pathArray.Add(part);
                    }
                }
                // 创建包含路径和内容的JSON对象
                resultObject["Path"] = pathArray;
                resultObject["Content"] = fileInfoArray;

                return resultObject.ToString();
            }
            catch (Exception e)
            {
                DatabaseOP.LogErr("获取文件夹信息时出现错误：" + e.ToString());
                return null;
            }
        }

        private static string? GetShortcutTarget(string shortcutPath)
        {
            if (Path.GetExtension(shortcutPath).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    WshShell shell = new WshShell();
                    IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
                    return shortcut.TargetPath; // 返回目标路径
                }
                catch (Exception)
                {
                    // 如果获取目标路径失败，返回 null
                    return null;
                }
            }

            return null;
        }

        public static (int SucNum, string? FailInfo) Delete(string DeleteList)
        {

            List<string> filePaths = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(DeleteList);

            if (filePaths == null) return (0, "待删除列表格式不正确");

            int successfulDeletions = 0; // 统计成功删除的文件数量
            string failedDeletions = "";
            foreach (var path in filePaths)
            {
                try
                {
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                        Console.WriteLine($"Successfully deleted: {path}");
                        successfulDeletions++;
                    }
                    else if (Directory.Exists(path))
                    {
                        // 删除文件夹及其内容
                        Directory.Delete(path, true); // 第二个参数为 true，表示递归删除
                        successfulDeletions++;
                    }
                    else
                    {
                        failedDeletions += $"{path} - 路径不存在 ";
                    }

                }
                catch (Exception ex)
                {
                    failedDeletions += $"{path} - {ex.Message} "; // 记录失败的文件及其错误信息
                }
            }

            return (successfulDeletions, failedDeletions);
        }


        public static string Rename(string path, string newName)
        {
            // 检查输入参数是否为空
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(newName))
            {
                return "路径或新名称不能为空"; // 提示用户路径或新名称不能为 null 或空白
            }

            // 检查新名称是否符合命名规范
            if (!IsValidFileName(newName))
            {
                return "新名称不符合命名规范"; // 提示用户新名称不符合规范
            }

            // 尝试重命名文件或文件夹
            try
            {
                string directory = Path.GetDirectoryName(path); // 获取原始路径的目录
                string newFullPath = Path.Combine(directory, newName); // 生成新的完整路径

                // 检查路径是否是文件
                if (System.IO.File.Exists(path))
                {
                    // 判断目标路径是否已存在
                    if (!System.IO.File.Exists(newFullPath))
                    {
                        System.IO.File.Move(path, newFullPath); // 重命名文件
                        return "文件重命名成功"; // 返回成功信息
                    }
                    else
                    {
                        return "目标文件已存在"; // 目标文件已存在
                    }
                }
                // 检查路径是否是文件夹
                else if (Directory.Exists(path))
                {
                    // 判断目标路径是否已存在
                    if (!Directory.Exists(newFullPath))
                    {
                        Directory.Move(path, newFullPath); // 重命名文件夹
                        return "文件夹重命名成功"; // 返回成功信息
                    }
                    else
                    {
                        return "目标文件夹已存在"; // 目标文件夹已存在
                    }
                }

                return "路径不存在"; // 既不是文件也不是文件夹时返回
            }
            catch (UnauthorizedAccessException)
            {
                return "权限不足，无法重命名"; // 捕获权限不足的异常
            }
            catch (IOException ex)
            {
                return $"I/O 错误: {ex.Message}"; // 捕获 I/O 错误并返回错误信息
            }
            catch (Exception ex)
            {
                return $"发生未知错误: {ex.Message}"; // 捕获其它任何异常并返回错误信息
            }
        }

        // 检查文件名是否符合命名规范
        private static bool IsValidFileName(string fileName)
        {
            // Windows 文件名不允许包含这些字符
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                if (fileName.Contains(c))
                {
                    return false; // 包含无效字符
                }
            }

            // 文件名不能以空白、点（.）开头或结尾
            return !(string.IsNullOrWhiteSpace(fileName) || fileName.Length == 0 || fileName.StartsWith(".") || fileName.EndsWith("."));
        }

        public static (int SucNum, string FailMsg) Copy(string resPaths, string targetPath, bool cut)
        {
            int SucNum = 0;
            string FailMsg = "";
            var resPathList = JArray.Parse(resPaths).ToObject<string[]>();
            foreach (var resPath in resPathList)
            {
                try
                {
                    if (Directory.Exists(resPath))
                    {
                        // 复制文件夹
                        CopyDirectory(resPath, targetPath, cut);
                    }
                    else if (System.IO.File.Exists(resPath))
                    {
                        // 复制文件
                        CopyFile(resPath, targetPath, cut);
                    }
                    else 
                    {
                        FailMsg += $"找不到文件/文件夹：{resPath}";
                    }
                    SucNum++;
                }
                catch (Exception ex)
                {
                    string f = $"{resPath}: {ex.Message}";
                    DatabaseOP.LogErr(f);
                    FailMsg += f;
                }

            }
            
            return (SucNum, FailMsg);

        }

        static void CopyDirectory(string sourceDir, string targetDir, bool cut)
        {
            var dirName = new DirectoryInfo(sourceDir).Name;
            var newDirPath = Path.Combine(targetDir, dirName);

            // 创建目标目录
            Directory.CreateDirectory(newDirPath);

            // 复制文件
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                CopyFile(file, newDirPath, cut);
            }

            // 递归复制子目录
            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                CopyDirectory(directory, newDirPath, cut);
            }

            // 如果是剪切操作并且不需要保留源目录，可以删除源目录
            if (cut)
            {
                Directory.Delete(sourceDir, true);
            }
        }

        static void CopyFile(string sourceFile, string targetDir, bool cut)
        {
            var fileName = Path.GetFileName(sourceFile);
            var destFile = Path.Combine(targetDir, fileName);

            // 复制文件
            System.IO.File.Copy(sourceFile, destFile, true);

            // 如果是剪切操作，删除源文件
            if (cut)
            {
                System.IO.File.Delete(sourceFile);
            }
        }

        public static string? ZipFiles(JArray itemsToCompress, string zipFilePath)
        {
            try
            {
                // 确保目标路径的文件夹存在
                var zipFileDirectory = Path.GetDirectoryName(zipFilePath);
                if (zipFileDirectory != null && !Directory.Exists(zipFileDirectory))
                {
                    Directory.CreateDirectory(zipFileDirectory);
                }

                // 创建并打开 ZIP 文件
                using (var zipFileStream = new FileStream(zipFilePath, FileMode.Create))
                using (var zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Create))
                {
                    foreach (var item in itemsToCompress)
                    {
                        string itemPath = item.ToString();

                        // 检查路径是否存在
                        if (System.IO.File.Exists(itemPath))
                        {
                            // 如果是文件，添加到 ZIP 中
                            zipArchive.CreateEntryFromFile(itemPath, Path.GetFileName(itemPath));
                        }
                        else if (Directory.Exists(itemPath))
                        {
                            // 如果是目录，递归添加文件
                            AddDirectoryToZip(zipArchive, itemPath, Path.GetFileName(itemPath));
                        }
                        else
                        {
                            Console.WriteLine($"Warning: {itemPath} does not exist and will be skipped.");
                        }
                    }
                }
            }
            catch(Exception e) { return e.Message; }
            return null;
        }

        private static void AddDirectoryToZip(ZipArchive zipArchive, string sourceDir, string entryName)
        {
            foreach (var filePath in Directory.GetFiles(sourceDir))
            {
                zipArchive.CreateEntryFromFile(filePath, Path.Combine(entryName, Path.GetFileName(filePath)));
            }

            foreach (var subDirectory in Directory.GetDirectories(sourceDir))
            {
                AddDirectoryToZip(zipArchive, subDirectory, Path.Combine(entryName, Path.GetFileName(subDirectory)));
            }
        }

    }
}


