// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Lorea contributors
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Piccasa
{
    sealed class UpdatesWindow : Window
    {
        readonly TextBlock status = new TextBlock { TextWrapping = TextWrapping.Wrap };
        readonly TextBlock available = new TextBlock { Margin = new Thickness(0, 4, 0, 12) };
        readonly TextBox notes = new TextBox {
            IsReadOnly = true, TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(8),
            Visibility = Visibility.Collapsed
        };
        readonly ProgressBar progress = new ProgressBar {
            Height = 8, Maximum = 100, Margin = new Thickness(0, 8, 0, 0), Visibility = Visibility.Collapsed
        };
        readonly Button primary = new Button { MinWidth = 116, Padding = new Thickness(10, 5, 10, 5) };
        readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        Action action;
        Updates.Update update;
        string installer;
        bool closed;

        internal UpdatesWindow(Viewer viewer)
        {
            Owner = viewer;
            Title = "Actualizaciones de Lorea";
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowInTaskbar = false;
            Width = 480;
            Height = 360;
            MinWidth = 380;
            MinHeight = 290;

            var layout = new DockPanel { Margin = new Thickness(20) };
            Content = layout;
            var versions = new StackPanel();
            versions.Children.Add(new TextBlock { Text = "Versión instalada: " + Updates.InstalledVersion.ToString(3) });
            versions.Children.Add(available);
            DockPanel.SetDock(versions, Dock.Top);
            layout.Children.Add(versions);

            var footer = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
            DockPanel.SetDock(footer, Dock.Bottom);
            layout.Children.Add(footer);
            footer.Children.Add(status);
            footer.Children.Add(progress);
            var buttons = new StackPanel {
                Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 14, 0, 0)
            };
            footer.Children.Add(buttons);
            var close = new Button { Content = "Cerrar", MinWidth = 78, Margin = new Thickness(0, 0, 8, 0) };
            close.Click += delegate { Close(); };
            buttons.Children.Add(close);
            buttons.Children.Add(primary);
            primary.Click += delegate { if (action != null) action(); };
            layout.Children.Add(notes);

            Loaded += async delegate { await Check(); };
            Closed += delegate {
                closed = true;
                lifetime.Cancel();
                lifetime.Dispose();
                if (installer != null) Updates.DiscardInstaller(installer);
            };
        }

        void SetAction(string label, Action next)
        {
            action = next;
            primary.Content = label;
            primary.IsEnabled = next != null;
        }

        async Task Check()
        {
            available.Text = "";
            notes.Visibility = Visibility.Collapsed;
            status.Text = "Buscando la última versión estable…";
            SetAction("Buscando…", null);
            try {
                update = await Task.Run(() => Updates.Check());
                if (closed) return;
                if (update == null) {
                    status.Text = "No hay una versión más reciente.";
                    SetAction("Buscar de nuevo", async delegate { await Check(); });
                    return;
                }
                available.Text = "Versión disponible: " + update.Version;
                notes.Text = string.IsNullOrWhiteSpace(update.Notes) ? "La publicación no incluye notas." : update.Notes;
                notes.Visibility = Visibility.Visible;
                status.Text = "Hay una actualización disponible.";
                SetAction("Descargar", async delegate { await Download(); });
            } catch (Exception ex) {
                if (closed) return;
                status.Text = "No se pudo buscar la actualización. " + DescribeError(ex);
                SetAction("Reintentar búsqueda", async delegate { await Check(); });
            }
        }

        async Task Download()
        {
            SetAction("Descargando…", null);
            status.Text = "Descargando el instalador…";
            progress.Value = 0;
            progress.IsIndeterminate = true;
            progress.Visibility = Visibility.Visible;
            var reporter = new Progress<int>(percent => {
                if (closed || progress.Visibility != Visibility.Visible) return;
                progress.IsIndeterminate = percent == 100;
                progress.Value = percent;
                status.Text = percent == 100 ? "Verificando el SHA-256 del instalador…" : "Descargando el instalador · " + percent + "%";
            });
            try {
                string path = await Updates.Download(update, ((IProgress<int>)reporter).Report, lifetime.Token);
                if (closed) {
                    Updates.DiscardInstaller(path);
                    return;
                }
                installer = path;
                status.Text = "Instalador verificado. Lorea se cerrará al iniciar la instalación.";
                SetAction("Instalar", async delegate { await Install(); });
            } catch (Exception ex) {
                if (closed) return;
                status.Text = "No se pudo preparar el instalador. " + DescribeError(ex);
                SetAction("Reintentar descarga", async delegate { await Download(); });
            } finally {
                if (!closed) progress.Visibility = Visibility.Collapsed;
            }
        }

        async Task Install()
        {
            SetAction("Instalar", null);
            status.Text = "Comprobando el instalador antes de abrirlo…";
            string path = installer;
            try {
                await Task.Run(() => Updates.VerifyInstaller(update, path));
            } catch (Exception ex) {
                Updates.DiscardInstaller(path);
                if (closed) return;
                installer = null;
                status.Text = "No se pudo verificar el instalador. " + DescribeError(ex);
                SetAction("Reintentar descarga", async delegate { await Download(); });
                return;
            }
            if (closed) {
                Updates.DiscardInstaller(path);
                return;
            }

            try {
                using (var setup = Process.Start(new ProcessStartInfo(path, "/LOREAUPDATE=1 /NORESTART") { UseShellExecute = true })) { }
            } catch (Exception ex) {
                status.Text = "No se pudo abrir el instalador. " + ex.Message;
                SetAction("Reintentar instalación", async delegate { await Install(); });
                return;
            }

            installer = null;
            Window viewer = Owner;
            Close();
            viewer.Close();
        }

        static string DescribeError(Exception error)
        {
            var network = error as WebException;
            if (network != null) {
                switch (network.Status) {
                    case WebExceptionStatus.SecureChannelFailure:
                    case WebExceptionStatus.TrustFailure:
                        return "No se pudo establecer una conexión segura con GitHub.";
                    case WebExceptionStatus.NameResolutionFailure:
                    case WebExceptionStatus.ConnectFailure:
                    case WebExceptionStatus.Timeout:
                        return "Comprueba tu conexión a Internet y vuelve a intentarlo.";
                    case WebExceptionStatus.ReceiveFailure:
                    case WebExceptionStatus.ConnectionClosed:
                        return "La conexión se interrumpió antes de terminar.";
                }
            }
            return error.Message;
        }
    }
}
