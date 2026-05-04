using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Netplwiz.Models;
using Netplwiz.Services;
using Netplwiz.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Netplwiz.Tests
{
    [TestClass]
    public class MainViewModelTests
    {
        private Mock<IUserService> _mockService = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockService = new Mock<IUserService>();
            _mockService.Setup(s => s.IsSecureLogonRequired()).Returns(false);
        }

        [TestMethod]
        public async Task LoadUsersAsync_PopulatesUsers()
        {
            var users = new List<UserAccount>
            {
                new() { UserName = "Alice", FullName = "Alice Smith" },
                new() { UserName = "Bob", FullName = "Bob Jones" }
            };
            _mockService.Setup(s => s.GetLocalUsers()).Returns(users);

            var vm = new MainViewModel(_mockService.Object);
            await vm.LoadUsersAsync();

            Assert.AreEqual(2, vm.Users.Count);
            Assert.AreEqual("Alice", vm.Users.First().UserName);
        }

        [TestMethod]
        public async Task LoadUsersAsync_SetsStatusMessage()
        {
            _mockService.Setup(s => s.GetLocalUsers()).Returns(new List<UserAccount>());

            var vm = new MainViewModel(_mockService.Object);
            await vm.LoadUsersAsync();

            StringAssert.Contains(vm.StatusMessage, "0");
        }

        [TestMethod]
        public void SelectedUser_Null_CommandsDisabled()
        {
            var vm = new MainViewModel(_mockService.Object) { SelectedUser = null };

            Assert.IsFalse(vm.RemoveUserCommand.CanExecute(null));
            Assert.IsFalse(vm.PropertiesCommand.CanExecute(null));
            Assert.IsFalse(vm.ResetPasswordCommand.CanExecute(null));
        }

        [TestMethod]
        public void SelectedUser_Set_CommandsEnabled()
        {
            var vm = new MainViewModel(_mockService.Object)
            {
                SelectedUser = new UserAccount { UserName = "Test" }
            };

            Assert.IsTrue(vm.RemoveUserCommand.CanExecute(null));
            Assert.IsTrue(vm.PropertiesCommand.CanExecute(null));
            Assert.IsTrue(vm.ResetPasswordCommand.CanExecute(null));
        }

        [TestMethod]
        public void DeleteUser_CallsService()
        {
            _mockService.Setup(s => s.DeleteUser("Test")).Returns(true);
            var vm = new MainViewModel(_mockService.Object);

            var result = vm.DeleteUser("Test");

            Assert.IsTrue(result);
            _mockService.Verify(s => s.DeleteUser("Test"), Times.Once);
        }

        [TestMethod]
        public void SetUserPassword_CallsService()
        {
            _mockService.Setup(s => s.SetPassword("Test", "NewPass123!")).Returns(true);
            var vm = new MainViewModel(_mockService.Object);

            var result = vm.SetUserPassword("Test", "NewPass123!");

            Assert.IsTrue(result);
            _mockService.Verify(s => s.SetPassword("Test", "NewPass123!"), Times.Once);
        }

        [TestMethod]
        public void SecureLogon_ReflectsServiceState()
        {
            _mockService.Setup(s => s.IsSecureLogonRequired()).Returns(true);
            var vm = new MainViewModel(_mockService.Object);

            Assert.IsTrue(vm.SecureLogonRequired);
        }

        [TestMethod]
        public void AddUserCommand_DoesNotThrow()
        {
            var vm = new MainViewModel(_mockService.Object);
            vm.AddUserCommand.Execute(null);
        }

        [TestMethod]
        public void ManagePasswordsCommand_DoesNotThrow()
        {
            var vm = new MainViewModel(_mockService.Object);
            vm.ManagePasswordsCommand.Execute(null);
        }

        [TestMethod]
        public void AdvancedUserManagementCommand_DoesNotThrow()
        {
            var vm = new MainViewModel(_mockService.Object);
            vm.AdvancedUserManagementCommand.Execute(null);
        }
    }
}
