using Microsoft.AspNetCore.Hosting;
using Moq;

namespace CMCS_Part3.Tests
{
    public static class TestHelpers
    {
        public static Mock<IWebHostEnvironment> CreateMockWebHostEnvironment()
        {
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            mockEnvironment.Setup(m => m.WebRootPath).Returns("wwwroot");
            mockEnvironment.Setup(m => m.ContentRootPath).Returns(".");
            mockEnvironment.Setup(m => m.EnvironmentName).Returns("Test");
            return mockEnvironment;
        }
    }
}