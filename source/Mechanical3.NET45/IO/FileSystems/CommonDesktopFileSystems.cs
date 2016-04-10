using System;
using System.IO;
using System.Threading;
using Mechanical3.Core;

namespace Mechanical3.IO.FileSystems
{
    /// <summary>
    /// Implements <see cref="ICommonFileSystems"/> for the desktop environment.
    /// The necessary directories do not get created until the first time a property is invoked.
    /// </summary>
    public class CommonDesktopFileSystems : ICommonFileSystems
    {
        #region Private Fields

        private readonly string persistentAppDataFullPath;
        private readonly string temporaryAppDataFullPath;
        private readonly string persistentUserDocumentsFullPath;
        private IFileSystem persistentAppDataFileSystem = null;
        private IFileSystem temporaryAppDataFileSystem = null;
        private IFileSystem persistentUserDocumentsFileSystem = null;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommonDesktopFileSystems"/> class.
        /// </summary>
        /// <param name="appName">The application name to use for generating the default locations. Ignored if all other parameters are specified.</param>
        /// <param name="persistentAppDataPath">The host directory path to use for <see cref="ICommonFileSystems.PersistentAppData"/>; or <c>null</c> to use the default location.</param>
        /// <param name="temporaryAppDataPath">The host directory path to use for <see cref="ICommonFileSystems.TemporaryAppData"/>; or <c>null</c> to use the default location.</param>
        /// <param name="persistentUserDocumentsPath">The host directory path to use for <see cref="ICommonFileSystems.PersistentUserDocuments"/>; or <c>null</c> to use the default location.</param>
        public CommonDesktopFileSystems( string appName, string persistentAppDataPath = null, string temporaryAppDataPath = null, string persistentUserDocumentsPath = null )
        {
            if( !appName.NullOrEmpty()
             && !FilePath.IsValidName(appName) )
                throw new ArgumentException("Invalid application name!").Store(nameof(appName), appName);

            if( appName.NullOrEmpty()
             && (persistentAppDataPath.NullReference() || temporaryAppDataPath.NullReference() || persistentUserDocumentsPath.NullReference()) )
                throw new ArgumentException("Application name required!").StoreFileLine();

            if( persistentAppDataPath.NullReference() )
            {
                persistentAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                persistentAppDataPath = Path.Combine(persistentAppDataPath, appName);
            }
            this.persistentAppDataFullPath = Path.GetFullPath(persistentAppDataPath);

            if( temporaryAppDataPath.NullReference() )
            {
                temporaryAppDataPath = Path.GetTempPath();
                temporaryAppDataPath = Path.Combine(temporaryAppDataPath, appName);
            }
            this.temporaryAppDataFullPath = Path.GetFullPath(temporaryAppDataPath);

            if( persistentUserDocumentsPath.NullReference() )
            {
                persistentUserDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                persistentUserDocumentsPath = Path.Combine(persistentUserDocumentsPath, appName);
            }
            this.persistentUserDocumentsFullPath = Path.GetFullPath(persistentUserDocumentsPath);

#if DEBUG
            Log.Debug("Initializing common file system paths.", new Exception().Store(nameof(this.persistentAppDataFullPath), this.persistentAppDataFullPath).Store(nameof(this.temporaryAppDataFullPath), this.temporaryAppDataFullPath).Store(nameof(this.persistentUserDocumentsFullPath), this.persistentUserDocumentsFullPath));
#endif
        }

        #endregion

        #region ICommonFileSystems

        /// <summary>
        /// Gets the file system used to store persistent application data (e.g. settings, accounts, databases, ... etc.).
        /// If applicable, it should be a location specific to the current user. It is always specific to the current application.
        /// This is not(!) guaranteed to be isolated from other apps, or encrypted.
        /// </summary>
        /// <value>The <see cref="IFileSystem"/> used to store persistent application data.</value>
        public IFileSystem PersistentAppData
        {
            get
            {
                if( this.persistentAppDataFileSystem.NullReference() )
                {
                    var newFileSystem = new DirectoryFileSystem(this.persistentAppDataFullPath);
                    Interlocked.CompareExchange(ref this.persistentAppDataFileSystem, newFileSystem, comparand: null);
                }

                return this.persistentAppDataFileSystem;
            }
        }

        /// <summary>
        /// Gets the file system used to store temporary (cache) application data.
        /// This is data that is useful (or necessary) to store a copy of locally, but the OS may decide to remove them if it is low on disk space.
        /// It is however still your own responsibility to manage how much space you use: if you don't need it, delete it.
        /// If applicable, it should be a location specific to the current user. It is always specific to the current application.
        /// This is not(!) guaranteed to be isolated from other apps, or encrypted.
        /// </summary>
        /// <value>The <see cref="IFileSystem"/> used to store temporary application data.</value>
        public IFileSystem TemporaryAppData
        {
            get
            {
                if( this.temporaryAppDataFileSystem.NullReference() )
                {
                    var newFileSystem = new DirectoryFileSystem(this.temporaryAppDataFullPath);
                    Interlocked.CompareExchange(ref this.temporaryAppDataFileSystem, newFileSystem, comparand: null);
                }

                return this.temporaryAppDataFileSystem;
            }
        }

        /// <summary>
        /// Gets the file system used to store files created by the user using this application (e.g. documents, pictures, projects, workspaces, ... etc.).
        /// Unlike the other properties, this should also be a location that is easy to find for the user.
        /// If applicable, it should be a location specific to the current user. It is always specific to the current application.
        /// This is not(!) guaranteed to be isolated from other apps, or encrypted.
        /// </summary>
        /// <value>The <see cref="IFileSystem"/> used to store persistent user files.</value>
        public IFileSystem PersistentUserDocuments
        {
            get
            {
                if( this.persistentUserDocumentsFileSystem.NullReference() )
                {
                    var newFileSystem = new DirectoryFileSystem(this.persistentUserDocumentsFullPath);
                    Interlocked.CompareExchange(ref this.persistentUserDocumentsFileSystem, newFileSystem, comparand: null);
                }

                return this.persistentUserDocumentsFileSystem;
            }
        }

        #endregion
    }
}
