using log4net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.Deployment;
using ProjectHorizon.ApplicationCore.DTOs;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using File = System.IO.File;
using Image = System.Drawing.Image;

namespace ProjectHorizon.ApplicationCore.Utility
{
    public static class ApplicationHelper
    {
        private const string DataUriScheme64Bit = "data:{0};base64,{1}";
        private const string JpegMime = "image/jpeg";
        private const string PngMime = "image/png";

        private const int SmallPictureSize = 64;

        [Pure]
        public static int CompareAppVersions(string incomingAppVersion, string currentAppVersion)
        {
            Version? incomingVersion = Version.Parse(incomingAppVersion);
            Version? currentVersion = Version.Parse(currentAppVersion);

            return incomingVersion.CompareTo(currentVersion);
        }

        [Pure]
        public static string GetArchiveFileNameFromInfo(ApplicationDto applicationDto, Guid subscriptionId)
            => GetPackageNameFromInfo(applicationDto, subscriptionId) + ".zip";

        [Pure]
        public static string GetArchiveFileNameFromInfo(ApplicationDto applicationDto)
            => GetPackageNameFromInfo(applicationDto) + ".zip";

        public static Attachment GetImageAttachment(Bitmap img, ImageFormat imageFormat, string contendId)
        {
            MemoryStream ms = new MemoryStream();
            img.Save(ms, imageFormat);

            Attachment? emailAttachment = new Attachment()
            {
                Content = Convert.ToBase64String(ms.ToArray()),
                Type = "image/png",
                Filename = contendId,
                Disposition = "inline",
                ContentId = contendId
            };

            return emailAttachment;
        }

        public static string GetMountPath()
        {
            string? mountFolderPath = Path.DirectorySeparatorChar + "share01";

            if (!Directory.Exists(mountFolderPath))
            {
                Directory.CreateDirectory(mountFolderPath);
            }

            return mountFolderPath;
        }

        [Pure]
        public static string GetPackageNameFromInfo(ApplicationDto applicationDto, Guid subscriptionId)
        {
            return $"{GetPackageNameFromInfo(applicationDto)}_{subscriptionId}";
        }

        public static string GetPackageNameFromInfo(ApplicationDto applicationDto)
        {
            string appName = RemoveSpecialCharacters(applicationDto.Name);
            string publisher = RemoveSpecialCharacters(applicationDto.Publisher);
            string architecture = applicationDto.Architecture;

            return $"{publisher}_{appName}_{architecture}_{applicationDto.Version}_{applicationDto.Language}";
        }

        public static async Task<string> GetSmallIconAsBase64Async(string iconFileName, ZipArchive archive)
        {
            ZipArchiveEntry? iconFile = archive.GetEntry(iconFileName);

            if (iconFile == null)
            {
                return DefaultIcons.defaultPngIconBase64;
            }

            string? folderName = Path.GetTempPath();
            string? newIconFileName = Guid.NewGuid() + Path.GetExtension(iconFileName);
            string? newIconFilePath = Path.Combine(folderName, newIconFileName);
            string? iconSmallFilePath = Path.Combine(folderName, "small_" + newIconFileName);

            iconFile.ExtractToFile(newIconFilePath, true);

            string mime;
            using (Image? icon = Image.FromFile(newIconFilePath))
            {
                mime = GetExpectedMimeOrThrow(icon);
                Bitmap? iconSmall = ResizeImage(icon, 40, 40);
                iconSmall.Save(iconSmallFilePath);
            }

            byte[] imageArray = await File.ReadAllBytesAsync(iconSmallFilePath);

            string? iconAsBase64 = string.Format(DataUriScheme64Bit, mime, Convert.ToBase64String(imageArray));

            File.Delete(newIconFilePath);
            File.Delete(iconSmallFilePath);

            return iconAsBase64;
        }

