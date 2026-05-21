//
// Copyright(c) 2020 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RealTimeWeather
{
    /// <summary>
    /// This class it's used for writing logs into a file
    /// </summary>
    public static class LogFile
    {
        #region Private Const
        private const string kPath = "/LogFile.txt";
        private const int kMaxFileSizeInBytes = 20000;
        private const int kNumberOfLines = 20;
        #endregion

        #region Public Proprieties
        static public string Path
        {
            get { return Application.persistentDataPath + kPath; }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Log a message into a file
        /// </summary>
        /// <param name="message"> The message that is write into the file</param>
        public static void Write(string message)
        {
            try
            {
                CheckFileSize();
                StreamWriter fileWriters = new StreamWriter(Path, true);
                fileWriters.Write(" \r\n" + DateTime.Now + " " + message + "\r\n");
                fileWriters.Close();
            }
            catch (Exception e)
            {
                Debug.LogError("Can't write into the file!" + e);
            }
        }

        /// <summary>
        /// This function return Log File path
        /// </summary>
        /// <returns>Return the containt of a log file </returns>
        public static string GetLogText()
        {
            var lines = File.ReadAllLines(Path);
            lines = lines.Skip(lines.Length - kNumberOfLines).ToArray();
            return String.Join("\n",lines);
        }
        #endregion

        /// <summary>
        /// Checks if the file doesn't exceed maximum size and trims the oldest errors if exceeded
        /// </summary>
        private static void CheckFileSize()
        {
            if(File.Exists(Path) == false)
            {
                File.WriteAllText(Path, string.Empty);
            }

            long length = new System.IO.FileInfo(Path).Length;

            if (length > kMaxFileSizeInBytes)
            {
                var lines = File.ReadAllLines(Path);
                File.WriteAllLines(Path, lines.Skip(kNumberOfLines).ToArray());
            }
        }
    }
}