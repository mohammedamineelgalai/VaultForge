using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace XnrgyEngineeringAutomationTools.Services
{
    /// <summary>
    /// Service centralise pour la gestion du focus des fenetres.
    /// Utilise la technique "topmost temporaire" pour forcer l'application au premier plan
    /// apres le demarrage d'Inventor ou d'autres applications externes.
    /// </summary>
    public static class WindowFocusService
    {
        #region Win32 API Imports

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        #endregion

        #region Constants

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;
        private const int SW_SHOWNOACTIVATE = 4;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;

        #endregion

        /// <summary>
        /// Force la fenetre principale de l'application au premier plan.
        /// Utilise la technique "topmost temporaire" pour garantir le focus meme si
        /// une autre application (comme Inventor) vient de prendre le focus.
        /// </summary>
        /// <param name="window">La fenetre WPF a mettre au premier plan (optionnel, utilise MainWindow si null)</param>
        /// <param name="delayMs">Delai avant la tentative de focus (permet a Inventor de se stabiliser)</param>
        /// <param name="attempts">Nombre de tentatives</param>
        public static async Task BringToFrontAsync(Window? window = null, int delayMs = 500, int attempts = 3)
        {
            try
            {
                // Attendre que l'application externe (Inventor) se stabilise
                if (delayMs > 0)
                {
                    await Task.Delay(delayMs);
                }

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    BringToFrontInternal(window, attempts);
                });

                Logger.Log("[+] Application remise au premier plan", Logger.LogLevel.DEBUG);
            }
            catch (Exception ex)
            {
                Logger.Log($"[!] Erreur lors de la remise au premier plan: {ex.Message}", Logger.LogLevel.WARNING);
            }
        }

        /// <summary>
        /// Version synchrone pour forcer la fenetre au premier plan.
        /// A utiliser quand on est deja sur le thread UI.
        /// </summary>
        public static void BringToFront(Window? window = null, int attempts = 3)
        {
            try
            {
                BringToFrontInternal(window, attempts);
                Logger.Log("[+] Application remise au premier plan (sync)", Logger.LogLevel.DEBUG);
            }
            catch (Exception ex)
            {
                Logger.Log($"[!] Erreur lors de la remise au premier plan: {ex.Message}", Logger.LogLevel.WARNING);
            }
        }

        /// <summary>
        /// Force la fenetre au premier plan apres le demarrage d'Inventor.
        /// Version specifique avec delais optimises pour Inventor.
        /// </summary>
        /// <param name="window">La fenetre WPF a mettre au premier plan</param>
        public static async Task BringToFrontAfterInventorStartAsync(Window? window = null)
        {
            Logger.Log("[>] Attente stabilisation Inventor puis remise au premier plan...", Logger.LogLevel.DEBUG);

            // Delai plus long pour laisser Inventor se stabiliser completement
            await Task.Delay(1500);

            // Tentatives multiples avec technique topmost temporaire
            await BringToFrontAsync(window, delayMs: 0, attempts: 5);

            // Verification finale apres un court delai
            await Task.Delay(300);
            await BringToFrontAsync(window, delayMs: 0, attempts: 2);

            Logger.Log("[+] Application definitivement au premier plan apres demarrage Inventor", Logger.LogLevel.INFO);
        }

        #region Private Methods

        private static void BringToFrontInternal(Window? window, int attempts)
        {
            // Obtenir la fenetre cible
            Window targetWindow = window ?? System.Windows.Application.Current.MainWindow;
            if (targetWindow == null) return;

            IntPtr handle = new System.Windows.Interop.WindowInteropHelper(targetWindow).Handle;
            if (handle == IntPtr.Zero) return;

            // Restaurer si minimisee
            if (IsIconic(handle))
            {
                ShowWindow(handle, SW_RESTORE);
                System.Threading.Thread.Sleep(100);
            }

            // Technique d'attachement de thread pour autoriser SetForegroundWindow
            IntPtr foregroundWindow = GetForegroundWindow();
            uint foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, out _);
            uint currentThreadId = GetCurrentThreadId();

            bool attached = false;
            if (foregroundThreadId != currentThreadId)
            {
                attached = AttachThreadInput(currentThreadId, foregroundThreadId, true);
            }

            try
            {
                // Tentatives multiples avec technique "topmost temporaire"
                for (int attempt = 1; attempt <= attempts; attempt++)
                {
                    // Activer la fenetre WPF
                    targetWindow.Activate();
                    targetWindow.Focus();

                    // SetForegroundWindow
                    SetForegroundWindow(handle);
                    ShowWindow(handle, SW_SHOW);
                    System.Threading.Thread.Sleep(100);

                    // Technique "topmost temporaire" pour forcer le focus
                    // 1. Mettre la fenetre en topmost
                    SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                    System.Threading.Thread.Sleep(50);

                    // 2. Enlever le topmost pour permettre a d'autres fenetres de passer devant si necessaire
                    SetWindowPos(handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                    System.Threading.Thread.Sleep(50);

                    // Verification: si on a le focus, on peut arreter
                    if (GetForegroundWindow() == handle)
                    {
                        break;
                    }
                }
            }
            finally
            {
                // Detacher les threads
                if (attached)
                {
                    AttachThreadInput(currentThreadId, foregroundThreadId, false);
                }
            }

            // Derniere verification
            SetForegroundWindow(handle);
        }

        #endregion
    }
}
