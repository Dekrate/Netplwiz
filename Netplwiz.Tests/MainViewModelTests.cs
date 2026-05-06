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
        public void SelectedUser_ProtectedUser_RemoveUserDisabled()
        {
            _mockService.Setup(s => s.IsProtectedAccount("Admin")).Returns(true);
            var vm = new MainViewModel(_mockService.Object)
            {
                SelectedUser = new UserAccount { UserName = "Admin" }
            };

            Assert.IsFalse(vm.RemoveUserCommand.CanExecute(null));
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
        public void OpenAddUserSettingsCommand_CallsService()
        {
            var vm = new MainViewModel(_mockService.Object);
            vm.OpenAddUserSettingsCommand.Execute(null);
            _mockService.Verify(s => s.OpenAddUserDialog(), Times.Once);
        }

        [TestMethod]
        public void ManagePasswordsCommand_CallsService()
        {
            var vm = new MainViewModel(_mockService.Object);
            vm.ManagePasswordsCommand.Execute(null);
            _mockService.Verify(s => s.OpenCredentialManager(), Times.Once);
        }

        [TestMethod]
        public void AdvancedUserManagementCommand_CallsService()
        {
            var vm = new MainViewModel(_mockService.Object);
            vm.AdvancedUserManagementCommand.Execute(null);
            _mockService.Verify(s => s.OpenAdvancedUserManagement(), Times.Once);
        }

        [TestMethod]
        public void CreateLocalUser_CallsService_WithCorrectParameters()
        {
            _mockService.Setup(s => s.AddLocalUser("TestUser", "Pass123!", "Test Full", "Desc", true)).Returns(true);
            var vm = new MainViewModel(_mockService.Object);

            var result = vm.CreateLocalUser("TestUser", "Pass123!", "Test Full", "Desc", true);

            Assert.IsTrue(result);
            _mockService.Verify(s => s.AddLocalUser("TestUser", "Pass123!", "Test Full", "Desc", true), Times.Once);
        }

        [TestMethod]
        public void CreateLocalUser_EmptyUserName_ReturnsFalse()
        {
            _mockService.Setup(s => s.AddLocalUser("", "Pass123!", "", "", false)).Returns(false);
            var vm = new MainViewModel(_mockService.Object);

            var result = vm.CreateLocalUser("", "Pass123!", "", "", false);

            Assert.IsFalse(result);
            _mockService.Verify(s => s.AddLocalUser("", "Pass123!", "", "", false), Times.Once);
        }

        [TestMethod]
        public void CreateLocalUser_EmptyPassword_ReturnsFalse()
        {
            _mockService.Setup(s => s.AddLocalUser("TestUser", "", "", "", false)).Returns(false);
            var vm = new MainViewModel(_mockService.Object);

            var result = vm.CreateLocalUser("TestUser", "", "", "", false);

            Assert.IsFalse(result);
            _mockService.Verify(s => s.AddLocalUser("TestUser", "", "", "", false), Times.Once);
        }

        [TestMethod]
        public void CreateLocalUser_UserAlreadyExists_ReturnsFalse()
        {
            _mockService.Setup(s => s.AddLocalUser("Existing", "Pass123!", "", "", false)).Returns(false);
            var vm = new MainViewModel(_mockService.Object);

            var result = vm.CreateLocalUser("Existing", "Pass123!", "", "", false);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CreateLocalUser_ServiceThrows_ReturnsFalse()
        {
            _mockService.Setup(s => s.AddLocalUser("Crash", "Pass123!", "", "", false)).Throws(new System.InvalidOperationException("Boom"));
            var vm = new MainViewModel(_mockService.Object);

            var result = vm.CreateLocalUser("Crash", "Pass123!", "", "", false);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void AddLocalUserCommand_RaisesEvent()
        {
            var vm = new MainViewModel(_mockService.Object);
            var eventFired = false;
            vm.RequestAddLocalUser += (_, _) => eventFired = true;

            vm.AddLocalUserCommand.Execute(null);

            Assert.IsTrue(eventFired);
        }

        [TestMethod]
        public void UserDetailsCommand_RaisesEvent_WhenUserSelected()
        {
            var vm = new MainViewModel(_mockService.Object)
            {
                SelectedUser = new UserAccount { UserName = "Test" }
            };
            var eventFired = false;
            vm.RequestUserDetails += (_, _) => eventFired = true;

            vm.UserDetailsCommand.Execute(null);

            Assert.IsTrue(eventFired);
        }

        [TestMethod]
        public void UserDetailsCommand_Disabled_WhenNoUserSelected()
        {
            var vm = new MainViewModel(_mockService.Object) { SelectedUser = null };
            Assert.IsFalse(vm.UserDetailsCommand.CanExecute(null));
        }

        [TestMethod]
        public void EditPasswordPolicyCommand_RaisesEvent()
        {
            var vm = new MainViewModel(_mockService.Object);
            var eventFired = false;
            vm.RequestEditPasswordPolicy += (_, _) => eventFired = true;

            vm.EditPasswordPolicyCommand.Execute(null);

            Assert.IsTrue(eventFired);
        }

        [TestMethod]
        public void SetPasswordPolicy_CallsService()
        {
            var policy = new PasswordPolicy { MinimumPasswordLength = 8, AccountLockoutThreshold = 3 };
            _mockService.Setup(s => s.SetPasswordPolicy(policy)).Returns(true);
            var vm = new MainViewModel(_mockService.Object);

            var result = vm.SetPasswordPolicy(policy);

            Assert.IsTrue(result);
            _mockService.Verify(s => s.SetPasswordPolicy(policy), Times.Once);
        }
    }
}
