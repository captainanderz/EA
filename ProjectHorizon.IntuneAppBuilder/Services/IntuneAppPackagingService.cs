using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using ProjectHorizon.ApplicationCore.Deployment;
using ProjectHorizon.IntuneAppBuilder.Domain;
using ProjectHorizon.IntuneAppBuilder.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Directory = System.IO.Directory;
using File = System.IO.File;

namespace ProjectHorizon.IntuneAppBuilder.Services
{
    /// <inheritdoc />
    internal class IntuneAppPackagingService : IIntuneAppPackagingService
    {
        private readonly ILogger logger;

        public IntuneAppPackagingService(ILogger<IntuneAppPackagingService> logger) => this.logger = logger;

        /// <inheritdoc />
        public async Task<IntuneAppPackage> BuildPackageAsync(string sourcePath = ".", string setupFilePath = null)
        {
            Stopwatch sw = Stopwatch.StartNew();

            string originalSourcePath = Path.GetFullPath(sourcePath);
            logger.LogInformation($"Creating Intune app package from {originalSourcePath}.");

            (string ZipFilePath, string SetupFilePath) = ZipContent(sourcePath, setupFilePath);

            if (ZipFilePath != null)
            {
                sourcePath = ZipFilePath;
                setupFilePath = SetupFilePath;
            }

            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException($"Could not find source file {sourcePath}.");
            }

            setupFilePath ??= sourcePath;

            logger.LogInformation($"Generating encrypted version of {sourcePath}.");

            MemoryStream data = new MemoryStream();
            FileEncryptionInfo encryptionInfo = await EncryptFileAsync(sourcePath, data);
            data.Position = 0;

            (Win32LobAppMsiInformation Info, MobileMsiManifest Manifest) msiInfo = GetMsiInfo(setupFilePath);

            MobileLobApp app = msiInfo.Info != null && ZipFilePath == null // a zip of a folder with an MSI is still a Win32LobApp, not a WindowsMobileMSI
                ? new WindowsMobileMSI
                {
                    ProductCode = msiInfo.Info.ProductCode,
                    ProductVersion = msiInfo.Info.ProductVersion,
                    FileName = Path.GetFileName(setupFilePath)
                } as MobileLobApp
                : new Win32LobApp
                {
                    FileName = $"{DeploymentConstants.DefaultPackageName}.intunewin",
                    SetupFilePath = Path.GetFileName(setupFilePath),
                    MsiInformation = msiInfo.Info,
                    InstallExperience = new Win32LobAppInstallExperience { RunAsAccount = GetRunAsAccountType(msiInfo) },
                };

            app.DisplayName = msiInfo.Info?.ProductName ?? DeploymentConstants.DefaultPackageName;
            app.Publisher = msiInfo.Info?.Publisher;

            MobileAppContentFile file = new MobileAppContentFile
            {
                Name = app.FileName,
                Size = new FileInfo(sourcePath).Length,
                SizeEncrypted = data.Length,
                Manifest = msiInfo.Manifest?.ToByteArray()
            };

            IntuneAppPackage result = new IntuneAppPackage
            {
                Data = data,
                App = app,
                EncryptionInfo = encryptionInfo,
                File = file
            };

            if (ZipFilePath != null)
            {
                File.Delete(ZipFilePath);
            }

            logger.LogInformation($"Created Intune app package from {originalSourcePath} in {sw.ElapsedMilliseconds}ms.");

            return result;
        }

        public async Task BuildPackageForPortalAsync(IntuneAppPackage package, Stream outputStream)
        {
            Stopwatch sw = Stopwatch.StartNew();

            logger.LogInformation($"Creating Intune portal package for {package.App.FileName}.");

            using ZipArchive archive = new ZipArchive(outputStream, ZipArchiveMode.Create);

            // the portal can only read if no compression is used

            ZipArchiveEntry packageEntry = archive.CreateEntry("IntuneWinPackage/Contents/IntunePackage.intunewin", CompressionLevel.NoCompression);
            package.Data.Position = 0;
            using (Stream dataEntryStream = packageEntry.Open())
            {
                await package.Data.CopyToAsync(dataEntryStream);
            }

            ZipArchiveEntry detectionEntry = archive.CreateEntry("IntuneWinPackage/Metadata/Detection.xml", CompressionLevel.NoCompression);
            using (Stream detectionEntryStream = detectionEntry.Open())
            {
                using StreamWriter writer = new StreamWriter(detectionEntryStream);
                await writer.WriteAsync(GetDetectionXml(package));
            }

            logger.LogInformation($"Created Intune portal package for {package.App.FileName} in {sw.ElapsedMilliseconds}ms.");
        }

