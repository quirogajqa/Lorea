// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Lorea contributors
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Piccasa
{
    static class Updates
    {
        const string Repository = "quirogajqa/Lorea";
        internal const string InstallerName = "Lorea-Setup.exe";
        internal static readonly Version InstalledVersion = typeof(Updates).Assembly.GetName().Version;

        [DataContract]
        internal sealed class Release
        {
            [DataMember(Name = "tag_name")] public string Tag { get; set; }
            [DataMember(Name = "body")] public string Notes { get; set; }
            [DataMember(Name = "assets")] public Asset[] Assets { get; set; }
            [DataMember(Name = "draft")] public bool Draft { get; set; }
            [DataMember(Name = "prerelease")] public bool Prerelease { get; set; }
        }

        [DataContract]
        internal sealed class Asset
        {
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "digest")] public string Digest { get; set; }
        }

        internal sealed class Update
        {
            public Version Version;
            public string Tag, Digest, Notes;
        }

        static WebClient CreateClient()
        {
            // The legacy .NET Framework compiler can leave WebClient on TLS 1.0 defaults.
            var protocols = ServicePointManager.SecurityProtocol;
            if (protocols != SecurityProtocolType.SystemDefault &&
                (protocols & SecurityProtocolType.Tls12) == 0)
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var client = new WebClient();
            client.Headers[HttpRequestHeader.UserAgent] = "Lorea/" + InstalledVersion.ToString(3);
            return client;
        }

        internal static Update Check()
        {
            using (var client = CreateClient()) {
                client.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
                using (var response = new MemoryStream(client.DownloadData("https://api.github.com/repos/" + Repository + "/releases/latest")))
                    return ReadRelease(response, InstalledVersion);
            }
        }

        internal static Update ReadRelease(Stream response, Version installed)
        {
            var release = (Release)new DataContractJsonSerializer(typeof(Release)).ReadObject(response);
            Version version;
            if (release == null || release.Draft || release.Prerelease || release.Tag == null ||
                !Regex.IsMatch(release.Tag, @"\Av?[0-9]+\.[0-9]+\.[0-9]+\z") ||
                !Version.TryParse(release.Tag.TrimStart('v'), out version))
                throw new IOException("GitHub no devolvió una versión estable válida de Lorea.");
            if (version <= new Version(installed.Major, installed.Minor, installed.Build)) return null;

            var installer = (release.Assets ?? new Asset[0]).SingleOrDefault(asset => asset != null && asset.Name == InstallerName);
            if (installer == null || installer.Digest == null || !Regex.IsMatch(installer.Digest, @"\Asha256:[0-9a-fA-F]{64}\z"))
                throw new IOException("La versión publicada no incluye un instalador con SHA-256 verificable.");
            return new Update { Version = version, Tag = release.Tag, Digest = installer.Digest.Substring(7), Notes = release.Notes };
        }

        internal static async Task<string> Download(Update update, Action<int> progress, CancellationToken cancellation)
        {
            string folder = Path.Combine(Path.GetTempPath(), "Lorea-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, InstallerName);
            try {
                using (var client = CreateClient()) {
                    client.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs e) {
                        if (e.TotalBytesToReceive > 0) progress(e.ProgressPercentage);
                    };
                    var address = new Uri("https://github.com/" + Repository + "/releases/download/" + update.Tag + "/" + InstallerName);
                    cancellation.ThrowIfCancellationRequested();
                    var download = client.DownloadFileTaskAsync(address, path);
                    using (cancellation.Register(client.CancelAsync)) await download.ConfigureAwait(false);
                }
                cancellation.ThrowIfCancellationRequested();
                progress(100);
                await Task.Run(() => VerifyInstaller(update, path), cancellation).ConfigureAwait(false);
                cancellation.ThrowIfCancellationRequested();
                return path;
            } catch {
                DiscardInstaller(path);
                throw;
            }
        }

        internal static void VerifyInstaller(Update update, string path)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(path)) {
                string digest = BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", "");
                if (!string.Equals(digest, update.Digest, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("El archivo descargado no coincide con la suma SHA-256 publicada.");
            }
        }

        internal static void DiscardInstaller(string path)
        {
            try {
                File.Delete(path);
                Directory.Delete(Path.GetDirectoryName(path));
            } catch (IOException) {
            } catch (UnauthorizedAccessException) {
            }
        }
    }
}
