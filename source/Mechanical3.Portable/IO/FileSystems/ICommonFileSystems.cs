namespace Mechanical3.IO.FileSystems
{
    /// <summary>
    /// File systems to use for common tasks.
    /// This is a basic service locator, so as usual with DI,
    /// only the topmost level of the application should have access to it.
    /// Everyone else should use <see cref="IFileSystem"/>.
    /// </summary>
    public interface ICommonFileSystems
    {
        /// <summary>
        /// Gets the file system used to store persistent application data (e.g. settings, accounts, databases, ... etc.).
        /// If applicable, it should be a location specific to the current user. It is always specific to the current application.
        /// This is not(!) guaranteed to be isolated from other apps, or encrypted.
        /// </summary>
        /// <value>The <see cref="IFileSystem"/> used to store persistent application data.</value>
        IFileSystem PersistentAppData { get; }

        /// <summary>
        /// Gets the file system used to store temporary (cache) application data.
        /// This is data that is useful (or necessary) to store a copy of locally, but the OS may decide to remove them if it is low on disk space.
        /// It is however still your own responsibility to manage how much space you use: if you don't need it, delete it.
        /// If applicable, it should be a location specific to the current user. It is always specific to the current application.
        /// This is not(!) guaranteed to be isolated from other apps, or encrypted.
        /// </summary>
        /// <value>The <see cref="IFileSystem"/> used to store temporary application data.</value>
        IFileSystem TemporaryAppData { get; }

        /// <summary>
        /// Gets the file system used to store files created by the user using this application (e.g. documents, pictures, projects, workspaces, ... etc.).
        /// Unlike the other properties, this should also be a location that is easy to find for the user.
        /// If applicable, it should be a location specific to the current user. It is always specific to the current application.
        /// This is not(!) guaranteed to be isolated from other apps, or encrypted.
        /// </summary>
        /// <value>The <see cref="IFileSystem"/> used to store persistent user files.</value>
        IFileSystem PersistentUserDocuments { get; }
    }
}