        private static string FormatXml(XmlDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                OmitXmlDeclaration = true
            };
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }
            return sb.ToString();
        }

        private static RunAsAccountType GetRunAsAccountType((Win32LobAppMsiInformation Info, MobileMsiManifest Manifest) msiInfo) => msiInfo.Info?.PackageType == Win32LobAppMsiPackageType.PerUser ? RunAsAccountType.User : RunAsAccountType.System;

        /// <summary>
        ///     Algorithm to encrypt file for upload to Intune as intunewin.
        /// </summary>
        private async Task<FileEncryptionInfo> EncryptFileAsync(string sourceFilePath, Stream outputStream)
        {
            byte[] CreateIVEncryptionKey()
            {
                using Aes aes = Aes.Create();
                return (aes ?? throw new InvalidOperationException()).IV;
            }

            byte[] CreateEncryptionKey()
            {
                using (AesCryptoServiceProvider provider = new AesCryptoServiceProvider())
                {
                    provider.GenerateKey();
                    return provider.Key;
                }
            }

            byte[] encryptionKey = CreateEncryptionKey();
            byte[] hmacKey = CreateEncryptionKey();
            byte[] initializationVector = CreateIVEncryptionKey();

            async Task<byte[]> EncryptFileWithIVAsync()
            {
                using Aes aes = Aes.Create() ?? throw new InvalidOperationException();
                using HMACSHA256 hmacSha256 = new HMACSHA256 { Key = hmacKey };
                int hmacLength = hmacSha256.HashSize / 8;
                const int bufferBlockSize = 1024 * 4;
                byte[] buffer = new byte[bufferBlockSize];

                await outputStream.WriteAsync(buffer, 0, hmacLength + initializationVector.Length);
                using (FileStream sourceStream = File.Open(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (ICryptoTransform encryptor = aes.CreateEncryptor(encryptionKey, initializationVector))
                using (CryptoStream cryptoStream = new(outputStream, encryptor, CryptoStreamMode.Write, true))
                {
                    int bytesRead;
                    while ((bytesRead = sourceStream.Read(buffer, 0, bufferBlockSize)) > 0)
                    {
                        await cryptoStream.WriteAsync(buffer, 0, bytesRead);
                        cryptoStream.Flush();
                    }

                    cryptoStream.FlushFinalBlock();
                }

                outputStream.Seek(hmacLength, SeekOrigin.Begin);
                await outputStream.WriteAsync(initializationVector, 0, initializationVector.Length);
                outputStream.Seek(hmacLength, SeekOrigin.Begin);

                byte[] hmac = hmacSha256.ComputeHash(outputStream);

                outputStream.Seek(0, SeekOrigin.Begin);
                await outputStream.WriteAsync(hmac, 0, hmac.Length);

                return hmac;
            }

            // Create the encrypted target file and compute the HMAC value.
            byte[] mac = await EncryptFileWithIVAsync();

            // Compute the SHA256 hash of the source file and convert the result to bytes.
            using SHA256 sha256 = SHA256.Create();
            using FileStream fs = File.OpenRead(sourceFilePath);

            return new FileEncryptionInfo
            {
                EncryptionKey = encryptionKey,
                MacKey = hmacKey,
                InitializationVector = initializationVector,
                Mac = mac,
                ProfileIdentifier = "ProfileVersion1",
                FileDigest = sha256.ComputeHash(fs),
                FileDigestAlgorithm = "SHA256"
            };
        }

        /// <summary>
        ///     This file is included in the zip file the portal expects.
        ///     It is essentially a collection of metadata, used specifically by the portal javascript to patch data on the mobile
        ///     app and its content (e.g. the Manifest of the content file).
        /// </summary>
        private string GetDetectionXml(IntuneAppPackage package)
        {
            XmlDocument xml = new XmlDocument();

            XmlElement AppendElement(XmlNode parent, string name, object value = null)
            {
                XmlElement e = xml.CreateElement(name);
                if (value != null)
                {
                    e.InnerText = value.ToString();
                }

                parent.AppendChild(e);
                return e;
            }

            XmlElement infoElement = AppendElement(xml, "ApplicationInfo");
            xml.DocumentElement?.SetAttribute("ToolVersion", "1.4.0.0");
            AppendElement(infoElement, "Name", package.App.DisplayName);
            AppendElement(infoElement, "UnencryptedContentSize", package.File.Size);
            AppendElement(infoElement, "FileName", "IntunePackage.intunewin");
            AppendElement(infoElement, "SetupFile", package.App is Win32LobApp win32 ? win32.SetupFilePath : package.App.FileName);

            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces(new[] {
                new XmlQualifiedName(string.Empty, string.Empty)
            });

            using (XmlWriter writer = infoElement.CreateNavigator().AppendChild())
            {
                writer.WriteWhitespace("");

                XmlAttributeOverrides overrides = new XmlAttributeOverrides();
                overrides.Add(typeof(FileEncryptionInfo), nameof(FileEncryptionInfo.AdditionalData), new XmlAttributes { XmlIgnore = true });
                overrides.Add(typeof(FileEncryptionInfo), nameof(FileEncryptionInfo.ODataType), new XmlAttributes { XmlIgnore = true });

                new XmlSerializer(typeof(FileEncryptionInfo), overrides, new Type[0],
                        new XmlRootAttribute("EncryptionInfo"), null)
                    .Serialize(writer, package.EncryptionInfo, namespaces);
            }

            if (package.File.Manifest != null)
            {
                using (XmlWriter writer = infoElement.CreateNavigator().AppendChild())
                {
                    writer.WriteWhitespace("");

                    XmlAttributeOverrides overrides = new XmlAttributeOverrides();
                    typeof(MobileMsiManifest).GetProperties().ToList().ForEach(p =>
                    {
                        if (p.DeclaringType != null)
                        {
                            overrides.Add(p.DeclaringType, p.Name, new XmlAttributes());
                        }
                    });
                    new XmlSerializer(typeof(MobileMsiManifest), overrides, new Type[0], new XmlRootAttribute("MsiInfo"), string.Empty)
                        .Serialize(writer, MobileMsiManifest.FromByteArray(package.File.Manifest), namespaces);
                }
            }

            return FormatXml(xml);
        }

        private (Win32LobAppMsiInformation Info, MobileMsiManifest Manifest) GetMsiInfo(string setupFilePath)
        {
            if (".msi".Equals(Path.GetExtension(setupFilePath), StringComparison.OrdinalIgnoreCase))
            {
                using (MsiUtil util = new MsiUtil(setupFilePath, logger))
                {
                    return util.ReadMsiInfo();
                }
            }

            return default;
        }

        private (string ZipFilePath, string SetupFilePath) ZipContent(string sourcePath, string setupFilePath)
        {
            string zipFilePath = null;
            if (Directory.Exists(sourcePath))
            {
                sourcePath = Path.GetFullPath(sourcePath);
                
                zipFilePath = Path.Combine(Path.GetTempPath(), Path.GetDirectoryName(sourcePath) ?? "", $"{Path.GetFileNameWithoutExtension(sourcePath)}.intunewin.zip");

                if (File.Exists(zipFilePath))
                {
                    File.Delete(zipFilePath);
                }

                logger.LogInformation($"Creating intermediate zip of {sourcePath} at {zipFilePath}.");

                ZipFile.CreateFromDirectory(sourcePath, zipFilePath, CompressionLevel.Optimal, false);
                if (setupFilePath == null)
                {
                    setupFilePath = Directory.GetFiles(sourcePath, "*.msi").FirstOrDefault() ?? Directory.GetFiles(sourcePath, "*.exe").FirstOrDefault();
                }
            }

            return (zipFilePath, setupFilePath);
        }
    }
}