using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ImageFolderFixer.Logic
{
    public class FixerLogic
    {        

        public event EventHandler<EventArgs<string>> NotifyMessage;
        private void OnNotifyMessage(string message)
        {
            NotifyMessage?.Invoke(this, new EventArgs<string>(message));
        }
        public event EventHandler<EventArgs<int>> NotifyMaxCount;
        private void OnNotifyMaxCount(int maxCount)
        {
            NotifyMaxCount?.Invoke(this, new EventArgs<int>(maxCount));
        }
        public event EventHandler NotifyProcessed;
        private void OnNotifyProcessed()
        {
            NotifyProcessed?.Invoke(this, new EventArgs());
        }

        public bool Execute(FixerLogicParameters parameters)
        {
            if (!System.IO.Directory.Exists(parameters.InputDirectory))
            {
                OnNotifyMessage($"Input directory {parameters.InputDirectory} does not exist.");
                return false;
            }
            if (!System.IO.Directory.Exists(parameters.OutputDirectory))
            {
                OnNotifyMessage($"Output directory {parameters.OutputDirectory} does not exist.");
                return false;
            }

            var inputDirectory = new System.IO.DirectoryInfo(parameters.InputDirectory);

            var files = EnumerateFiles(parameters, inputDirectory);

            var imageFixInfos = files
                .Select(GetImageFixInfo)
                .OrderBy(o => o.DateTaken)
                .ToList();
            if (imageFixInfos.Count == 0)
            {
                OnNotifyMessage("No images to process.");
                return false;
            }
            OnNotifyMaxCount(imageFixInfos.Count);

            var imageFixInfosByYear = imageFixInfos
                .GroupBy(o => o.Year);

            var outputDirectory = new System.IO.DirectoryInfo(parameters.OutputDirectory);

            int totalCount = 0;
            foreach (var yearGroup in imageFixInfosByYear)
            {
                ExecuteYearGroup(parameters, outputDirectory, yearGroup, ref totalCount);
            }

            OnNotifyMessage($"Copied {totalCount} total files.");

            return true;
        }
        private ImageFixInfo GetImageFixInfo(System.IO.FileInfo fileInfo)
        {
            switch (fileInfo.Extension.ToUpperInvariant())
            {
                case "JPG":
                case "JPEG":
                    return GetJpegImageFixInfo(fileInfo);
                default:
                    return GetDefaultImageFixInfo(fileInfo);
            }
        }
        private ImageFixInfo GetJpegImageFixInfo(System.IO.FileInfo fileInfo)
        {
            using (var fs = new System.IO.FileStream(fileInfo.FullName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
            {
                var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(fs, System.Windows.Media.Imaging.BitmapCreateOptions.DelayCreation, System.Windows.Media.Imaging.BitmapCacheOption.OnDemand);
                var image = decoder.Frames[0];
                var md = (System.Windows.Media.Imaging.BitmapMetadata)image.Metadata;

                //Use Exif when possible.
                if (md.ContainsQuery("System.Photo.DateTaken"))
                {
                    var dateTakenFileTime = (System.Runtime.InteropServices.ComTypes.FILETIME)md.GetQuery("System.Photo.DateTaken");
                    var dateTaken = DateTime.FromFileTime((((long)dateTakenFileTime.dwHighDateTime) << 32) + (uint)dateTakenFileTime.dwLowDateTime);
                    return new ImageFixInfo
                    {
                        FileInfo = fileInfo,
                        DateTaken = dateTaken
                    };
                }
                else
                {
                    return GetDefaultImageFixInfo(fileInfo);
                }

            }
        }
        private ImageFixInfo GetDefaultImageFixInfo(System.IO.FileInfo fileInfo)
        {
            //Assume the last write time is the date.
            return new ImageFixInfo
            {
                FileInfo = fileInfo,
                DateTaken = fileInfo.LastWriteTime
            };
        }
        private IEnumerable<System.IO.FileInfo> EnumerateFiles(FixerLogicParameters parameters, System.IO.DirectoryInfo inputDirectory)
        {
            return inputDirectory.EnumerateFiles("*.*", parameters.RecurseInput ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly);
        }

        private void ExecuteYearGroup(
            FixerLogicParameters parameters,
            System.IO.DirectoryInfo outputDirectory, 
            IGrouping<int, ImageFixInfo> yearGroup, 
            ref int totalCount
            )
        {
            var yearDirectory = System.IO.Path.Combine(outputDirectory.FullName, yearGroup.Key.ToString(CultureInfo.InvariantCulture));
            if (!System.IO.Directory.Exists(yearDirectory))
            {
                OnNotifyMessage($"Creating year directory {yearDirectory}.");
                if (!parameters.IsPreview)
                {
                    System.IO.Directory.CreateDirectory(yearDirectory);
                }
            }

            int yearCount = 0;
            var yearGroupByMonth = yearGroup.GroupBy(o => new { Month = o.Month, MonthName = o.MonthName });
            foreach (var monthGroup in yearGroupByMonth)
            {
                ExecuteMonthGroup(parameters, yearDirectory, monthGroup.Key.Month, monthGroup.Key.MonthName, monthGroup, ref yearCount);
            }

            OnNotifyMessage($"Copied {yearCount} files to {yearDirectory}.");
            totalCount += yearCount;
        }
        private void ExecuteMonthGroup(
            FixerLogicParameters parameters, 
            string yearDirectory, 
            int month, string monthName,
            IEnumerable<ImageFixInfo> monthGroup,
            ref int yearCount
            )
        {
            var dirName = $"{month.ToString("d2")} {monthName}";
            var monthDirectory = System.IO.Path.Combine(yearDirectory, dirName);
            if (!System.IO.Directory.Exists(monthDirectory))
            {
                OnNotifyMessage($"Creating month directory {monthDirectory}.");

                if (!parameters.IsPreview)
                {
                    System.IO.Directory.CreateDirectory(monthDirectory);
                }
            }

            int monthCount = 0;
            foreach (var info in monthGroup)
            {
                ExecuteInfo(parameters, info, monthDirectory, ref monthCount);
            }

            OnNotifyMessage($"Copied {monthCount} files to {monthDirectory}.");
            yearCount += monthCount;
        }
        private void ExecuteInfo(
            FixerLogicParameters parameters,
            ImageFixInfo info,
            string monthDirectory, 
            ref int monthCount
            )
        {
            var fileInfo = info.FileInfo;
            var destPath = GetDestFilePath(fileInfo, monthDirectory);
            if (parameters.IsPreview)
            {
                OnNotifyMessage($"Copying {fileInfo.FullName} to {destPath}");
            }

            monthCount++;
            if (!parameters.IsPreview)
            {
                fileInfo.CopyTo(destPath, true);
            }
            //else
            //{
            //    System.Threading.Thread.Sleep(150);
            //}
            OnNotifyProcessed();
        }
        private string GetDestFilePath(System.IO.FileInfo fileInfo, string monthDirectory)
        {
            var fileName = fileInfo.Name;
            var originalFileName = System.IO.Path.GetFileNameWithoutExtension(fileName);

            int i = 2;
            var destPath = System.IO.Path.Combine(monthDirectory, fileName);
            while (System.IO.File.Exists(destPath))
            {
                fileName = $"{originalFileName}_{i}";
                fileName = System.IO.Path.ChangeExtension(fileName, fileInfo.Extension);
                destPath = System.IO.Path.Combine(monthDirectory, fileName);
                i++;
            }
            return destPath;
        }

    }

    public class FixerLogicParameters
    {
        public string InputDirectory { get; }
        public string OutputDirectory { get; }
        public bool RecurseInput { get; }

        public bool IsPreview { get; }

        public FixerLogicParameters(string inputDirectory, string outputDirectory, bool recurseInput, bool isPreview)
        {
            InputDirectory = inputDirectory;
            OutputDirectory = outputDirectory;
            RecurseInput = recurseInput;
            IsPreview = isPreview;
        }
    }

    public class ImageFixInfo
    {
        public System.IO.FileInfo FileInfo { get; set; }

        public DateTime DateTaken { get; set; }
        public int Year { get { return DateTaken.Year; } }
        public int Month { get { return DateTaken.Month; } }
        public string MonthName { get { return DateTaken.ToString("MMMM"); } }

        public override string ToString()
        {
            return $"{FileInfo.FullName} {DateTaken.ToString()}";
        }
    }

}