using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Netplwiz.Models;
using Netplwiz.Services;
using Netplwiz.ViewModels;
using System.Threading.Tasks;

namespace Netplwiz.Tests
{
    [TestClass]
    public class UserPropertiesViewModelTests
    {
        private Mock<IUserService> _mockService = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockService = new Mock<IUserService>();
        }

        [TestMethod]
        public void Constructor_InitializesProperties()
        {
            var user = new UserAccount { UserName = "Alice", FullName = "Alice Smith", Description = "Dev" };
            _mockService.Setup(s => s.GetLocalUsers()).Returns(new System.Collections.Generic.List<UserAccount>());

            var vm = new UserPropertiesViewModel(user, _mockService.Object);

            Assert.AreEqual("Alice", vm.UserName);
            Assert.AreEqual("Alice Smith", vm.FullName);
            Assert.AreEqual("Dev", vm.Description);
        }

        [TestMethod]
        public async Task SaveAsync_CallsUpdateUser_WithCorrectData()
        {
            var user = new UserAccount { UserName = "Alice", FullName = "Alice Smith", Description = "Dev" };
            _mockService.Setup(s => s.UpdateUser(It.IsAny<UserAccount>())).Returns(true);

            var vm = new UserPropertiesViewModel(user, _mockService.Object)
            {
                FullName = "Alice Smith-Jones",
                Description = "Senior Dev"
            };

            await vm.SaveAsync();

            _mockService.Verify(s => s.UpdateUser(It.Is<UserAccount>(u =>
                u.UserName == "Alice" &&
                u.FullName == "Alice Smith-Jones" &&
                u.Description == "Senior Dev"
            )), Times.Once);
        }

        [TestMethod]
        public void RadioButtons_AreMutuallyExclusive_StandardUser()
        {
            var user = new UserAccount { UserName = "Alice" };
            var vm = new UserPropertiesViewModel(user, _mockService.Object)
            {
                IsStandardUser = true
            };

            Assert.IsTrue(vm.IsStandardUser);
            Assert.IsFalse(vm.IsAdministrator);
            Assert.IsFalse(vm.IsOtherGroup);
        }

        [TestMethod]
        public void RadioButtons_AreMutuallyExclusive_Administrator()
        {
            var user = new UserAccount { UserName = "Alice" };
            var vm = new UserPropertiesViewModel(user, _mockService.Object)
            {
                IsAdministrator = true
            };

            Assert.IsFalse(vm.IsStandardUser);
            Assert.IsTrue(vm.IsAdministrator);
            Assert.IsFalse(vm.IsOtherGroup);
        }

        [TestMethod]
        public void RadioButtons_AreMutuallyExclusive_OtherGroup()
        {
            var user = new UserAccount { UserName = "Alice" };
            var vm = new UserPropertiesViewModel(user, _mockService.Object)
            {
                IsOtherGroup = true
            };

            Assert.IsFalse(vm.IsStandardUser);
            Assert.IsFalse(vm.IsAdministrator);
            Assert.IsTrue(vm.IsOtherGroup);
        }
    }
}
