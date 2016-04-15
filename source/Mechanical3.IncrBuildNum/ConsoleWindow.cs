using System;
using System.Runtime.InteropServices;

namespace Mechanical3.IncrBuildNum
{
    /// <summary>
    /// Hides console WinAPI calls.
    /// </summary>
    public static class ConsoleWindow
    {
        #region Private API calls

        //// NOTE: a process may only have one console at most.

        /// <summary>
        /// Allocates a new console for the calling process.
        /// </summary>
        /// <returns><c>true</c> if the function succeeds; <c>false</c>, if the process already has a console.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        /// <summary>
        /// Sets the specified window's show state.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="nCmdShow">Controls how the window is to be shown.</param>
        /// <returns><c>true</c> if the window was previously visible.</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow( IntPtr hWnd, int nCmdShow );

        /// <summary>
        /// Hides the window and activates another window.
        /// </summary>
        private const int SW_HIDE = 0;

        /// <summary>
        /// Activates the window and displays it in its current size and position.
        /// </summary>
        private const int SW_SHOW = 5;

        /// <summary>
        /// Retrieves the window handle used by the console associated with the calling process.
        /// </summary>
        /// <returns>The return value is a handle to the window used by the console associated with the calling process or <see cref="IntPtr.Zero"/> if there is no such associated console.</returns>
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        #endregion

        /// <summary>
        /// Makes sure that the current process has a console window, and that it is visible.
        /// </summary>
        public static void Show()
        {
            if( !AllocConsole() )
            {
                // a console window already exists
                IntPtr consoleWindow = GetConsoleWindow();
                ShowWindow(consoleWindow, SW_SHOW);
            }
        }

        /// <summary>
        /// Hides the console window of the current process.
        /// Does nothing if no console window is available.
        /// </summary>
        public static void Hide()
        {
            IntPtr consoleWindow = GetConsoleWindow();
            if( consoleWindow != IntPtr.Zero )
                ShowWindow(consoleWindow, SW_HIDE);
        }
    }
}
