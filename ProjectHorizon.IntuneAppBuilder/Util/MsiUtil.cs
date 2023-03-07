﻿using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using ProjectHorizon.IntuneAppBuilder.Domain;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using File = System.IO.File;

namespace ProjectHorizon.IntuneAppBuilder.Util
{
    /// <summary>
    ///     Helper for reading MSI metadata. Relies on Windows OS. When the tool is not run on Windows, reading MSI metadata will be skipped.
    /// </summary>
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
    internal class MsiUtil : IDisposable
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
    {
        private readonly dynamic database;

        private readonly dynamic installer;

        public MsiUtil(string path, ILogger logger)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("MSI file was not found.", path);
            }

            try
            {
                try
                {
                    installer = ComObject.CreateObject("WindowsInstaller.Installer");
                }
                catch
                {
                    logger.LogWarning("Could not create WindowsInstaller COM object. Ensure that you are running on Windows and that the Windows Installer service is available.");
                    return;
                }

                try
                {
                    database = installer.OpenDatabase(path, 0);
                }
                catch (COMException ex)
                {
                    throw new InvalidDataException($"The specified Windows Installer file {path} could not be opened. Verify the file is a valid Windows Installer file.", ex);
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            new[] { installer, database }.OfType<ComObject>().ToList().ForEach(x => x.Dispose());
            // otherwise MSI can remain locked for some time
#pragma warning disable S1215 // "GC.Collect" should not be called
            GC.Collect();
#pragma warning restore S1215 // "GC.Collect" should not be called
        }

        public (Win32LobAppMsiInformation Info, MobileMsiManifest Manifest) ReadMsiInfo()
        {
            if (installer == null)
            {
                // non-Windows platform
                return default;
            }

            Win32LobAppMsiInformation info = new Win32LobAppMsiInformation
            {
                ProductName = RetrievePropertyWithSummaryInfo("ProductName", 3),
                ProductCode = ReadProperty("ProductCode"),
                ProductVersion = ReadProperty("ProductVersion"),
                UpgradeCode = ReadProperty("UpgradeCode", false),
                Publisher = RetrievePropertyWithSummaryInfo("Manufacturer", 4),
                PackageType = GetPackageType(),
                RequiresReboot = ReadProperty("REBOOT", false) is { } s && !string.IsNullOrEmpty(s) && s[0] == 'F'
            };

            MobileMsiManifest? manifest = GetManifest(info);

            return (info, manifest);
        }

        private string GetMsiExecutionContext(Win32LobAppMsiPackageType? type)
        {
            switch (type)
            {
                case Win32LobAppMsiPackageType.PerUser:
                    return "User";
                case Win32LobAppMsiPackageType.PerMachine:
                    return "System";
                default:
                    return "Any";
            }
        }

        private MobileMsiManifest GetManifest(Win32LobAppMsiInformation info)
        {
#pragma warning disable S125 // Sections of code should not be commented out
            return new MobileMsiManifest
            {
                MsiExecutionContext = GetMsiExecutionContext(info.PackageType),
                MsiPublisher = info.Publisher,
                MsiProductCode = info.ProductCode,
                MsiPackageCode = RetrievePropertyWithSummaryInfo("PackageCode", 9),
                MsiProductVersion = info.ProductVersion,
                MsiUpgradeCode = info.UpgradeCode,
                MsiRequiresLogon = info.PackageType == Win32LobAppMsiPackageType.PerUser,
                MsiRequiresReboot = info.RequiresReboot.GetValueOrDefault(),
                MsiIsUserInstall = IsUserInstall(),
                MsiIsMachineInstall = info.PackageType == Win32LobAppMsiPackageType.PerMachine || info.PackageType == Win32LobAppMsiPackageType.DualPurpose && !string.IsNullOrEmpty(ReadProperty("MSIINSTALLPERUSER", false)),
                MsiIncludesServices = TableContainsRecords("ServiceInstall", "ServiceInstall"),
                MsiContainsSystemFolders = ContainsSystemFolders(),
                MsiContainsSystemRegistryKeys = ContainsSystemRegistryKeys(),
                MsiIncludesODBCDataSource = TableContainsRecords("ODBCDataSource", "DataSource")
            };
        }

        private bool IsUserInstall()
        {
            return GetPackageType() is { } type
                   && type == Win32LobAppMsiPackageType.PerUser
                   || (type == Win32LobAppMsiPackageType.DualPurpose
                       && !string.IsNullOrEmpty(ReadProperty("MSIINSTALLPERUSER", false)));
        }

        private Win32LobAppMsiPackageType GetPackageType()
        {
            switch (ReadProperty("ALLUSERS", false))
            {
                case var s when string.IsNullOrEmpty(s):
                    return Win32LobAppMsiPackageType.PerUser;
                case var s when s == "1":
                    return Win32LobAppMsiPackageType.PerMachine;
                case var s when s == "2":
                    return Win32LobAppMsiPackageType.DualPurpose;
                case var s:
                    throw new InvalidDataException($"Invalid ALLUSERS property value: {s}.");
            }
        }

        private string ReadProperty(string name, bool throwOnNotFound = true)
        {
            try
            {
                dynamic view = database.OpenView("SELECT Value FROM Property WHERE Property ='" + name + "'");
                view.Execute();
                dynamic record = view.Fetch();
                if (record == null && throwOnNotFound)
                {
                    throw new ArgumentException($"Property not found: {name}.");
                }

                return record?.get_StringData(1).Trim();
            }
            catch (Exception ex) when (ex is ArgumentException || ex is COMException)
            {
                if (throwOnNotFound)
                {
                    throw;
                }

                return null;
            }
        }

        private string RetrievePropertyWithSummaryInfo(string propertyName, int summaryId)
        {
            try
            {
                string? text = ReadProperty(propertyName, false);
                if (!string.IsNullOrEmpty(text))
                {
                    return text;
                }

                return database.get_SummaryInformation(0).get_Property(summaryId) as string;
            }
            catch (COMException)
            {
                return null;
            }
        }
        private bool ContainsSystemRegistryKeys()
            => (ContainsSystemRegistryKeys("Registry") || ContainsSystemRegistryKeys("RemoveRegistry"));

        private bool ContainsSystemRegistryKeys(string table)
        {
            try
            {
                dynamic view = Query(table, "Root");
                for (dynamic record = view.Fetch(); record != null; record = view.Fetch())
                {
                    if (record.get_StringData(1) is string s &&
                        (new[] { "2", "3", }.Contains(s, StringComparer.OrdinalIgnoreCase)
                         || (s == "-1" && IsUserInstall())))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex) when (ex is ArgumentException || ex is COMException)
            {
                return false;
            }
            return false;
        }

        private bool ContainsSystemFolders()
        {
            dynamic view = Query("Directory", "Directory");
            for (dynamic record = view.Fetch(); record != null; record = view.Fetch())
            {
                if (new[]
                {
                    "AdminToolsFolder",
                    "CommonAppDataFolder",
                    "FontsFolder",
                    "WindowsFolder",
                    "WindowsVolume",
                    "System16Folder",
                    "System64Folder",
                    "SystemFolder",
                    "TempFolder"
                }.Contains((string)record.get_StringData(1), StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TableContainsRecords(string table, string column)
        {
            try
            {
                if (Query(table, column).Fetch() == null)
                {
                    return true;
                }
            }
            catch (Exception ex) when (ex is ArgumentException || ex is COMException)
            {
                return false;
            }

            return false;
        }

        private dynamic Query(string table, string columns)
        {
            try
            {
                dynamic view = database.OpenView($"SELECT {columns} FROM `{table}`");
                view.Execute();
                return view;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is COMException)
            {
                throw new ArgumentException($"Table not found: {table}.", nameof(table), ex);
            }
        }
    }
}