        public static async Task<string> GetSmallPictureAsBase64Async(MemoryStream stream)
        {
            stream.Position = 0;

            using Image? image = Image.FromStream(stream);

            string? mime = GetExpectedMimeOrThrow(image);

            double ratio = (image.Width < image.Height)
                ? (double)image.Width / (double)image.Height
                : (double)image.Height / (double)image.Width;

            Bitmap? smallBitmap = (image.Width > image.Height)
                ? ResizeImage(image, SmallPictureSize, (int)(SmallPictureSize * ratio))
                : ResizeImage(image, (int)(SmallPictureSize * ratio), SmallPictureSize);

            await using MemoryStream? smallPictureStream = new MemoryStream();
            smallBitmap.Save(smallPictureStream, mime == PngMime ? ImageFormat.Png : ImageFormat.Jpeg);

            return string.Format(DataUriScheme64Bit, mime, Convert.ToBase64String(smallPictureStream.ToArray()));
        }

        public static async Task<string> GetSmallLogoAsBase64Async(MemoryStream stream)
        {
            stream.Position = 0;

            using Image? image = Image.FromStream(stream);

            string? mime = GetExpectedMimeOrThrow(image);

            int destinationHeight = Math.Min(image.Height, SmallPictureSize);
            int destinationWidth = (int)(((float)image.Width / image.Height) * destinationHeight);

            Bitmap? smallBitmap = ResizeImage(image, destinationWidth, destinationHeight);

            await using MemoryStream? smallPictureStream = new MemoryStream();
            smallBitmap.Save(smallPictureStream, mime == PngMime ? ImageFormat.Png : ImageFormat.Jpeg);

            return string.Format(DataUriScheme64Bit, mime, Convert.ToBase64String(smallPictureStream.ToArray()));
        }

        [Pure]
        public static string GetTempArchiveFileNameFromInfo(ApplicationDto applicationDto, Guid subscriptionId)
            => "temp_" + GetArchiveFileNameFromInfo(applicationDto, subscriptionId);

        [Pure]
        public static string GetTempArchiveFileNameFromInfo(ApplicationDto applicationDto)
           => "temp_" + GetArchiveFileNameFromInfo(applicationDto);

        [Pure]
        public static string RemoveSpecialCharacters(string str)
            => Regex.Replace(str, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);

        public static async Task<Response<ApplicationDto>> ValidateAndGetInfo(string archiveFullPath, bool readRequirementScript = false)
        {
            Response<ApplicationDto>? response = new Response<ApplicationDto>
            {
                IsSuccessful = false,
                ErrorMessage = "Invalid zip package."
            };

            try
            {
                await Task.Delay(500);
                using FileStream archiveFile = new(archiveFullPath, FileMode.Open);
                using ZipArchive archive = new(archiveFile, ZipArchiveMode.Read);

                Response<PackageConfigurationContent>? validateReadResponse = ValidateAndReadConfigurationContent(archive, readRequirementScript);

                if (validateReadResponse.IsSuccessful)
                {
                    response = GetApplicationDto(validateReadResponse.Dto);
                    response.Dto.IconBase64 = await GetSmallIconAsBase64Async(response.Dto.IconBase64, archive);

                    return response;
                }

                response.ErrorMessage = validateReadResponse.ErrorMessage;
            }
            catch
            {
                throw;
            }

            return response;
        }

        [Pure]
        private static Response<ApplicationDto> GetApplicationDto(PackageConfigurationContent xmlContent)
        {
            ICollection<string> missingRequiredFields = new List<string>();

            if (string.IsNullOrEmpty(xmlContent.Publisher))
            {
                missingRequiredFields.Add(nameof(xmlContent.Publisher));
            }

            if (string.IsNullOrEmpty(xmlContent.Name))
            {
                missingRequiredFields.Add(nameof(xmlContent.Name));
            }

            if (string.IsNullOrEmpty(xmlContent.Version))
            {
                missingRequiredFields.Add(nameof(xmlContent.Version));
            }

            if (missingRequiredFields.Any())
            {
                return new Response<ApplicationDto>
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Configuration file is missing the following required fields: {string.Join(",", missingRequiredFields)}"
                };
            }

