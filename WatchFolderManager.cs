#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2023 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using ShareX.HelpersLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ShareX
{
    /// <summary>
    /// Manages the watch folders for automatic file uploading
    /// </summary>
    public class WatchFolderManager : IDisposable
    {
        /// <summary>
        /// Gets the list of specified Watch Folders to upload file in
        /// </summary>
        public List<WatchFolder> WatchFolders { get; private set; }

        /// <summary>
        /// Updates the Watch Folder list based on default watch folder setting and hotkey-specific watch folder settings
        /// </summary>
        public void UpdateWatchFolders()
        {
            if (WatchFolders != null)
            {
                UnregisterAllWatchFolders();
            }

            WatchFolders = new List<WatchFolder>();

            foreach (WatchFolderSettings defaultWatchFolderSetting in Program.DefaultTaskSettings.WatchFolderList)
            {
                AddWatchFolder(defaultWatchFolderSetting, Program.DefaultTaskSettings);
            }

            foreach (HotkeySettings hotkeySetting in Program.HotkeysConfig.Hotkeys)
            {
                foreach (WatchFolderSettings watchFolderSetting in hotkeySetting.TaskSettings.WatchFolderList)
                {
                    AddWatchFolder(watchFolderSetting, hotkeySetting.TaskSettings);
                }
            }
        }

        private WatchFolder FindWatchFolder(WatchFolderSettings watchFolderSetting)
        {
            return WatchFolders.FirstOrDefault(watchFolder => watchFolder.Settings == watchFolderSetting);
        }

        private bool IsExist(WatchFolderSettings watchFolderSetting)
        {
            return FindWatchFolder(watchFolderSetting) != null;
        }

        /// <summary>
        /// Adds a new watch folder with provided settings into the watch folders list if it doesn't exist yet
        /// </summary>
        /// <param name="watchFolderSetting">Watch folder settings to add</param>
        /// <param name="taskSettings">Associated task settings</param>
        public void AddWatchFolder(WatchFolderSettings watchFolderSetting, TaskSettings taskSettings)
        {
            if (!IsExist(watchFolderSetting))
            {
                if (!taskSettings.WatchFolderList.Contains(watchFolderSetting))
                {
                    taskSettings.WatchFolderList.Add(watchFolderSetting);
                }

                WatchFolder watchFolder = new WatchFolder();
                watchFolder.Settings = watchFolderSetting;
                watchFolder.TaskSettings = taskSettings;

                watchFolder.FileWatcherTrigger += origPath =>
                {
                    TaskSettings taskSettingsCopy = TaskSettings.GetSafeTaskSettings(taskSettings);
                    string destPath = origPath;

                    if (watchFolderSetting.MoveFilesToScreenshotsFolder)
                    {
                        string screenshotsFolder = TaskHelpers.GetScreenshotsFolder(taskSettingsCopy);
                        string fileName = Path.GetFileName(origPath);
                        destPath = TaskHelpers.HandleExistsFile(screenshotsFolder, fileName, taskSettingsCopy);
                        FileHelpers.CreateDirectoryFromFilePath(destPath);
                        File.Move(origPath, destPath);
                    }

                    UploadManager.UploadFile(destPath, taskSettingsCopy);
                };

                WatchFolders.Add(watchFolder);

                if (taskSettings.WatchFolderEnabled)
                {
                    watchFolder.Enable();
                }
            }
        }

        /// <summary>
        /// Removes the watch folder with the provided settings from the watch folder list
        /// </summary>
        /// <param name="watchFolderSetting">Watch folder settings to remove</param>
        public void RemoveWatchFolder(WatchFolderSettings watchFolderSetting)
        {
            using (WatchFolder watchFolder = FindWatchFolder(watchFolderSetting))
            {
                if (watchFolder != null)
                {
                    watchFolder.TaskSettings.WatchFolderList.Remove(watchFolderSetting);
                    WatchFolders.Remove(watchFolder);
                }
            }
        }

        /// <summary>
        /// Updates state of watch folder that matches the specified settings
        /// </summary>
        /// <param name="watchFolderSetting">The watch folder settings to state update</param>
        public void UpdateWatchFolderState(WatchFolderSettings watchFolderSetting)
        {
            WatchFolder watchFolder = FindWatchFolder(watchFolderSetting);
            if (watchFolder != null)
            {
                if (watchFolder.TaskSettings.WatchFolderEnabled)
                {
                    watchFolder.Enable();
                }
                else
                {
                    watchFolder.Dispose();
                }
            }
        }

        /// <summary>
        /// Unregisters all watch folders
        /// </summary>
        public void UnregisterAllWatchFolders()
        {
            if (WatchFolders != null)
            {
                foreach (WatchFolder watchFolder in WatchFolders)
                {
                    if (watchFolder != null)
                    {
                        watchFolder.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Disposes the WatchFolderManager instanse and unregisters all the watch folders
        /// </summary>
        public void Dispose()
        {
            UnregisterAllWatchFolders();
        }
    }
}