using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.Infrastructure.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ProjectHorizon.UnitTests.Infrastructure
{
    public class IntuneConverterServiceTest
    {
        //valid zip file included in TestData folder
        private readonly string validTestZipName = "valid_test_package.zip";
        private readonly string invalidTestZipName = "invalid_test_package.zip";
        private readonly string validTestZipNameLocation;
        private readonly string invalidTestZipNameLocation;

        public IntuneConverterServiceTest()
        {
            validTestZipNameLocation = Path.Combine(Directory.GetCurrentDirectory(), $"TestData\\{validTestZipName}");
            invalidTestZipNameLocation = Path.Combine(Directory.GetCurrentDirectory(), $"TestData\\{invalidTestZipName}");
        }

        [Fact]
        public async Task ConvertToIntuneFormatAsync_ValidInputZip_ValidIntuneOutputFile_ReturnsPackageInfo()
        {
            //arrange
            IIntuneConverterService intuneConverterService = new IntuneConverterService(null);
            string zipFileNameWithoutExtension = Path.GetFileNameWithoutExtension(validTestZipNameLocation);
            DirectoryInfo extractPath = new DirectoryInfo(Path.GetTempPath()).CreateSubdirectory(zipFileNameWithoutExtension);

            //act
            await intuneConverterService.ConvertToIntuneFormatAsync(validTestZipNameLocation);

            //assert
            Assert.True(Directory.GetFiles(extractPath.FullName, "*.intunewin").Length > 0,
                $"{extractPath.FullName} should include intune files");
        }

        [Fact]
        public async Task ConvertToIntuneFormatAsync_InvalidInputZip_NoOutputIntuneFile()
        {
            //arrange
            IIntuneConverterService intuneConverterService = new IntuneConverterService(null);

            //act & assert
            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                intuneConverterService.ConvertToIntuneFormatAsync(invalidTestZipNameLocation));
        }

        [Fact]
        public async Task ConvertToIntuneFormatAsync_NullArgumentValidationAsync()
        {
            //arrange
            IIntuneConverterService intuneConverterService = new IntuneConverterService(null);

            //act & assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => intuneConverterService.ConvertToIntuneFormatAsync(null));
        }

        [Fact]
        public async Task ConvertToIntuneFormatAsync_EmptyPathArgumentValidationAsync()
        {
            //arrange
            IIntuneConverterService intuneConverterService = new IntuneConverterService(null);

            //act & assert
            await Assert.ThrowsAsync<ArgumentException>(() => intuneConverterService.ConvertToIntuneFormatAsync(String.Empty));
        }
    }
}
