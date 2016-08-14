using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Mechanical3.Core;

namespace Mechanical3.IO.FileSystems
{
    /// <summary>
    /// Represents an abstract file system, that's both readable and writable.
    /// </summary>
    public interface IFileSystem : IFileSystemReader, IFileSystemWriter
    {
        /// <summary>
        /// Gets a value indicating whether the ReadWriteFile method is supported.
        /// </summary>
        /// <value><c>true</c> if the method is supported; otherwise, <c>false</c>.</value>
        bool SupportsReadWriteFile { get; }

        /// <summary>
        /// Opens an existing file, or creates a new one, for both reading and writing.
        /// </summary>
        /// <param name="filePath">The path specifying the file to open.</param>
        /// <returns>A <see cref="Stream"/> representing the file opened.</returns>
        Stream ReadWriteFile( FilePath filePath );
    }

    /// <content>
    /// Methods extending the <see cref="IFileSystem"/> interface.
    /// </content>
    public static partial class FileSystemExtensions
    {
        #region DeleteAllFrom

        /// <summary>
        /// Deletes all content from the specified directory.
        /// The directory itself is not removed.
        /// Does nothing if the directory does not exist.
        /// </summary>
        /// <param name="fileSystem">The file system to query.</param>
        /// <param name="directoryPath">The path specifying the directory to delete all content from; or <c>null</c> to specify the root of this file system.</param>
        public static void DeleteAllFrom( this IFileSystem fileSystem, FilePath directoryPath = null )
        {
            if( fileSystem.NullReference() )
                throw new ArgumentNullException(nameof(fileSystem)).StoreFileLine();

            if( directoryPath.NullReference()
             || fileSystem.Exists(directoryPath) )
            {
                var entries = fileSystem.GetPaths(directoryPath);
                foreach( var entry in entries )
                    fileSystem.Delete(entry);
            }
        }

        #endregion

        #region CreateFileWithBackups

        private const string BackupExtensionPrefix = ".backup";

        /// <summary>
        /// Creates a new empty file, and opens it for writing.
        /// </summary>
        /// <param name="fileSystem">The file system to query.</param>
        /// <param name="filePath">The path specifying the file to create.</param>
        /// <param name="maxBackupNum">The maximum number of backups to keep. This does not include the actual file, to keep backups of.</param>
        /// <returns>A <see cref="Stream"/> representing the file.</returns>
        public static Stream CreateFileWithBackups( this IFileSystem fileSystem, FilePath filePath, int maxBackupNum )
        {
            if( fileSystem.NullReference() )
                throw new ArgumentNullException(nameof(fileSystem)).StoreFileLine();

            if( filePath.NullReference()
             || filePath.IsDirectory )
                throw new ArgumentException("Invalid file path").Store(nameof(filePath), filePath);

            if( maxBackupNum <= 0 )
                throw new ArgumentOutOfRangeException().Store(nameof(maxBackupNum), maxBackupNum);

            // create backups if:
            // ... there is a file to back up
            if( fileSystem.Exists(filePath) )
            {
                // remove extra backups
                var backupPaths = GetBackupPaths(fileSystem, filePath);
                RemoveExtraBackups(fileSystem, backupPaths, maxBackupNum);

                // overwrite old backups
                OverwriteOldBackups(fileSystem, filePath, maxBackupNum);
            }

            // create the origianl file
            return fileSystem.CreateFile(filePath, overwriteIfExists: true);
        }

        private static void RemoveExtraBackups( IFileSystem fileSystem, List<KeyValuePair<FilePath, int>> backupPaths, int maxBackupNum )
        {
            // too high backup indexes?
            for( int i = 0; i < backupPaths.Count; )
            {
                if( backupPaths[i].Value > maxBackupNum )
                {
                    fileSystem.Delete(backupPaths[i].Key);
                    backupPaths.RemoveAt(i);
                }
                else
                {
                    ++i;
                }
            }
        }

        private static void OverwriteOldBackups( IFileSystem fileSystem, FilePath filePath, int maxBackupNum )
        {
            for( int i = maxBackupNum - 1; i > 0; --i )
            {
                Overwrite(
                    fileSystem,
                    GetBackupFilePath(filePath, i),
                    GetBackupFilePath(filePath, i + 1));
            }

            Overwrite(
                fileSystem,
                filePath,
                GetBackupFilePath(filePath, 1));
        }

        private static FilePath GetBackupFilePath( FilePath filePath, int backupIndex )
        {
            return FilePath.From(filePath.ToString() + BackupExtensionPrefix + backupIndex.ToString("D", CultureInfo.InvariantCulture));
        }

        private static void Overwrite( IFileSystem fileSystem, FilePath filePath, FilePath backupPath )
        {
            // we don't know that simultaneous file streams are allowed (compressed archives probably won't allow it)
            if( fileSystem.Exists(filePath) )
            {
                using( var ms = new MemoryStream() )
                {
                    using( var fromStream = fileSystem.ReadFile(filePath) )
                        fromStream.CopyTo(ms);

                    ms.Position = 0;

                    using( var toStream = fileSystem.CreateFile(backupPath, overwriteIfExists: true) )
                        ms.CopyTo(toStream);
                }
            }
        }

        private static List<KeyValuePair<FilePath, int>> GetBackupPaths( IFileSystem fileSystem, FilePath filePath )
        {
            return fileSystem
                .GetPaths(filePath.Parent) // parent may be null
                .Where(p => !p.IsDirectory && IsBackupExtension(p.Extension))
                .Select(p => new KeyValuePair<FilePath, int>(p, GetBackupIndex(p.Extension)))
                .ToList();
        }

        private static bool IsBackupExtension( string extension )
        {
            if( extension.NullOrEmpty() )
                return false;

            if( extension.Length <= BackupExtensionPrefix.Length
             || !FilePath.Comparer.Equals(BackupExtensionPrefix, extension.Substring(0, BackupExtensionPrefix.Length)) )
                return false;

            for( int i = BackupExtensionPrefix.Length; i < extension.Length; ++i )
            {
                if( !char.IsDigit(extension, i) )
                    return false;
            }

            return true;
        }

        private static int GetBackupIndex( string extension )
        {
            return int.Parse(
                extension.Substring(startIndex: BackupExtensionPrefix.Length),
                NumberStyles.None,
                CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