            ApplicationDto? dto = new ApplicationDto()
            {
                Name = xmlContent.Name,
                Version = xmlContent.Version,
                Publisher = xmlContent.Publisher,
                RunAs32Bit = xmlContent.RunAs32Bit,
                InformationUrl = xmlContent.InformationUrl,
                Notes = xmlContent.Notes,
                IconBase64 = xmlContent.Icon,
                IconFileName = xmlContent.Icon,
                Language = xmlContent.Language ?? "en-US",
                Architecture = xmlContent.Architecture ?? "x86",
                Description = xmlContent.Description
            };

            return new Response<ApplicationDto> { Dto = dto, IsSuccessful = true };
        }

        [Pure]
        private static string GetExpectedMimeOrThrow(Image image) =>
            image.RawFormat.ToString() switch
            {
                "Png" => PngMime,
                "Jpeg" => JpegMime,
                _ => throw new ArgumentException("Picture must be either .png or .jpeg.")
            };

        [Pure]
        private static Bitmap ResizeImage(Image image, int width, int height)
        {
            Rectangle destinationRectangle = new Rectangle(0, 0, Math.Max(width, height), Math.Max(width, height));
            Bitmap? destinationBitmap = new Bitmap(width, height);

            try
            {
                destinationBitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            }

            //For some images "HorizontalResolution" and "VerticalResolution" erroneously returns 0 on Linux systems.
            //Please check the following links for more information:
            //https://xamarin.github.io/bugzilla-archives/33/33310/bug.html
            //https://stackoverflow.com/questions/28549617/c-sharp-bitmap-class-on-mono-has-invalid-properties
            catch (ArgumentException)
            {
                Graphics? graphicsImage = Graphics.FromImage(image);
                destinationBitmap.SetResolution(graphicsImage.DpiX, graphicsImage.DpiY);
            }

            using Graphics? graphics = Graphics.FromImage(destinationBitmap);

            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using ImageAttributes? wrapMode = new ImageAttributes();

            wrapMode.SetWrapMode(WrapMode.TileFlipXY);
            graphics.DrawImage(
                image,
                destinationRectangle,
                0,
                0,
                Math.Max(image.Width, image.Height),
                Math.Max(image.Width, image.Height),
                GraphicsUnit.Pixel,
                wrapMode);

            return destinationBitmap;
        }

        [Pure]
        private static Response<PackageConfigurationContent> ValidateAndReadConfigurationContent(ZipArchive archive, bool readRequirementScript)
        {
            ICollection<string> missingRequiredFields = new List<string>();

            Response<PackageConfigurationContent> validateReadResponse = new Response<PackageConfigurationContent>
            {
                IsSuccessful = true
            };

            bool toolkitFolderFound = archive.Entries.Any(entry => entry.FullName.Contains($"{DeploymentConstants.ToolkitFolder}/"));
            if (!toolkitFolderFound)
            {
                missingRequiredFields.Add(DeploymentConstants.ToolkitFolder);
            }

            ZipArchiveEntry detectAppScriptEntry = archive.GetEntry(DeploymentConstants.DetectApplicationScriptName);
            if (detectAppScriptEntry == null)
            {
                missingRequiredFields.Add(DeploymentConstants.DetectApplicationScriptName);
            }

            if (readRequirementScript)
            {
                ZipArchiveEntry requirementScriptEntry = archive.GetEntry(DeploymentConstants.RequirementScriptName);
                if (requirementScriptEntry == null)
                {
                    missingRequiredFields.Add(DeploymentConstants.RequirementScriptName);
                }
            }

            ZipArchiveEntry configXmlContent = archive.GetEntry(DeploymentConstants.ConfigurationFileName);
            if (configXmlContent == null)
            {
                missingRequiredFields.Add(DeploymentConstants.ConfigurationFileName);
            }

            if (missingRequiredFields.Any())
            {
                validateReadResponse.IsSuccessful = false;
                validateReadResponse.ErrorMessage = $"Missing zip contents: {string.Join(",", missingRequiredFields)}";

                return validateReadResponse;
            }

            using Stream? configXmlStream = configXmlContent.Open();
            XmlSerializer serializer = new XmlSerializer(typeof(PackageConfigurationContent));

            validateReadResponse.Dto = (PackageConfigurationContent)serializer.Deserialize(configXmlStream);

            return validateReadResponse;
        }
    }
}